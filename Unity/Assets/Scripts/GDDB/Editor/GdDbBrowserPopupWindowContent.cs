using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Adapter for <see cref="GdDbBrowserWidget"/> to use as PopupWindow content
    /// </summary>
    public class GdDbBrowserPopupWindowContent : PopupWindowContent
    {
        private readonly GdDb                    _db;
        private readonly String                  _query;
        private readonly Type[]                  _components;
        private readonly Object                  _selectedObject;
        private readonly Rect                    _activatorRect;
        private readonly GdDbBrowserWidget.EMode _mode;
        private readonly OnSelected              _onSelected;
        private readonly OnSelected              _onChosed;
        private          GdDbBrowserWidget       _widget;
        private          Vector2                 _ownerWindowSize;

        public GdDbBrowserPopupWindowContent( GdDb db, String query, Type[] components, Object selectedObject, Rect activatorRect, GdDbBrowserWidget.EMode mode, OnSelected onSelected, OnSelected onChosed )
        {
            _db             = db;
            _query          = query;
            _components     = components;
            _selectedObject = selectedObject;
            _activatorRect  = activatorRect;
            _mode      = mode;
            _onSelected     = onSelected;
            _onChosed       = onChosed;
        }

        public override void OnOpen( )          //TODO add support for Unity 6 CreateGUI
        {
            if ( _mode == GdDbBrowserWidget.EMode.ObjectsAndFolders )
            {
                if ( !String.IsNullOrEmpty( _query ) || (_components != null ) )
                {
                    var resultObjects = new List<ScriptableObject>();
                    var resultFolders = new List<GdFolder>();
                    _db.FindObjects( _query, _components, resultObjects, resultFolders );
                    _widget = new GdDbBrowserWidget( _db, resultObjects, resultFolders, _selectedObject );
                }
                else
                    _widget = new GdDbBrowserWidget( _db, _selectedObject, GdDbBrowserWidget.EMode.ObjectsAndFolders );
            }
            else
            {
                if ( !String.IsNullOrEmpty( _query ) )
                {
                    var resultFolders = new List<GdFolder>();
                    _db.FindFolders( _query, resultFolders );
                    _widget = new GdDbBrowserWidget( _db, resultFolders, _selectedObject );
                }
                else
                    _widget = new GdDbBrowserWidget( _db, _selectedObject, GdDbBrowserWidget.EMode.Folders );
            }

            _widget.CreateGUI( editorWindow.rootVisualElement );
            if( _onSelected != null )
                _widget.Selected += ( folder, gdObject ) => _onSelected( this, folder, gdObject );
            if( _onChosed != null )
                _widget.Chosed += ( folder, gdObject ) => _onChosed( this, folder, gdObject );
        }

        public override void OnClose( )
        {
            Closed?.Invoke();
        }

        public override Vector2 GetWindowSize( )
        {
            return new Vector2( _activatorRect.width, 200 ); //TODO modify height based on number of items
            
        }

        public event Action Closed;

        public delegate void OnSelected( GdDbBrowserPopupWindowContent sender, GdFolder folder, ScriptableObject gdObject );
    }
}