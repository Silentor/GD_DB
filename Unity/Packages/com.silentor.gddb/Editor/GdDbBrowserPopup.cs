using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Gddb.Editor
{
    /// <summary>
    /// Adapter for <see cref="GdDbBrowserWidget"/> to use as PopupWindow content
    /// </summary>
    public class GdDbBrowserPopup : PopupWindowContent
    {
        private readonly GdDb                            _db;
        private readonly IReadOnlyList<ScriptableObject> _objects;
        private readonly IReadOnlyList<GdFolder>         _folders;
        private readonly Object                          _selectedObject;
        private readonly Rect                            _activatorRect;
        private readonly Boolean                         _showClearButton;
        private readonly GdDbBrowserWidget.EMode         _mode;
        private readonly OnSelected                      _onSelected;
        private readonly OnSelected                      _onChosed;
        private          GdDbBrowserWidget               _widget;
        private          Vector2                         _ownerWindowSize;

        public GdDbBrowserPopup( GdDb db, IReadOnlyList<ScriptableObject> objects, IReadOnlyList<GdFolder> folders, Object selectedObject, Rect activatorRect, Boolean showClearButton, GdDbBrowserWidget.EMode mode, OnSelected onSelected, OnSelected onChosed )
        {
            _db                   = db;
            _objects              = objects;
            _folders              = folders;
            _selectedObject       = selectedObject;
            _activatorRect        = activatorRect;
            _showClearButton = showClearButton;
            _mode                 = mode;
            _onSelected           = onSelected;
            _onChosed             = onChosed;
        }

        public override VisualElement CreateGUI( )         
        {
            if ( _mode == GdDbBrowserWidget.EMode.Objects )
            {
                if ( _objects == null )
                    _widget = new GdDbBrowserWidget( _db, _selectedObject, _showClearButton );
                else
                    _widget = new GdDbBrowserWidget( _db, _objects, _folders, (ScriptableObject)_selectedObject, _showClearButton );
            }
            else
            {
                if( _folders == null )
                    _widget = new GdDbBrowserWidget( _db, _selectedObject, _showClearButton, GdDbBrowserWidget.EMode.Folders );
                else
                    _widget = new GdDbBrowserWidget( _db, _folders, (GdFolder)_selectedObject, _showClearButton );
            }

            if( _onSelected != null )
                _widget.Selected += ( folder, gdObject ) => _onSelected( this, folder, gdObject );
            if( _onChosed != null )
                _widget.Chosed += ( folder, gdObject ) => _onChosed( this, folder, gdObject );

            var result = new VisualElement();
            result.style.width     = _activatorRect.width;
            result.style.maxHeight = Screen.height  / 2;
            _widget.CreateGUI( result );
            return result;
        }

        public override void OnClose( )
        {
            Closed?.Invoke();
        }

        public event Action Closed;

        public delegate void OnSelected( GdDbBrowserPopup sender, GdFolder folder, ScriptableObject gdObject );
    }

    
}