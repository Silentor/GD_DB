﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Popup window for browsing GdDb
    /// </summary>
    public class GdDbTreePopup : EditorWindow
    {
        public static GdDbTreePopup Open( GdDb db, [CanBeNull] String query, Type[] components, [CanBeNull] GDObject selectedObject, Rect dropDownRect )
        {
            if ( db == null ) throw new ArgumentNullException( nameof(db) );

            var treeBrowser = EditorWindow.GetWindow<GdDbTreePopup>( true, "Select GD Object", true );
            treeBrowser.Init( db, query, components, selectedObject );
            treeBrowser.ShowAsDropDown( dropDownRect, new Vector2( dropDownRect.width, 400 ) );
            return treeBrowser;
        }

        public event Action<GDObject> Selected;
        public event Action<GDObject> Chosed;

        private TreeView                       _tree;
        private Label                           _hintLabel;
        //private Folder                         _rootFolder;
        private List<TreeViewItemData<Object>> _treeItems;
        private TreeView                       _treeView;

        private void CreateGUI( )
        {
            _hintLabel = new Label( "No objects found" );
            rootVisualElement.Add( _hintLabel );

            _treeView                  =  new TreeView();
            _treeView.fixedItemHeight  =  18;
            _treeView.makeItem         =  () => Resources.TreeItemElem.Instantiate();
            _treeView.bindItem         =  BindItem;
            _treeView.unbindItem       =  UnbindItem;
            _treeView.destroyItem      =  elem => elem.Clear();
            _treeView.showBorder       =  true;
            _treeView.selectionType    =  SelectionType.Single;
            _treeView.selectionChanged += TreeView_selectionChanged;
            _treeView.itemsChosen      += TreeView_Chosed;

            rootVisualElement.Add( _treeView );
        }

        private void OnDestroy( )
        {
            Selected = null;
            Chosed   = null;
        }

        private void TreeView_selectionChanged(IEnumerable<Object> items )
        {
            if ( items.Any() )
            {
                var result = items.First();
                if( result is GDObject gdo )
                    Selected?.Invoke( gdo );
            }
        }

        private void TreeView_Chosed(IEnumerable<Object> items )
        {
            if ( items.Any() )
            {
                var result = items.First();
                if ( result is GDObject gdo )
                {
                    Chosed?.Invoke( gdo );
                    Close();
                }
            }
        }

        private void Init(   GdDb db, String query, Type[] components, ScriptableObject selectedObject )
        {
            GdFolder rootFolder = db.RootFolder;
            if ( !String.IsNullOrEmpty( query ) )
            {
                var objects = new List<ScriptableObject>();
                var folders = new List<GdFolder>();
                if( components != null && components.Length > 0 )
                    db.FindObjects( query, components, objects, folders );
                else
                    db.FindObjects( query, objects, folders );

                //Reconstruct tree from search result
                Dictionary<Guid, GdFolder> queryFolders = new ();

                for ( int i = 0; i < objects.Count; i++ )
                {
                    var obj = objects[ i ];
                    var folder = folders[ i ];
                    var tempFolder = GetTempFolder( folder );
                    tempFolder.Objects.Add( obj );
                }

                if( queryFolders.Count == 0 )
                {
                    _hintLabel.style.display = DisplayStyle.Flex;
                    var hintLabelText = $"No objects found for query string '{query}'";
                    if( components != null && components.Length > 0 )
                        hintLabelText += $" with components <{String.Join(", ", components.Select( c => c.Name ).ToArray())}>";

                    _hintLabel.text = hintLabelText;
                    return;
                }

                rootFolder = queryFolders.Values.First();

                var objCount = rootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).Count();
                Debug.Log( $"[GdDbTreeWindow] Show results for query '{query}', retrieved {objCount} objects" );

                GdFolder GetTempFolder( GdFolder originalFolder )
                {
                    if ( !queryFolders.TryGetValue( originalFolder.FolderGuid, out var tempFolder ) )
                    {
                        tempFolder = new GdFolder( originalFolder.Name, originalFolder.FolderGuid)
                                     {
                                             Depth      = originalFolder.Depth,
                                             Parent = originalFolder.Parent != null ? GetTempFolder( originalFolder.Parent )  : null,
                                     };
                        if( tempFolder.Parent != null )
                            tempFolder.Parent.SubFolders.Add( tempFolder );
                        queryFolders.Add( tempFolder.FolderGuid, tempFolder );
                    }

                    return tempFolder;
                }
            }

            _hintLabel.style.display = DisplayStyle.None;
            var rootTreeItem         = PrepareTreeRoot( rootFolder );
            _treeView.SetRootItems( new []{ rootTreeItem } );
            _treeView.Rebuild();
            _treeView.ExpandAll();
            
            var selectedTreeItem = FindTreeItemData( rootTreeItem, o => o is GDObject gdo && gdo == selectedObject );
            if( selectedTreeItem.data != null )
                _treeView.SetSelectionById( selectedTreeItem.id );
        }

       

        private TreeViewItemData<Object> PrepareTreeRoot( GdFolder rootFolder )
        {
            var rootItem = PrepareTreeItem( rootFolder );
            return rootItem;
        }

        private TreeViewItemData<Object> PrepareTreeItem( GdFolder folder )
        {
            var childs = new List<TreeViewItemData<Object>>();
            var result = new TreeViewItemData<Object>( folder.GetHashCode(), folder, childs );

            foreach ( var subFolder in folder.SubFolders )
            {
                var subfolderItem = PrepareTreeItem( subFolder );
                childs.Add( subfolderItem );
            }

            foreach ( var gdo in folder.Objects )
            {
                var objectTreeItem = new TreeViewItemData<Object>( GetGDObjectHash( gdo), gdo );
                childs.Add( objectTreeItem );
            }

            return result;
        }

        private void BindItem(VisualElement elem, Int32 index )
        {
            var item  = _treeView.GetItemDataForIndex<Object>( index );
            var icon = elem.Q<VisualElement>( "Icon" );
            var label = elem.Q<Label>( "Label" );
            if ( item is GdFolder folder )
            {
                icon.style.backgroundImage = Resources.FolderIcon;
                label.text = folder.Name;
            }
            else if ( item is GDRoot root )
            {
                icon.style.backgroundImage = Resources.DatabaseIcon;
                label.text                 = $"{root.Name} ({root.Id})";
            }
            else if ( item is GDObject gdo )
            {
                icon.style.backgroundImage = Resources.ObjectIcon;
                label.text                 = gdo.Name;
            }
        }

        private void UnbindItem(VisualElement elem, Int32 index )
        {
            //throw new NotImplementedException();
        }

        private TreeViewItemData<Object> FindTreeItemData( TreeViewItemData<Object> root, Predicate<Object> predicate )
        {
            if( predicate( root.data ) )
                return root;

            foreach ( var viewItemChild in root.children )
            {
                var result = FindTreeItemData( viewItemChild, predicate );
                if ( result.data != null )
                    return result;
            }

            return default;
        }

        private Int32 GetGDObjectHash( ScriptableObject obj )
        {
            if( obj is GDObject gdo )
                return gdo.Guid.GetHashCode();
            else if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( obj, out var guidStr, out Int64 _ ) )
                return guidStr.GetHashCode();
            else
                return obj.GetInstanceID();
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset TreeItemElem = UnityEngine.Resources.Load<VisualTreeAsset>( "GdTreeItem" );
            public static readonly Texture2D FolderIcon = UnityEngine.Resources.Load<Texture2D>( "folder_24dp" );
            public static readonly Texture2D ObjectIcon = UnityEngine.Resources.Load<Texture2D>( "description_24dp" );
            public static readonly Texture2D DatabaseIcon = UnityEngine.Resources.Load<Texture2D>( "database_24dp" );
        }

    }
}