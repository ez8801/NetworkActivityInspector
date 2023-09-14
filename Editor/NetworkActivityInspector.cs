using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using EZ.Network.Editor.View;
using System.Linq;

namespace EZ.Network.Editor
{
    public class NetworkActivityInspector : EditorWindow, MenuBar.IEventListener
    {
        private static NetworkActivityInspector s_window = null;

        private int _prevCount;
        private float _realtimeSinceStartup;

        private bool _isDirty;

        private List<PacketLog> _logs;
        private List<PacketLog> _filteredLogs;
        
        private PacketLog _selected;
        private int _selectedIndex;

        private MenuBar _menuBar;
        private ListView _listView;
        private LogDetailView _logDetailView;

        public int LogCount => _filteredLogs?.Count ?? 0;

        public class Model
        {
            public bool IsClearOnStop = false;
            public bool IsPretty = true;
            public bool IsUpdateWhenChanged = true;
            public bool IsScrollLock = false;

            public string SearchKeyword = "";

            public Protocol ProtocolFlags = Protocol.Everything;
        }

        private Model _model = new Model();

        [MenuItem("Tools/Inspect Network Activity %F1", false, 1)]
        static public void Open()
        {
            if (s_window == null)
            {
                s_window = EditorWindow.GetWindow<NetworkActivityInspector>(false, "Network Activity Inspector", true);
                if (s_window != null)
                    s_window.Initialize(s_window.rootVisualElement);
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void RegistCallback()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void Initialize(VisualElement rootVisualElement)
        {
            _prevCount = 0;
            _realtimeSinceStartup = Time.realtimeSinceStartup;

            _selected = PacketLog.Default;
            _selectedIndex = -1;

            NetworkActivity.webRequestRequested += OnRequest;
            NetworkActivity.webResponseReceived += OnReceived;

            UpdateLogs();

            _menuBar = new MenuBar(_model);
            _menuBar.SetOnEventListener(this);
            rootVisualElement.Add(_menuBar);

            var splitView = new TwoPaneSplitView(0, 350, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            var leftPane = new VisualElement();
            _listView = new ListView();
            _listView.makeItem = () => new Label();
            _listView.itemsSource = _filteredLogs;
            _listView.bindItem = (item, index) => {
                if (!(index < 0 || index >= LogCount))
                {
                    var packetLog = _filteredLogs[index] as PacketLog;
                    (item as Label).text = packetLog.Url;
                }                
            };
            _listView.unbindItem = (item, index) =>
            {
                (item as Label).text = string.Empty;
            };
            _listView.destroyItem = (item) =>
            {
                
            };
            _listView.onSelectedIndicesChange += (e) =>
            {
                var selectedId = e.FirstOrDefault();
                if (selectedId < 0 || selectedId >= LogCount)
                {
                    _selected = PacketLog.Default;
                    _selectedIndex = -1;
                }
                else
                {
                    _selected = _filteredLogs[selectedId] as PacketLog;
                    _selectedIndex = selectedId;
                }
                _logDetailView.SetSelected(_selected);
            };
            leftPane.Add(_listView);
            splitView.Add(leftPane);

            var rightPane = new VisualElement();
            _logDetailView = new LogDetailView(() => _model.IsPretty);
            rightPane.Add(_logDetailView);
            splitView.Add(rightPane);
        }

        private void OnRequest(PacketLog packetLog)
        {
            _isDirty = true;
        }

        private void OnReceived()
        {
            _isDirty = true;
        }

        private void Reload()
        {
            _listView.RefreshItems();
            rootVisualElement.MarkDirtyRepaint();
            Repaint();
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
                            _logDetailView?.SetSelected(_selected);
                            Reload();
                            return true;
                        case KeyCode.DownArrow:
                            _selectedIndex = Mathf.Min(++_selectedIndex, LogCount - 1);
                            _selected = _filteredLogs[_selectedIndex];
                            _logDetailView?.SetSelected(_selected);
                            Reload();
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

            if (s_window == null)
                Open();
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

                _prevCount = LogCount;
                _realtimeSinceStartup = Time.realtimeSinceStartup;

                _isDirty = false;

                if (_model.IsUpdateWhenChanged)
                {
                    if (_model.IsScrollLock == false)
                    {
                        var logCount = LogCount;
                        if (logCount > 0)
                            _listView.ScrollToItem(logCount - 1);
                    }

                    Reload();
                }
            }
        }

        private bool IsValid(PacketLog packetLog)
        {
            var isContains = packetLog.Url.Contains(_model.SearchKeyword);
            if (isContains == false)
            {
                if (!string.IsNullOrEmpty(packetLog.RequestPayload))
                    isContains = packetLog.RequestPayload.Contains(_model.SearchKeyword);
            }

            if (isContains == false)
            {
                if (!string.IsNullOrEmpty(packetLog.Response))
                    isContains = packetLog.Response.Contains(_model.SearchKeyword);
            }
            return isContains;
        }

        private void UpdateLogs()
        {
            _logs = NetworkActivityLogger.Filter(_model.ProtocolFlags);
            if (_filteredLogs == null)
                _filteredLogs = new List<PacketLog>();

            if (!(_logs == null || _logs.Count == 0))
            {
                if (string.IsNullOrEmpty(_model.SearchKeyword))
                {
                    _filteredLogs.Clear();
                    _filteredLogs.AddRange(_logs);
                }
                else
                    _filteredLogs = _logs.FindAll(IsValid);                
            }
            else
            {
                _filteredLogs.Clear();
            }
        }

        private void OnExitPlayMode()
        {
            if (_model.IsClearOnStop)
            {
                Clear();
                Reload();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                if (s_window != null)
                    s_window.OnExitPlayMode();
            }
        }

        private void Fold()
        {
            _logDetailView.Fold();            
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
            s_window = this;
        }

        private void OnDisable()
        {
            if (_model.IsClearOnStop)
            {
                Clear();
            }
        }

        private void OnEnable()
        {
            
        }

        #endregion Callbacks

        void MenuBar.IEventListener.OnClear()
        {
            Clear();
        }

        void MenuBar.IEventListener.OnFold()
        {
            Fold();
        }

        void MenuBar.IEventListener.OnProtocolFlagChanged()
        {
            UpdateLogs();
            Reload();
        }

        void MenuBar.IEventListener.OnSearchingKeywordChanged(string keyword)
        {
            UpdateLogs();
            Reload();
        }
    }
}