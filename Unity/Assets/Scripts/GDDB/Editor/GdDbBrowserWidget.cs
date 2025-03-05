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
        /// <summary>
        /// Constructor for browsing selected gd objects and folders 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="objects">Should align <see cref="folders"/> collection</param>
        /// <param name="folders">Should align <see cref="objects"/> collection</param>
        /// <param name="selectedObject"></param>
        /// <param name="mode"></param>
        public GdDbBrowserWidget( GdDb db, [NotNull] IReadOnlyList<ScriptableObject> objects, [NotNull] IReadOnlyList<GdFolder> folders, [CanBeNull] Object selectedObject ) : this( db, selectedObject, EMode.ObjectsAndFolders )
        {
            _folders = folders ?? throw new ArgumentNullException( nameof(folders) );
            _objects = objects ?? throw new ArgumentNullException( nameof(objects) );
        }

        /// <summary>
        /// Constructor for browsing selected gd folders 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="folders"></param>
        /// <param name="objects"></param>
        /// <param name="selectedObject"></param>
        /// <param name="mode"></param>
        public GdDbBrowserWidget( GdDb db, [NotNull] IReadOnlyList<GdFolder> folders, [CanBeNull] Object selectedObject ) :this( db, selectedObject, EMode.Folders )
        {
            _folders        = folders ?? throw new ArgumentNullException( nameof(folders) );
        }


        /// <summary>
        /// Constructor for browsing entire DB
        /// </summary>
        /// <param name="db"></param>
        /// <param name="selectedObject"></param>
        /// <param name="mode"></param>
        public GdDbBrowserWidget( [NotNull] GdDb db, [CanBeNull] Object selectedObject, EMode mode = EMode.ObjectsAndFolders )
        {
            _db             = db ?? throw new ArgumentNullException( nameof(db) );
            _selectedObject = selectedObject;
            _mode           = mode;
            _queryExecutor  = new Executor( _db );
            _queryParser    = new Parser( _queryExecutor );
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
                InitObjectsBrowser( _db, _objects, _folders, _selectedObject as ScriptableObject ); 
            else
                InitFoldersBrowser( _db, _folders, _selectedObject as GdFolder );
        }

        //Init values
        private readonly GdDb            _db;
        private readonly IReadOnlyList<GdFolder> _folders;
        private readonly IReadOnlyList<ScriptableObject> _objects;
        private readonly Object          _selectedObject;
        private readonly EMode           _mode;

        private GdFolder                        _rootFolder; //Root folder for the initial search query. It can be queried further 
        private List<TreeViewItemData<Object>> _treeItems;
        private TreeView                       _treeView;
        private TextField                      _queryTextBox;
        private Label                          _statsLbl;
        private VisualElement                  _toolBar;


        private void TreeView_selectionChanged(IEnumerable<Object> items )
        {
            if( items.FirstOrDefault() is not TreeItem selectedItem )
                return;
            if( selectedItem.State != ETreeItemState.Normal )
                return;

            if ( _mode == EMode.ObjectsAndFolders )
            {
                if ( selectedItem.Item is ScriptableObject gdo )
                {
                    var folder = _db.RootFolder.EnumerateFoldersDFS().FirstOrDefault( f => f.Objects.Contains( gdo ) );
                    Selected?.Invoke( folder, gdo );
                }
            }
            else
            {
                if ( selectedItem.Item is GdFolder folder )
                {
                    Selected?.Invoke( folder, null );
                }
            }
        }

        private void TreeView_Chosed(IEnumerable<Object> items )
        {
            if( items.FirstOrDefault() is not TreeItem selectedItem )
                return;
            if( selectedItem.State != ETreeItemState.Normal )
                return;

            if ( _mode == EMode.ObjectsAndFolders )
            {
                if ( selectedItem.Item is ScriptableObject gdo )
                {
                    var folder = _db.RootFolder.EnumerateFoldersDFS().FirstOrDefault( f => f.Objects.Contains( gdo ) );
                    Chosed?.Invoke( folder, gdo );
                }
            }
            else
            {
                if ( selectedItem.Item is GdFolder folder )
                {
                    Chosed?.Invoke( folder, null );
                }
            }
        }

        private void InitFoldersBrowser(   GdDb db, IReadOnlyList<GdFolder> folders, GdFolder selectedFolder )
        {
            if ( folders == null )                  //Browse entire db
            {
                var allFolders = db.RootFolder.EnumerateFoldersDFS(  ).ToArray(); 
                _rootFolder             = ConvertSearchResultToHierarchy( Array.Empty<ScriptableObject>(), allFolders );
            }
            else if ( folders.Count == 0 )
            {
                _rootFolder             = null;
            }
            else                           //Some folders to browse
            {
                _rootFolder             = ConvertSearchResultToHierarchy( Array.Empty<ScriptableObject>(), folders );
            }

            var selectedFolderId = selectedFolder?.FolderGuid.GetHashCode();
            ShowSearchResults( _rootFolder, selectedFolderId );
        }
        

        private void InitObjectsBrowser( GdDb db, IReadOnlyList<ScriptableObject> objects, IReadOnlyList<GdFolder> folders, ScriptableObject selectedObject )
        {
            if( objects == null )           //Browse entire db
            {
                _rootFolder             = db.RootFolder;
            } 
            else if ( objects.Count == 0 )       //No objects to browse
            {
                _rootFolder             = null;
            }
            else                                //There are some objects to browse
            {
                _rootFolder             = ConvertSearchResultToHierarchy( objects, folders );
            }

            Int32? selectedObjectId = selectedObject != null ? GetGDObjectHash( selectedObject ) : null;
            ShowSearchResults( _rootFolder, selectedObjectId );
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
                _treeView.SetRootItems( Array.Empty<TreeViewItemData<TreeItem>>() );
                _treeView.Rebuild();
                _statsLbl.text = _mode == EMode.ObjectsAndFolders ? "No objects found" : "No folders found";
            }
            else
            {
                var objectsFound = resultRootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).Count();
                var rootTreeItem         = PrepareTreeRoot( resultRootFolder, _folders );
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

            var resultRootFolder = ConvertSearchResultToHierarchy( objects, folders );

            return resultRootFolder;
        }

        //Reconstruct objects/folders tree from flat search result lists
        private GdFolder  ConvertSearchResultToHierarchy( IReadOnlyList<ScriptableObject> objects, IReadOnlyList<GdFolder> folders )
        {
            Dictionary<Guid, GdFolder> tempFolders = new ();

            if ( folders.Count == 0 )
                return null;

            GdFolder rootFolder;
            if( _mode == EMode.ObjectsAndFolders )
            {
                for ( int i = 0; i < objects.Count; i++ )
                {
                    var obj        = objects[ i ];
                    var folder     = folders[ i ];
                    var tempFolder = GetTempFolder( folder );
                    tempFolder.Objects.Add( obj );
                }
                rootFolder = tempFolders.Values.First();
                
            }
            else
            {
                for ( int i = 0; i < folders.Count; i++ )
                {
                    var folder     = folders[ i ];
                    var tempFolder = GetTempFolder( folder );
                }
                rootFolder = tempFolders.Values.First();
            }

            //Remove root folder if no meaningful content
            if ( rootFolder == _db.RootFolder && rootFolder.SubFolders.Count == 1 && rootFolder.Objects.Count == 0 )
            {
                rootFolder        = rootFolder.SubFolders[ 0 ];
                rootFolder.Parent = null;
            }

            return rootFolder;

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
        }

        private void ShowSearchResults( GdFolder rootFolder, Int32? selectedObjectId = null )
        {
            if ( _mode == EMode.ObjectsAndFolders )
            {
                if ( rootFolder == null )
                {
                    _treeView.style.display = DisplayStyle.None;
                    _statsLbl.text          = "No objects found";
                }
                else
                {
                    var rootTreeItem = PrepareTreeRoot( rootFolder, null );
                    _treeView.SetRootItems( new []{ rootTreeItem } );
                    _treeView.Rebuild();
                    _treeView.ExpandAll();
                    _treeView.style.display = DisplayStyle.Flex;
                    _statsLbl.text          = $"Objects found: {rootFolder.EnumerateFoldersDFS().SelectMany( f => f.Objects ).Count()}";
                }
            }
            else
            {
                if( rootFolder == null )
                {
                    _treeView.style.display = DisplayStyle.None;
                    _statsLbl.text          = "No folders found";
                }
                else
                {
                    var rootTreeItem = PrepareTreeRoot( rootFolder, _folders );
                    _treeView.SetRootItems( new []{ rootTreeItem } );
                    _treeView.Rebuild();
                    _treeView.ExpandAll();
                    _treeView.style.display = DisplayStyle.Flex;
                    _statsLbl.text = $"Folders found: {rootFolder.EnumerateFoldersDFS().Count()}";
                } 
            }

            if ( selectedObjectId.HasValue )
            {
                _treeView.SetSelectionByIdWithoutNotify( new []{selectedObjectId.Value} );
            }
        }

        private TreeViewItemData<TreeItem> PrepareTreeRoot( GdFolder rootFolder, IReadOnlyList<GdFolder> selectableFolders )
        {
            var rootItem = PrepareTreeItem( rootFolder, selectableFolders );
            return rootItem;
        }

        private TreeViewItemData<TreeItem> PrepareTreeItem( GdFolder folder, IReadOnlyList<GdFolder> selectableFolders )
        {
            var childs = new List<TreeViewItemData<TreeItem>>();
            var isSelectable = _mode == EMode.Folders 
                    ?  selectableFolders == null || selectableFolders.Contains( folder, GdFolder.GuidComparer.Instance )
                    : false;                          //In object mode folders always unselectable
            var result = new TreeViewItemData<TreeItem>( folder.FolderGuid.GetHashCode(), new TreeItem(folder) {State = isSelectable ? ETreeItemState.Normal : ETreeItemState.Disabled }, childs );

            foreach ( var subFolder in folder.SubFolders )
            {
                var subfolderItem = PrepareTreeItem( subFolder, selectableFolders );
                childs.Add( subfolderItem );
            }

            foreach ( var gdo in folder.Objects )
            {
                var objectTreeItem = new TreeViewItemData<TreeItem>( GetGDObjectHash( gdo), new TreeItem(gdo) );
                childs.Add( objectTreeItem );
            }

            return result;
        }

        private void BindItem(VisualElement elem, Int32 index )
        {
            var item  = _treeView.GetItemDataForIndex<TreeItem>( index );
            var container = elem.Q<VisualElement>( "Content" );
            var icon = elem.Q<VisualElement>( "Icon" );
            var label = elem.Q<Label>( "Label" );
            if ( item.Item is GdFolder folder )
            {
                icon.style.backgroundImage = Resources.FolderIcon;
                label.text = folder.Name;
            }
            else if ( item.Item is GDRoot root )
            {
                icon.style.backgroundImage = Resources.DatabaseIcon;
                label.text                 = $"{root.Name} ({root.Id})";
            }
            else if ( item.Item is GDObject gdo )
            {
                icon.style.backgroundImage = Resources.GDObjectIcon;
                label.text                 = gdo.name;
            }
            else if ( item.Item is ScriptableObject so )
            {
                icon.style.backgroundImage = Resources.GDObjectIcon;
                label.text                 = so.name;
            }

            if( item.State == ETreeItemState.Disabled )
                container.AddToClassList( "item-disabled" );
        }

        private void UnbindItem(VisualElement elem, Int32 index )
        {
            var container = elem.Q<VisualElement>( "Content" );
            container.RemoveFromClassList( "item-disabled" );
        }

        private TreeViewItemData<TreeItem> FindTreeItemData( TreeViewItemData<TreeItem> root, Predicate<Object> predicate )
        {
            if( predicate( root.data.Item ) )
                return root;

            foreach ( var viewItemChild in root.children )
            {
                var result = FindTreeItemData( viewItemChild, predicate );
                if ( result.data.Item != null )
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
            public static readonly VisualTreeAsset TreeItemElem = UnityEngine.Resources.Load<VisualTreeAsset>( "GdDbBrowserTreeItem" );
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

        public struct TreeItem
        {
            public readonly Object          Item;         //gd object or folder
            public ETreeItemState           State;

            public TreeItem(Object item ) : this()
            {
                Item = item;
            }
        }

        public enum ETreeItemState
        {
            Normal,
            Disabled,
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
            _browser = new GdDbBrowserWidget( db, null );
            _browser.CreateGUI( rootVisualElement );
        }

        private void OnDisable( )
        {
            _browser = null;
        }
    }
}