using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EZ.Network.Editor.View
{
    public class MenuBar : IMGUIContainer
    {
        private GUIStyle _dropDownStyle;

        public interface IEventListener
        {
            void OnClear();
            void OnFold();
            void OnProtocolFlagChanged();
            void OnSearchingKeywordChanged(string keyword);
        }

        private IEventListener _eventListener;

        private NetworkActivityInspector.Model _model;

        public MenuBar(NetworkActivityInspector.Model model)
        {
            onGUIHandler += OnDraw;

            _model = model;

            _dropDownStyle = EditorStyles.toolbarDropDown;
            _dropDownStyle.fixedWidth = 100f;
        }

        void OnDraw()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.MaxWidth(82f)))
                _eventListener?.OnClear();

            if (GUILayout.Button("Fold", EditorStyles.toolbarButton, GUILayout.MaxWidth(82f)))
                _eventListener?.OnFold();
            
            _model.IsClearOnStop = GUILayout.Toggle(_model.IsClearOnStop, 
                "Clear on Stop", 
                EditorStyles.toolbarButton, 
                GUILayout.MaxWidth(116f));

            _model.IsPretty = GUILayout.Toggle(_model.IsPretty, 
                "Pretty", 
                EditorStyles.toolbarButton);

            var changedProtocolFlags = (Protocol)EditorGUILayout.EnumFlagsField(_model.ProtocolFlags, _dropDownStyle);
            if (_model.ProtocolFlags != changedProtocolFlags)
            {
                _model.ProtocolFlags = changedProtocolFlags;
                _eventListener?.OnProtocolFlagChanged();
            }

            var searchKeyWord = EditorGUILayout.TextField(_model.SearchKeyword, EditorStyles.toolbarSearchField);
            if (_model.SearchKeyword != searchKeyWord)
            {
                _model.SearchKeyword = searchKeyWord;
                _eventListener?.OnSearchingKeywordChanged(searchKeyWord);
            }

            _model.IsUpdateWhenChanged = GUILayout.Toggle(_model.IsUpdateWhenChanged, 
                "Update When Changed", 
                EditorStyles.toolbarButton, 
                GUILayout.MaxWidth(146f));

            _model.IsScrollLock = GUILayout.Toggle(_model.IsScrollLock, 
                "Scroll Lock", 
                EditorStyles.toolbarButton, 
                GUILayout.MaxWidth(100f));

            GUILayout.EndHorizontal();
        }

        public void SetOnEventListener(IEventListener l)
        {
            _eventListener = l;
        }

        public new void Dispose()
        {
            onGUIHandler -= OnDraw;

            base.Dispose();
        }
    }
}