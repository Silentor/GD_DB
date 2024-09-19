using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
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
        public String RootFolderPath => Root.Path;

        public IReadOnlyList<GDObject> AllObjects   =>  _allObjects;
        public IReadOnlyList<Folder> AllFolders     =>  _allFolders;

        /// <summary>
        /// Parse physical folder structure, root folder goes to <see cref="Root"/>
        /// </summary>
        public void Parse( )
        {
            _allFolders.Clear();
            _allObjects.Clear();

            //Get cache of GDObjects
            var gdoids            = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            var gdos              = new GDObjectData[gdoids.Length];
            for ( var i = 0; i < gdoids.Length; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath(gdoids[i]);
                var gdo  = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                gdos[i] = new GDObjectData
                {
                    GdObject = gdo,
                    Path     = path,
                    SplittedPath = path.Split( "/" ),
                    Guid     = gdoids[i]
                };    
            }

            var gddbRootFolderStr = GetGdDbRootFolder( gdos );
            var gddbRootFolderGuid = AssetPath2Guid( gddbRootFolderStr );

            //Make folders cache of GDDB folder
            var folderIds    = AssetDatabase.FindAssets("t:Folder", new []{ gddbRootFolderStr });
            var foldersGuids = new List<(Guid, String)> { (gddbRootFolderGuid, gddbRootFolderStr) };
            foreach ( var folderId in folderIds )
            {
                var guid = Guid.ParseExact( folderId, "N" );
                var path = AssetDatabase.GUIDToAssetPath( folderId );
                foldersGuids.Add( (guid, path) );
            }

            //Assign Assets folder guid for consistency (Unity is not counts Assets as a folder asset  )
            Root = new Folder( "Assets", "Assets", Guid.ParseExact( "A55E7500-F5B6-4EBA-825C-B1BC7331A193", "D" ) );
            _allFolders.Add( Root );

            //Add folders to hierarchy
            foreach ( var folderId in folderIds )
            {
                 var path = AssetDatabase.GUIDToAssetPath( folderId );
                 var splittedPath = Split( path );
                 var folder = GetFolderForSplittedPath( Root, splittedPath, foldersGuids );
            }

            //Add GDObjects to hierarchy
            foreach ( var gdoData in gdos )
            {
                var folder = GetFolderForSplittedPath( Root, gdoData.SplittedPath, foldersGuids );
                var guid   = Guid.ParseExact( gdoData.Guid, "N" );
                folder.ObjectIds.Add( guid );
                folder.Objects.Add( gdoData.GdObject );
                _allObjects.Add( gdoData.GdObject );
            }

            //Set true GDDB root folder
            Root           = GetFolderForSplittedPath( Root, gddbRootFolderStr.Split( "/" ), foldersGuids );
            Root.Parent    = null;

            CalculateDepth( Root );
        }

        private String GetGdDbRootFolder( GDObjectData[] gdos )
        {
            var shortestPathLength = Int32.MaxValue;
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var pathLength = gdos[i].SplittedPath.Length;
                if( pathLength < shortestPathLength )
                {
                    shortestPathLength = pathLength;
                }
            }

            var topFoldersList = new List<GDObjectData>();
            for ( var i = 0; i < gdos.Length; i++ )
            {
                if( gdos[i].SplittedPath.Length == shortestPathLength )
                {
                    topFoldersList.Add( gdos[i] );
                }
            }

            var topFoldersCount = topFoldersList.Distinct( PathComparerWithoutFilename.Instance ).Count();
            if( topFoldersCount == 1 )
            {
                return GetDirectoryName( topFoldersList[0].Path );
            }
            else
            {
                //Root folder is common for top folders
                return GetDirectoryName( GetDirectoryName( topFoldersList[0].Path ) );
            }

            String GetDirectoryName( String path )
            {
                var lastSlashIndex = path.LastIndexOf( '/' );
                return path.Substring( 0, lastSlashIndex );
            }
        }
        
        public void DebugParse( Folder presetHierarchy )
        {
            Root = presetHierarchy;
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

        private Guid AssetPath2Guid( String assetPath )
        {
            var unityGuid = AssetDatabase.GUIDFromAssetPath( assetPath );
            var clrGuid   = UnsafeUtility.As<GUID, Guid>( ref unityGuid );
            return clrGuid;
        }

        private readonly List<GDObject> _allObjects = new List<GDObject>();
        private readonly List<Folder>   _allFolders = new List<Folder>();

        private struct GDObjectData
        {
            public GDObject GdObject;
            public String   Path;
            public String[] SplittedPath;
            public String   Guid;

        }                    

        private class PathComparerWithoutFilename : IEqualityComparer<GDObjectData>
        {
            public static readonly PathComparerWithoutFilename Instance = new ();

            public Boolean Equals(GDObjectData x, GDObjectData y )
            {
                if( x.SplittedPath.Length != y.SplittedPath.Length )
                    return false;

                for ( int i = 0; i < Math.Min( x.SplittedPath.Length, y.SplittedPath.Length ); i++ )
                {
                    var xPart =  i == x.SplittedPath.Length - 1 && Path.HasExtension( x.SplittedPath[ i ] ) ? String.Empty : x.SplittedPath[ i ];
                    var yPart =  i == y.SplittedPath.Length - 1 && Path.HasExtension( y.SplittedPath[ i ] ) ? String.Empty : y.SplittedPath[ i ];

                    if ( xPart != yPart )
                        return false;    
                }

                return true;
            }

            public Int32 GetHashCode( GDObjectData obj )
            {
                var splittedPath = obj.SplittedPath;
                var pathHash = new HashCode();
                for ( var i = 0; i < splittedPath.Length; i++ )
                {
                    if( i == splittedPath.Length - 1 && Path.HasExtension( splittedPath[ i ] ) )
                        break;

                    pathHash.Add( splittedPath[i] );
                }

                return pathHash.ToHashCode();
            }
        }

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