using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using EZ.Json.Extensions;

namespace EZ.Network.Editor
{
    public class NetworkActivityInspector : EditorWindow
    {
        private static NetworkActivityInspector window = null;

        private const float FOLD_WIDTH = 160f;

        private Vector2 _scrollPositionLogs = Vector2.zero;
        private Vector2 _scrollPositionDetails = Vector2.zero;

        private int _prevCount;
        private float _realtimeSinceStartup;

        private bool _isUpdateWhenChanged;
        private bool _isScrollLock;
        private bool _isClearOnStop;
        private string _searchKeyword;

        private bool _isFoldGeneral;
        private bool _isFoldResponseHeaders;
        private bool _isFoldRequestHeaders;
        private bool _isFoldRequestPayload;
        private bool _isFoldResponse;
        private bool _isPretty;
        private Protocol protocolFlags = Protocol.Everything;

        private bool _isDirty;

        private List<PacketLog> _filteredLogs;
        private int logCount => _filteredLogs?.Count ?? 0;

        private PacketLog _selected;
        private int _selectedIndex;

        private GUIStyle _bufferContentStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _selectedStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dropDownStyle;

        [MenuItem("Tools/Inspect Network Activity %F1", false, 1)]
        static public void Open()
        {
            window = EditorWindow.GetWindow<NetworkActivityInspector>(false, "Network Activity Inspector", true);
            if (window != null)
                window.Initialize();
        }

        private void Initialize()
        {
            _scrollPositionLogs = Vector2.zero;
            _scrollPositionDetails = Vector2.zero;

            _isUpdateWhenChanged = false;
            _isScrollLock = false;
            _isClearOnStop = false;
            _searchKeyword = string.Empty;

            _prevCount = 0;
            _realtimeSinceStartup = Time.realtimeSinceStartup;

            _isFoldGeneral = true;
            _isFoldResponseHeaders = true;
            _isFoldRequestHeaders = true;
            _isFoldRequestPayload = true;
            _isFoldResponse = true;

            _selected = null;
            _selectedIndex = -1;

            var style = EditorStyles.toolbarButton;

            _normalStyle = new GUIStyle(style);
            _normalStyle.alignment = TextAnchor.MiddleLeft;
            _normalStyle.hover.textColor = Color.white;
            _normalStyle.focused.textColor = Color.white;

            _selectedStyle = new GUIStyle(style);
            _selectedStyle.alignment = TextAnchor.MiddleLeft;
            _selectedStyle.hover.textColor = Color.white;
            _selectedStyle.focused.textColor = Color.white;

            _buttonStyle = new GUIStyle(style);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.fixedHeight = 56f;

            _bufferContentStyle = new GUIStyle(EditorStyles.label);
            _bufferContentStyle.richText = true;

            _dropDownStyle = new GUIStyle("ToolbarDropDown");
            _dropDownStyle.fixedWidth = 100f;

            _isPretty = true;

            NetworkActivity.webRequestRequested += OnRequest;
            NetworkActivity.webResponseReceived += OnReceived;
        }

        private void OnRequest(PacketLog packetLog)
        {
            _isDirty = true;
        }

        private void OnReceived()
        {
            _isDirty = true;
        }

