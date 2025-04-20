using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Gddb.Editor
{
    public class TypeSearchPopup : PopupWindowContent
    {
        private readonly TypeSearchSettings _settings;
        private readonly IReadOnlyList<Type> _types;
        private readonly Rect _activatorRect;
        private readonly Action<Type> _onSelected;

        public TypeSearchPopup(   TypeSearchSettings settings, IReadOnlyList<Type> types, Rect activatorRect, Action<Type> onSelected )
        {
            _settings = settings;
            _types = types;
            _activatorRect = activatorRect;
            _onSelected = onSelected;
        }

        public override VisualElement CreateGUI( )
        {
            var widget = new TypeSearchWidget( _settings, _types );
            widget.Selected +=  selectedType  =>
            {
                _onSelected?.Invoke( selectedType );
                editorWindow.Close();
            };
            var result = widget.CreateGUI();
            result.style.width = _activatorRect.width;
            result.style.maxHeight = Screen.height / 2;
            return result;
        }

        public override void OnClose( )
        {
            base.OnClose();

            Closed?.Invoke();
        }

        /// <summary>
        /// When popup closed
        /// </summary>
        public event Action Closed;

        public delegate void OnSelected( TypeSearchPopup sender, Type selectedType );
    }
}