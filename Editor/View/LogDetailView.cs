using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using EZ.Json.Extensions;

namespace EZ.Network.Editor.View
{
    public class LogDetailView : IMGUIContainer
    {
        private const float FOLD_WIDTH = 160f;

        private Vector2 _scrollPositionDetails = Vector2.zero;

        private bool _isFoldGeneral = true;
        private bool _isFoldResponseHeaders = true;
        private bool _isFoldRequestHeaders = true;
        private bool _isFoldRequestPayload = true;
        private bool _isFoldResponse = true;

        private GUIStyle _buttonStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIStyle _bufferContentStyle;

        private System.Func<bool> _pretty;
        private PacketLog _selected;

        public LogDetailView(System.Func<bool> pretty)
        {
            onGUIHandler += OnDraw;

            _pretty = pretty;

            _bufferContentStyle = new GUIStyle(EditorStyles.label);
            _bufferContentStyle.richText = true;

            _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            
            var style = EditorStyles.toolbarButton;
            _buttonStyle = new GUIStyle(style);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.fixedHeight = 56f;

            _selected = PacketLog.Default;
        }

        internal void SetSelected(PacketLog packetLog)
        {
            _selected = packetLog;
        }

        private string GetFoldHeader(string headerName, bool isFold)
        {
            return string.Format("{0} {1}", (isFold) ? "▼" : "▶", headerName);
        }

        public void Fold()
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

        private string GetContentType()
        {
            var contentType = string.Empty;
            if (_selected != null)
                _selected.RequestHeaders.TryGetValue("Content-Type", out contentType);
            return contentType;
        }

        void OnDraw()
        {
            EditorGUILayout.BeginVertical();

            _scrollPositionDetails = EditorGUILayout.BeginScrollView(_scrollPositionDetails);
            {
                _isFoldGeneral = GUILayout.Toggle(_isFoldGeneral,
                    GetFoldHeader("General", _isFoldGeneral),
                    EditorStyles.toolbarButton,
                    GUILayout.ExpandWidth(true));

                if (_isFoldGeneral)
                {
                    EditorGUILayout.LabelField("Request URL:", _selected.Url);
                    EditorGUILayout.LabelField("Request Method:", _selected.Method);
                    EditorGUILayout.LabelField("Status Code:", _selected.StatusCode.ToString());

                    EditorGUILayout.LabelField("Result:", _selected.Result.ToString());
                    EditorGUILayout.LabelField("Http Error:", _selected.IsHttpError.ToString());
                    EditorGUILayout.LabelField("Network Error:", _selected.IsNetworkError.ToString());
                    EditorGUILayout.LabelField("Error:", _selected.Error);
                }

                _isFoldResponseHeaders = GUILayout.Toggle(_isFoldResponseHeaders,
                    GetFoldHeader("Response Headers", _isFoldResponseHeaders),
                    EditorStyles.toolbarButton,
                    GUILayout.ExpandWidth(true));

                if (_isFoldResponseHeaders)
                {
                    if (_selected != null
                        && !(_selected.ResponseHeaders == null || _selected.ResponseHeaders.Count == 0))
                    {
                        var enumerator = _selected.ResponseHeaders.GetEnumerator();

                        while (enumerator.MoveNext())
                        {
                            EditorGUILayout.LabelField(enumerator.Current.Key, enumerator.Current.Value);
                        }

                        if (GUILayout.Button("Copy Response Headers", 
                            _toolbarButtonStyle, 
                            GUILayout.ExpandWidth(true)))
                        {
                            var builder = new System.Text.StringBuilder();
                            foreach (var header in _selected.ResponseHeaders)
                            {
                                builder.AppendLine($"{header.Key} : {header.Value}");
                            }
                            EditorGUIUtility.systemCopyBuffer = builder.ToString();
                        }
                    }
                }

                _isFoldRequestHeaders = GUILayout.Toggle(_isFoldRequestHeaders,
                    GetFoldHeader("Request Headers", _isFoldRequestHeaders),
                    EditorStyles.toolbarButton,
                    GUILayout.ExpandWidth(true));

                if (_isFoldRequestHeaders)
                {
                    var contentType = GetContentType();
                    EditorGUILayout.LabelField("Content-Type:", contentType);
                }

                GUILayout.BeginHorizontal();
                {
                    _isFoldRequestPayload = GUILayout.Toggle(_isFoldRequestPayload,
                        GetFoldHeader("Request Payload", _isFoldRequestPayload),
                        EditorStyles.toolbarButton,
                        GUILayout.Width(FOLD_WIDTH));

                    if (GUILayout.Button("Copy", 
                        _toolbarButtonStyle, 
                        GUILayout.ExpandWidth(true),
                        GUILayout.MinWidth(128f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = GetSelectedContent(_selected.RequestPayload);
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                if (_isFoldRequestPayload)
                {
                    GUILayout.Label(GetSelectedContent(_selected.RequestPayload), _bufferContentStyle);
                }

                GUILayout.BeginHorizontal();
                {
                    _isFoldResponse = GUILayout.Toggle(_isFoldResponse, GetFoldHeader("Response", _isFoldResponse),
                        EditorStyles.toolbarButton,
                        GUILayout.Width(FOLD_WIDTH));

                    if (GUILayout.Button("Copy", 
                        _toolbarButtonStyle, 
                        GUILayout.ExpandWidth(true),
                        GUILayout.MinWidth(128f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = GetSelectedContent(_selected.Response);
                    }

                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();

                if (_isFoldResponse)
                {
                    GUILayout.Label(GetSelectedContent(_selected.Response), _bufferContentStyle);
                }

                if (GUILayout.Button("Copy Packet", _buttonStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = _selected.ToString();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private string GetSelectedContent(string content)
        {
            try
            {
                if (!_pretty())
                    return content;

                var prettyFallback = content.ToPrettyJSONify();
                return prettyFallback;
            }
            catch
            {
                return string.Empty;
            }
        }

        public new void Dispose()
        {
            onGUIHandler -= OnDraw;

            base.Dispose();
        }
    }
}