        private void DrawMenuBar()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.MaxWidth(82f)))
                Clear();

            if (GUILayout.Button("Fold", EditorStyles.toolbarButton, GUILayout.MaxWidth(82f)))
            {
                if (_isFoldGeneral
                    || _isFoldRequestHeaders
                    || _isFoldResponseHeaders
                    || _isFoldRequestPayload
                    || _isFoldResponse)
                {
                    _isFoldGeneral = false;
                    _isFoldRequestHeaders = false;
                    _isFoldResponseHeaders = false;
                    _isFoldRequestPayload = false;
                    _isFoldResponse = false;
                }
                else
                {
                    _isFoldGeneral = true;
                    _isFoldRequestHeaders = true;
                    _isFoldResponseHeaders = true;
                    _isFoldRequestPayload = true;
                    _isFoldResponse = true;
                }
            }

            _isClearOnStop = GUILayout.Toggle(_isClearOnStop, "Clear on Stop", EditorStyles.toolbarButton, GUILayout.MaxWidth(116f));
            _isPretty = GUILayout.Toggle(_isPretty, "Pretty", EditorStyles.toolbarButton);

            var changedProtocolFlags = (Protocol)EditorGUILayout.EnumFlagsField(protocolFlags, _dropDownStyle);

            if (protocolFlags != changedProtocolFlags)
            {
                protocolFlags = changedProtocolFlags;
                OnProtocolFlagChanged();
            }

            var searchKeyWord = EditorGUILayout.TextField(_searchKeyword, EditorStyles.toolbarSearchField);

            if (searchKeyWord != _searchKeyword)
            {
                _searchKeyword = searchKeyWord;
                OnSearchingKeywordChanged();
            }

            _isUpdateWhenChanged = GUILayout.Toggle(_isUpdateWhenChanged, "Update When Changed", EditorStyles.toolbarButton, GUILayout.MaxWidth(146f));
            _isScrollLock = GUILayout.Toggle(_isScrollLock, "Scroll Lock", EditorStyles.toolbarButton, GUILayout.MaxWidth(100f));

            GUILayout.EndHorizontal();
        }

        private bool ProcessKeyEvent(Event current)
        {
            if (current != null)
            {
                if (current.type == EventType.KeyUp)
                {
                    switch (current.keyCode)
                    {
                        case KeyCode.UpArrow:
                            _selectedIndex = Mathf.Max(--_selectedIndex, 0);
                            _selected = _filteredLogs[_selectedIndex];
                            Repaint();
                            return true;
                        case KeyCode.DownArrow:
                            _selectedIndex = Mathf.Min(++_selectedIndex, logCount - 1);
                            _selected = _filteredLogs[_selectedIndex];
                            Repaint();
                            return true;
                    }
                }
            }
            return false;
        }

        private void OnGUI()
        {
            var isProcessed = ProcessKeyEvent(Event.current);

            if (isProcessed)
                return;

            DrawMenuBar();

            EditorGUILayout.BeginHorizontal();
            {
                var hasSelected = (_selected != null);

                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                {
                    if (logCount > 0)
                        DrawLogs();
                }
                EditorGUILayout.EndVertical();

                if (hasSelected)
                    DrawDetails();
            }
            EditorGUILayout.EndHorizontal();
        }

        private string GetFoldHeader(string headerName, bool isFold)
        {
            return string.Format("{0} {1}", (isFold) ? "▼" : "▶", headerName);
        }

        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical();

            _scrollPositionDetails = EditorGUILayout.BeginScrollView(_scrollPositionDetails);
            {
                _isFoldGeneral = GUILayout.Toggle(_isFoldGeneral, GetFoldHeader("General", _isFoldGeneral), EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));

                if (_isFoldGeneral)
                {
                    EditorGUILayout.LabelField("Request URL:", _selected.Url);
                    EditorGUILayout.LabelField("Request Method:", _selected.Method);
                    EditorGUILayout.LabelField("Status Code:", _selected.StatusCode.ToString());

                    EditorGUILayout.LabelField("Result:", _selected.result.ToString());
                    EditorGUILayout.LabelField("Http Error:", _selected.IsHttpError.ToString());
                    EditorGUILayout.LabelField("Network Error:", _selected.IsNetworkError.ToString());
                    EditorGUILayout.LabelField("Error:", _selected.Error);
                }

                _isFoldResponseHeaders = GUILayout.Toggle(_isFoldResponseHeaders, GetFoldHeader("Response Headers", _isFoldResponseHeaders), EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));

                if (_isFoldResponseHeaders)
                {
                    if (!(_selected.ResponseHeaders == null || _selected.ResponseHeaders.Count == 0))
                    {
                        var enumerator = _selected.ResponseHeaders.GetEnumerator();

                        while (enumerator.MoveNext())
                        {
                            EditorGUILayout.LabelField(enumerator.Current.Key, enumerator.Current.Value);
                        }
                    }
                }

                _isFoldRequestHeaders = GUILayout.Toggle(_isFoldRequestHeaders, GetFoldHeader("Request Headers", _isFoldRequestHeaders), EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));

                if (_isFoldRequestHeaders)
                {
                    var contentType = string.Empty;
                    _selected.RequestHeaders.TryGetValue("Content-Type", out contentType);
                    EditorGUILayout.LabelField("Content-Type:", contentType);
                }

                GUILayout.BeginHorizontal();

                _isFoldRequestPayload = GUILayout.Toggle(_isFoldRequestPayload, GetFoldHeader("Request Payload", _isFoldRequestPayload), EditorStyles.toolbarButton, GUILayout.Width(FOLD_WIDTH));

                if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                {
                    EditorGUIUtility.systemCopyBuffer = GetSelectedContent(_selected, _selected.RequestPayload);
                }

                GUILayout.EndHorizontal();

                if (_isFoldRequestPayload)
                {
                    GUILayout.Label(GetSelectedContent(_selected, _selected.RequestPayload), _bufferContentStyle);
                }

                GUILayout.BeginHorizontal();
                _isFoldResponse = GUILayout.Toggle(_isFoldResponse, GetFoldHeader("Response", _isFoldResponse), EditorStyles.toolbarButton, GUILayout.Width(FOLD_WIDTH));

                if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                {
                    EditorGUIUtility.systemCopyBuffer = GetSelectedContent(_selected, _selected.Response);
                }

                GUILayout.EndHorizontal();

                if (_isFoldResponse)
                {
                    GUILayout.Label(GetSelectedContent(_selected, _selected.Response), _bufferContentStyle);
                }

                if (GUILayout.Button("Copy Packet", _buttonStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = _selected.ToString();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private string GetSelectedContent(PacketLog selected, string content)
        {
            try
            {
                if (!_isPretty)
                    return content;

                var prettyFallback = content.ToPrettyJSONify();
                return prettyFallback;                
            }
            catch
            {
                return string.Empty;
            }
        }

        private void DrawLogs()
        {
            _scrollPositionLogs = EditorGUILayout.BeginScrollView(_scrollPositionLogs);

            for (var i = 0; i < logCount; i++)
            {
                var packetLog = _filteredLogs[i];

                if (string.IsNullOrEmpty(_searchKeyword) == false)
                {
                    var isContains = packetLog.Url.Contains(_searchKeyword);

                    if (isContains == false)
                    {
                        if (!string.IsNullOrEmpty(packetLog.RequestPayload))
                            isContains = packetLog.RequestPayload.Contains(_searchKeyword);
                    }
                    if (isContains == false)
                    {
                        if (!string.IsNullOrEmpty(packetLog.Response))
                            isContains = packetLog.Response.Contains(_searchKeyword);
                    }
                    if (isContains == false)
                    {
                        continue;
                    }
                }

                var isSelected = (_selectedIndex == i);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button($"{packetLog.Url}", isSelected ? _selectedStyle : _normalStyle))
                {
                    _selected = packetLog;
                    _selectedIndex = i;
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (_isDirty)
            {
                UpdateLogs();

                if (_prevCount == 0)
                {
                    //...
                }
                else if ((Time.realtimeSinceStartup - _realtimeSinceStartup) < 0.2f)
                {
                    return;
                }

                _prevCount = logCount;
                _realtimeSinceStartup = Time.realtimeSinceStartup;

                _isDirty = false;

                if (_isUpdateWhenChanged)
                {
                    if (_isScrollLock == false)
                    {
                        _scrollPositionLogs = new Vector2(_scrollPositionLogs.x, Mathf.Infinity);
                    }

                    Repaint();
                }
            }
        }

        private void OnSearchingKeywordChanged()
        {
            // TODO : Implementation
        }

        private void OnProtocolFlagChanged()
        {
            UpdateLogs();
        }

        private void UpdateLogs()
        {
            _filteredLogs = NetworkActivityLogger.Filter(protocolFlags);            
        }

        private void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (_isClearOnStop)
            {
                if (playModeState == PlayModeStateChange.ExitingPlayMode)
                    Clear();
            }

            if (playModeState == PlayModeStateChange.EnteredPlayMode)
            {
                UpdateLogs();
            }
        }

        private void Clear()
        {
            _selected = null;
            _selectedIndex = -1;
            NetworkActivityLogger.Clear();
            UpdateLogs();
        }

        #region Callbacks

        private void Awake()
        {
            window = this;
        }

        private void OnDisable()
        {
            if (_isClearOnStop)
            {
                Clear();
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion Callbacks
    }
}