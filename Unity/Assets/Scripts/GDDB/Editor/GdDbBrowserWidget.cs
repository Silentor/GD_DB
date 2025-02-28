using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GDDB.Queries;
using JetBrains.Annotations;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Widget for browsing Gddb objects and/or folders. Can be embedded to popup window or editor window
    /// </summary>
    public class GdDbBrowserWidget
    {
        public GdDbBrowserWidget( GdDb db, [CanBeNull] String query, Type[] components, [CanBeNull] Object selectedObject, EMode mode = EMode.ObjectsAndFolders )
        {
            _db               = db;
            _query            = query;
            _components       = components;
            _selectedObject = selectedObject;
            _mode        = mode;
            _queryExecutor = new Executor( _db );
            _queryParser   = new Parser( _queryExecutor );
        }

        public event Action<GdFolder, ScriptableObject> Selected;
        public event Action<GdFolder, ScriptableObject> Chosed;

        public void CreateGUI( VisualElement root )
        {
            var content = Resources.BrowserWidget.Instantiate();

            _treeView                  =  content.Q<TreeView>( "BrowserTreeView" );
            _treeView.makeItem         =  () => Resources.TreeItemElem.Instantiate();
            _treeView.bindItem         =  BindItem;
            _treeView.unbindItem       =  UnbindItem;
            _treeView.destroyItem      =  elem => elem.Clear();
            _treeView.selectionChanged += TreeView_selectionChanged;
            _treeView.itemsChosen      += TreeView_Chosed;

            _toolBar      = content.Q<VisualElement>( "ToolBar" );
            _queryTextBox = _toolBar.Q<TextField>( "QueryTxt" );
            _queryTextBox.RegisterValueChangedCallback( evt => SearchAsync( evt.newValue, _rootFolder ) );
            _statsLbl     = content.Q<Label>( "StatsLbl" );

            root.Add( content );

            if( _mode == EMode.ObjectsAndFolders )
                InitObjectsBrowser( _db, _query, _components, _selectedObject as ScriptableObject ); 
            else
                InitFoldersBrowser( _db, _query, _selectedObject as GdFolder );
        }

        //Init values
        private readonly GdDb   _db;
        private readonly String _query;
        private readonly Type[] _components;
        private readonly Object _selectedObject;
        private readonly EMode  _mode;

        private GdFolder                        _rootFolder; //Root folder for the initial search query. It can be queried further 
        private List<TreeViewItemData<Object>> _treeItems;
        private TreeView                       _treeView;
        private TextField                      _queryTextBox;
        private Label                          _statsLbl;
        private VisualElement                  _toolBar;


        private void TreeView_selectionChanged(IEnumerable<Object> items )
        {
            var selectedItem = items.FirstOrDefault();
            if ( _mode == EMode.ObjectsAndFolders )
            {
                if ( selectedItem is ScriptableObject gdo )
                {
                    var folder = _db.RootFolder.EnumerateFoldersDFS().FirstOrDefault( f => f.Objects.Contains( gdo ) );
                    Selected?.Invoke( folder, gdo );
                }
            }
            else
            {
                if ( selectedItem is GdFolder folder )
                {
                    Selected?.Invoke( folder, null );
                }
            }
        }

        private void TreeView_Chosed(IEnumerable<Object> items )
        {
            var selectedItem = items.FirstOrDefault();
            if ( _mode == EMode.ObjectsAndFolders )
            {
                if ( selectedItem is ScriptableObject gdo )
                {
                    var folder = _db.RootFolder.EnumerateFoldersDFS().FirstOrDefault( f => f.Objects.Contains( gdo ) );
                    Chosed?.Invoke( folder, gdo );
                }
            }
            else
            {
                if ( selectedItem is GdFolder folder )
                {
                    Chosed?.Invoke( folder, null );
                }
            }
        }

        private void InitFoldersBrowser(   GdDb db, String query, GdFolder selectedFolder )
        {
            if ( !String.IsNullOrEmpty( query ) )
            {
                var folders = new List<GdFolder>();
                db.FindFolders( query, folders );
                
                if( folders.Count == 0 )
                {
                    var hintLabelText = $"No folders found for query string '{query}'";

                    _rootFolder    = null;
                    _statsLbl.text = hintLabelText;
                    //Initial query limits me to zero objects, so hide the tree
                    _toolBar.style.display  = DisplayStyle.None;
                    _treeView.style.display = DisplayStyle.None;
                    return;
                }

                _rootFolder = ConvertSearchResultToHierarchy( Array.Empty<ScriptableObject>(), folders, true );
            }
            else
            {
                //Create copy of entire DB folders structure without objects
                _rootFolder = ConvertSearchResultToHierarchy( Array.Empty<ScriptableObject>(), db.RootFolder.EnumerateFoldersDFS(  ).ToArray(), true );
            }

            _statsLbl.text = $"Folders found: {_rootFolder.EnumerateFoldersDFS().Count()}";

            var rootTreeItem         = PrepareTreeRoot( _rootFolder );
            _treeView.SetRootItems( new []{ rootTreeItem } );
            _treeView.Rebuild();
            _treeView.ExpandAll();
            
            var selectedTreeItem = FindTreeItemData( rootTreeItem, o => o is GdFolder f && f == selectedFolder );
            if( selectedTreeItem.data != null )
                _treeView.SetSelectionById( selectedTreeItem.id );
        }
        

        private void InitObjectsBrowser(   GdDb db, String query, Type[] components, ScriptableObject selectedObject )
        {
            _rootFolder = db.RootFolder;
            if ( !String.IsNullOrEmpty( query ) )
            {
                var objects = new List<ScriptableObject>();
                var folders = new List<GdFolder>();
                db.FindObjects( query, components, objects, folders );
                
                if( folders.Count == 0 )
                {
                    var hintLabelText = $"No objects found for query string '{query}'";
                    if( components != null && components.Length > 0 )
                        hintLabelText += $" with components <{String.Join(", ", components.Select( c => c.Name ).ToArray())}>";

                    _rootFolder    = null;
                    _statsLbl.text = hintLabelText;
                    //Initial query limits me to zero objects, so hide the tree
                    _toolBar.style.display = DisplayStyle.None;
                    _treeView.style.display = DisplayStyle.None;
                    return;
                }

                _rootFolder = ConvertSearchResultToHierarchy( objects, folders, true );
            }

            _statsLbl.text = $"Object found: {_rootFolder.EnumerateFoldersDFS().SelectMany( f => f.Objects ).Count()}";

            var rootTreeItem         = PrepareTreeRoot( _rootFolder );
            _treeView.SetRootItems( new []{ rootTreeItem } );
            _treeView.Rebuild();
            _treeView.ExpandAll();
            
            var selectedTreeItem = FindTreeItemData( rootTreeItem, o => o is GDObject gdo && gdo == selectedObject );
            if( selectedTreeItem.data != null )
                _treeView.SetSelectionById( selectedTreeItem.id );
        }

        private readonly EditorWaitForSeconds _searchDelay = new ( 0.3f );
        private          EditorCoroutine      _searchDelayCoroutine;
        private readonly Executor             _queryExecutor;
        private readonly Parser               _queryParser;

        private void SearchAsync( String query, GdFolder rootFolder )
        {
            if ( _searchDelayCoroutine != null )                
                EditorCoroutineUtility.StopCoroutine( _searchDelayCoroutine );

            _searchDelayCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless( SearchWithDelayCoroutine( query, rootFolder ) );
        }

        private IEnumerator SearchWithDelayCoroutine( String query, GdFolder rootFolder )
        {
            yield return _searchDelay;
            
            _searchDelayCoroutine = null;
            var resultRootFolder = SearchInternal( query, rootFolder );

            if ( resultRootFolder == null )
            {
                _treeView.SetRootItems( Array.Empty<TreeViewItemData<Object>>() );
                _treeView.Rebuild();
                _statsLbl.text = _mode == EMode.ObjectsAndFolders ? "No objects found" : "No folders found";
            }
            else
            {
                var objectsFound = resultRootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).Count();
                var rootTreeItem         = PrepareTreeRoot( resultRootFolder );
                _treeView.SetRootItems( new []{ rootTreeItem } );
                _treeView.Rebuild();
                _treeView.ExpandAll();
                _statsLbl.text = _mode == EMode.ObjectsAndFolders ? $"Object found: {objectsFound}" : $"Folders found: {resultRootFolder.EnumerateFoldersDFS().Count()}";
            }
        }

        private GdFolder SearchInternal( String query, GdFolder rootFolder )
        {
            if ( String.IsNullOrEmpty( query ) )
                return rootFolder;

            var objects = new List<ScriptableObject>();
            var folders = new List<GdFolder>();

            if( _mode == EMode.ObjectsAndFolders )
            {
                var queryTokens = _queryParser.ParseObjectsQuery( query );
                _queryExecutor.FindObjects( queryTokens, rootFolder, objects, folders );
            }
            else
            {
                var queryTokens = _queryParser.ParseFoldersQuery( query );
                _queryExecutor.FindFolders( queryTokens, rootFolder, folders );
            }

            var resultRootFolder = ConvertSearchResultToHierarchy( objects, folders, false );

            return resultRootFolder;
        }

        //Reconstruct objects/folders tree from flat search result
        private GdFolder  ConvertSearchResultToHierarchy( IReadOnlyList<ScriptableObject> objects, IReadOnlyList<GdFolder> folders, Boolean removeEmptyTopFolders )
        {
            Dictionary<Guid, GdFolder> tempFolders = new ();

            if ( folders.Count == 0 )
                return null;

            if( _mode == EMode.ObjectsAndFolders )
            {
                for ( int i = 0; i < objects.Count; i++ )
                {
                    var obj        = objects[ i ];
                    var folder     = folders[ i ];
                    var tempFolder = GetTempFolder( folder );
                    tempFolder.Objects.Add( obj );
                }

                //Remove root folder if no meaningful content
                var rootFolder = tempFolders.Values.First();
                if ( rootFolder == _db.RootFolder && rootFolder.SubFolders.Count == 1 && rootFolder.Objects.Count == 0 )
                {
                    rootFolder        = rootFolder.SubFolders[ 0 ];
                    rootFolder.Parent = null;
                }
            }
            else
            {
                for ( int i = 0; i < folders.Count; i++ )
                {
                    var folder     = folders[ i ];
                    var tempFolder = GetTempFolder( folder );
                }
            }

            GdFolder GetTempFolder( GdFolder originalFolder )
            {
                if ( !tempFolders.TryGetValue( originalFolder.FolderGuid, out var tempFolder ) )
                {
                    tempFolder = new GdFolder( originalFolder.Name, originalFolder.FolderGuid)
                                 {
                                         Depth  = originalFolder.Depth,
                                         Parent = originalFolder.Parent != null ? GetTempFolder( originalFolder.Parent )  : null,
                                 };
                    if( tempFolder.Parent != null )
                        tempFolder.Parent.SubFolders.Add( tempFolder );
                    tempFolders.Add( tempFolder.FolderGuid, tempFolder );
                }

                return tempFolder;
            }

            var rootFolder = tempFolders.Values.First();

            // if( removeEmptyTopFolders )                             //Truncate top level folders without objects
            // {
            //     while ( rootFolder.SubFolders.Count == 1 && rootFolder.Objects.Count == 0 )
            //     {
            //         rootFolder        = rootFolder.SubFolders.First();
            //         rootFolder.Parent = null;
            //     }
            // }
            return rootFolder;
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
                icon.style.backgroundImage = Resources.GDObjectIcon;
                label.text                 = gdo.name;
            }
            else if ( item is ScriptableObject so )
            {
                icon.style.backgroundImage = Resources.GDObjectIcon;
                label.text                 = so.name;
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
            return obj.GetInstanceID();
        }

        //TODO Unity 6 added expand item event so we can adjust parent window height to better fit the tree
        //Unfortunately, Unity 2022.3 doest support this event, so its not implemented yet
        private Int32 GetVisibleItemsInTreeView( TreeViewItemData<Object> root )
        {
            Int32 result = 0;

            GetVisibleItemsInTreeViewRecursive( root, ref result );
            return result;
        }

        private void GetVisibleItemsInTreeViewRecursive( TreeViewItemData<Object> root, ref Int32 result )
        {
            if( _treeView.IsExpanded( root.id ) )
            {
                foreach ( var child in root.children )                
                    GetVisibleItemsInTreeViewRecursive( child, ref result );
                result++;
            }
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset BrowserWidget = UnityEngine.Resources.Load<VisualTreeAsset>( "GdDbBrowser" );
            public static readonly VisualTreeAsset TreeItemElem = UnityEngine.Resources.Load<VisualTreeAsset>( "GdTreeItem" );
            public static readonly Texture2D FolderIcon = UnityEngine.Resources.Load<Texture2D>( "folder_24dp" );
            public static readonly Texture2D GDObjectIcon = UnityEngine.Resources.Load<Texture2D>( "description_24dp" );
            public static readonly Texture2D SObjectIcon = UnityEngine.Resources.Load<Texture2D>( "description_24dp" );
            public static readonly Texture2D DatabaseIcon = UnityEngine.Resources.Load<Texture2D>( "database_24dp" );
        }

        public enum EMode
        {
            ObjectsAndFolders,
            Folders,
        }

    }

    public class GDDBBrowserTestWindow : EditorWindow
    {
        private GdDbBrowserWidget _browser;

        [MenuItem("GDDB/Show GdDb Browser")]
        public static void ShowWindow( )
        {
            var window = GetWindow<GDDBBrowserTestWindow>();
            window.titleContent = new GUIContent( "GdDb Browser" );
            window.Show();
        }

        private void OnEnable( )
        {
            var db = GDDBEditor.DB;
            _browser = new GdDbBrowserWidget( db, null, null, null );
            _browser.CreateGUI( rootVisualElement );
        }

        private void OnDisable( )
        {
            _browser = null;
        }
    }
}