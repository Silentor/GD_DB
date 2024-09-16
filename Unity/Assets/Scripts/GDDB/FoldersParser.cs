using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    /// <summary>
    /// Creates GDDB DOM from folders and GDO assets structure. Starts from Assets/ folder
    /// </summary>
    public class FoldersParser
    {
        public Folder Root           { get; private set; }
        public String RootFolderPath { get; private set; }

        public IReadOnlyList<GDObject> AllObjects   =>  _allObjects;
        public IReadOnlyList<Folder> AllFolders     =>  _allFolders;

        /// <summary>
        /// Parse physical folder structure, root folder goes to <see cref="Root"/>
        /// </summary>
        public void Parse( )
        {
            _allFolders.Clear();
            _allObjects.Clear();

            //Make folders cache
            var folders      = AssetDatabase.FindAssets("t:Folder", new []{"Assets/"});
            var foldersGuids = new List<(Guid, String)>();
            foreach ( var folderId in folders )
            {
                 var guid = Guid.ParseExact( folderId, "N" );
                 var path = AssetDatabase.GUIDToAssetPath( folderId );
                 foldersGuids.Add( (guid, path) );
            }

            //Assign Assets folder guid for consistency (Unity is not counts Assets as a folder asset  )
            Root = new Folder( "Assets", "Assets", Guid.ParseExact( "A55E7500-F5B6-4EBA-825C-B1BC7331A193", "D" ) );
            _allFolders.Add( Root );

            //Create folders hierarchy and add GDObjects
            var gdos = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            foreach ( var guidStr in gdos )
            {
                var path        = AssetDatabase.GUIDToAssetPath(guidStr);

                var splittedPath   = Split( path );
                var folderForAsset = GetFolderForSplittedPath( Root, splittedPath, foldersGuids );
                var guid           = Guid.ParseExact( guidStr, "N" );
                folderForAsset.ObjectIds.Add( guid );
                var gdObject = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                folderForAsset.Objects.Add( gdObject );
                _allObjects.Add( gdObject );
            }

            //Find GDDB root folder
            var gddbRootFolder = Root;
            while( gddbRootFolder.SubFolders.Count == 1 && gddbRootFolder.Objects.Count == 0 )
            {
                gddbRootFolder     = gddbRootFolder.SubFolders[0];
            }
            var gddbRootFolderPath = gddbRootFolder.Path;

            //Add empty folders
            foreach ( var folder in foldersGuids )
            {
                if ( folder.Item2.StartsWith( gddbRootFolderPath ) && !_allFolders.Exists( f => f.FolderGuid == folder.Item1 ) )
                {
                    var emptyFolderPath = folder.Item2;
                    var splittedPath    = Split( emptyFolderPath );
                    var emptyFolder     = GetFolderForSplittedPath( Root, splittedPath, foldersGuids );        
                    _allFolders.Add( emptyFolder );
                }
            }

            //Set root folder
            Root        = gddbRootFolder;
            Root.Parent = null;
            RootFolderPath = Root.Path;

            CalculateDepth( Root );
        }

        public void DebugParse( Folder presetHierarchy )
        {
            Root.SubFolders.Add( presetHierarchy );
            presetHierarchy.Parent = Root;

            CalculateDepth( Root );
        }

        public void Print( )
        {
            PrintRecursively( Root, 0 );
        }

        public void CalculateDepth( Folder root )
        {
            foreach ( var folder in root.EnumerateFoldersDFS(  ) )
            {
                var depth       = 0;
                var checkFolder = folder;
                while ( checkFolder.Parent != null )
                {
                    depth++;
                    checkFolder = checkFolder.Parent;
                }
                folder.Depth = depth;
            }
        }

        private void PrintRecursively(Folder folder, int indent )
        {
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}/");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2 );
            }

            foreach ( var obj in folder.Objects )
            {
                Debug.Log($"  {indentStr}{obj.Name}");
            }
        }

        private Folder GetFolderForSplittedPath( Folder root, IReadOnlyList<String> splittedObjectPath, List<(Guid, String)> foldersCache )
        {
            //Create folders hierarchy
            var parentFolder = root;
            for ( var i = 1; i < splittedObjectPath.Count; i++ )//Skip root folder (because root folder definitely exists)
            {
                var pathPart = splittedObjectPath[ i ];
                if(  i == splittedObjectPath.Count - 1 && System.IO.Path.HasExtension( pathPart) )             //Skip asset file name
                    continue;

                var folder = parentFolder.SubFolders.Find( f => f.Name == pathPart );
                if ( folder == null )
                {
                    var newFolderPath = String.Concat( parentFolder.Path, "/", pathPart );
                    var folderData = foldersCache.Find( f => f.Item2 == newFolderPath );
                    folder = new Folder( pathPart, folderData.Item1, parentFolder );
                    _allFolders.Add( folder );
                }

                parentFolder = folder;
            }

            return parentFolder;
        }

        //Custom split with preservation of slash at the folders names end
        //Assets/Resources/Objects/myobj.asset -> [Assets/, Resources/, Objects/, myobj.asset]
        private IReadOnlyList<String> Split( String assetPath )
        {
            if ( String.IsNullOrEmpty( assetPath ) )
                return Array.Empty<String>();

            return assetPath.Split( '/' );

            //
            // var start      = 0;
            // var slashIndex = 0;
            // var result     = new List<String>();
            // while ( start < assetPath.Length )
            // {
            //     slashIndex = assetPath.IndexOf( '/', start );
            //     if ( slashIndex < 0 )
            //     {
            //         result.Add( assetPath.Substring( start, assetPath.Length - start ) );
            //         return result;
            //     }
            //     result.Add( assetPath.Substring( start, slashIndex - start + 1 ) );
            //     start = slashIndex + 1;
            // }
            //
            // return result;
        }

        private readonly List<GDObject> _allObjects = new List<GDObject>();
        private readonly List<Folder>   _allFolders = new List<Folder>();

#if UNITY_EDITOR
        [MenuItem( "GDDB/Print hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
            var  folders  = new FoldersParser();
            folders.Parse();
            folders.Print();
        }
#endif
        
    }
}