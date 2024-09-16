using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    public class GdDbTreeWindow : EditorWindow
    {
        public static GdDbTreeWindow Open( GdDb db, [CanBeNull] String query, Type[] components, [CanBeNull] GDObject selectedObject, Rect dropDownRect )
        {
            if ( db == null ) throw new ArgumentNullException( nameof(db) );

            var treeBrowser = EditorWindow.GetWindow<GdDbTreeWindow>( true, "Gddb", true );
            treeBrowser.Init( db, query, components, selectedObject );
            treeBrowser.ShowAsDropDown( dropDownRect, new Vector2( dropDownRect.width, 400 ) );
            return treeBrowser;
        }

        public event Action<GDObject> Selected;
        public event Action<GDObject> Chosed;

        private TreeView                       _tree;
        //private Folder                         _rootFolder;
        private List<TreeViewItemData<Object>> _treeItems;
        private TreeView                       _treeView;

        private void CreateGUI( )
        {
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

        private void Init(   GdDb db, String query, Type[] components, GDObject selectedObject )
        {
            Folder rootFolder = db.RootFolder;
            if ( !String.IsNullOrEmpty( query ) )
            {
                var result = components != null ? db.GetObjectsAndFolders( query, components.First() ) : db.GetObjectsAndFolders( query );
                //Reconstruct tree from search result
                Dictionary<Guid, Folder> queryFolders = new ();

                foreach ( var (folder, obj) in result  )
                {
                     var tempFolder = GetTempFolder( folder );
                     tempFolder.Objects.Add( obj );
                }

                rootFolder = queryFolders.Values.First();

                var objCount = rootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).Count();
                Debug.Log( $"[GdDbTreeWindow] Show results for query '{query}', retrieved {objCount} objects" );

                Folder GetTempFolder( Folder originalFolder )
                {
                    if ( !queryFolders.TryGetValue( originalFolder.FolderGuid, out var tempFolder ) )
                    {
                        tempFolder = new Folder( originalFolder.Path, originalFolder.Name, originalFolder.FolderGuid)
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
            

            var rootTreeItem         = PrepareTreeRoot( rootFolder );
            _treeView.SetRootItems( new []{ rootTreeItem } );
            _treeView.Rebuild();
            _treeView.ExpandAll();
            
            var selectedTreeItem = FindTreeItemData( rootTreeItem, o => o is GDObject gdo && gdo == selectedObject );
            if( selectedTreeItem.data != null )
                _treeView.SetSelectionById( selectedTreeItem.id );
        }

       

        private TreeViewItemData<Object> PrepareTreeRoot( Folder rootFolder )
        {
            var rootItem = PrepareTreeItem( rootFolder );
            return rootItem;
        }

        private TreeViewItemData<Object> PrepareTreeItem( Folder folder )
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
                var objectTreeItem = new TreeViewItemData<Object>( gdo.Guid.GetHashCode(), gdo );
                childs.Add( objectTreeItem );
            }

            return result;
        }

        private void BindItem(VisualElement elem, Int32 index )
        {
            var item  = _treeView.GetItemDataForIndex<Object>( index );
            var icon = elem.Q<VisualElement>( "Icon" );
            var label = elem.Q<Label>( "Label" );
            if ( item is Folder folder )
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

        private static class Resources
        {
            public static readonly VisualTreeAsset TreeItemElem = UnityEngine.Resources.Load<VisualTreeAsset>( "GdTreeItem" );
            public static readonly Texture2D FolderIcon = UnityEngine.Resources.Load<Texture2D>( "folder_24dp" );
            public static readonly Texture2D ObjectIcon = UnityEngine.Resources.Load<Texture2D>( "description_24dp" );
            public static readonly Texture2D DatabaseIcon = UnityEngine.Resources.Load<Texture2D>( "database_24dp" );
        }

    }
}