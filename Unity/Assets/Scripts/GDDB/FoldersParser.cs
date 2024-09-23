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
        public String RootFolderPath => Root?.Path;

        public IReadOnlyList<GDObject> AllObjects   =>  _allObjects;
        //public IReadOnlyList<Folder> AllFolders     =>  _allFolders;

        public String GetRootFolderPath( )
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            //var  result = GetGdDbRootFolder( out var gdos );

            //Get all GDObjects
            var gdoids        = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            var gdos              = new GDObjectPath[gdoids.Length];
            for ( var i = 0; i < gdoids.Length; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath( gdoids[ i ] );
                gdos[i] = new GDObjectPath
                          {
                                  Path      = path,
                                  FolderPath = GetDirectoryName( path ),
                                  SplittedPath = path.Split( '/' ),
                                  PathDepth = CharCount( path, '/' ),
                                  Guid      = gdoids[i]
                          };    
            }

            if( gdoids.Length == 0 )
            {
                Debug.LogError( $"[{nameof(FoldersParser)}] No GDObjects found, impossible to parse game data base" );
                return null;
            }

            var mostCommonFolder = new FolderPath( gdos[ 0 ].FolderPath );
            for ( var i = 1; i < gdos.Length; i++ )
            {
                 mostCommonFolder = mostCommonFolder.GetMostCommonFolder( new FolderPath(gdos[i].Path ) );    
            }

            timer.Stop();
            Debug.Log( $"[{nameof(FoldersParser)}]-[{nameof(GetRootFolderPath)}] processed {gdos.Length} GDObjects, {timer.ElapsedMilliseconds} ms, result {mostCommonFolder.Path}" );
            return mostCommonFolder.Path;
        }

        /// <summary>
        /// Parse physical folder structure, root folder goes to <see cref="Root"/>
        /// </summary>
        public Boolean Parse( )
        {
            //_allFolders.Clear();
            _allObjects.Clear();

            var gdRootFolderPath   = GetGdDbRootFolder( out var gdos );
            var gddbRootFolderGuid = AssetPath2Guid( gdRootFolderPath );

            //Make folders cache of GDDB folder
            var folderIds    = AssetDatabase.FindAssets("t:Folder", new []{ gdRootFolderPath });
            var foldersGuids = new List<(Guid, String)> ( folderIds.Length + 1 ){ (gddbRootFolderGuid, gdRootFolderPath) };
            foreach ( var folderId in folderIds )
            {
                var guid = Guid.ParseExact( folderId, "N" );
                var path = AssetDatabase.GUIDToAssetPath( folderId );
                foldersGuids.Add( (guid, path) );
            }

            //Assign Assets folder guid for consistency (Unity is not counts Assets as a folder asset  )
            var assetsRoot = new Folder( "Assets", "Assets", Guid.ParseExact( "A55E7500-F5B6-4EBA-825C-B1BC7331A193", "D" ) );
            //_allFolders.Add( Root );

            //Add folders to hierarchy
            var gdbRootFolder = GetOrCreateFolderForSplittedPath( assetsRoot, gdRootFolderPath.Split( '/' ), foldersGuids );
            foreach ( var folderId in folderIds )
            {
                 var path         = AssetDatabase.GUIDToAssetPath( folderId );
                 var splittedPath = Split( path );
                 var folder       = GetOrCreateFolderForSplittedPath( assetsRoot, splittedPath, foldersGuids );
            }

            //Add GDObjects to hierarchy
            _allObjects.Capacity = gdos.Length;
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var gdoData = gdos[ i ];
                if ( gdoData.SplittedPath == null )
                {
                    gdoData.SplittedPath = gdoData.Path.Split( '/' );
                    gdos[ i ]            = gdoData;
                }
                var folder = GetOrCreateFolderForSplittedPath( assetsRoot, gdoData.SplittedPath, foldersGuids );
                var guid   = Guid.ParseExact( gdoData.Guid, "N" );
                folder.ObjectIds.Add( guid );
                var gdobject = AssetDatabase.LoadAssetAtPath<GDObject>( gdoData.Path );
                folder.Objects.Add( gdobject );
                _allObjects.Add( gdobject );
            }

            //Set true GDDB root folder
            Root        = gdbRootFolder;
            Root.Parent = null;

            CalculateDepth( Root );

            return true;
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

        private String GetGdDbRootFolder( out GDObjectPath[] gdos )
        {
            //Get all GDObjects
            var gdoids        = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            gdos              = new GDObjectPath[gdoids.Length];
            for ( var i = 0; i < gdoids.Length; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath( gdoids[ i ] );
                gdos[i] = new GDObjectPath
                          {
                                  Path      = path,
                                  PathDepth = CharCount( path, '/' ),
                                  Guid      = gdoids[i]
                          };    
            }

            if( gdoids.Length == 0 )
            {
                Debug.LogError( $"[{nameof(FoldersParser)}] No GDObjects found, impossible to parse game data base" );
                return null;
            }

            var shortestPathLength = Int32.MaxValue;
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var pathLength = gdos[i].PathDepth;
                if( pathLength < shortestPathLength )
                {
                    shortestPathLength = pathLength;
                }
            }

            var topFoldersList = new List<GDObjectPath>();
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var gdo = gdos[ i ];
                if( gdo.PathDepth == shortestPathLength )
                {
                    gdo.SplittedPath = gdo.Path.Split( '/' ); //todo Optimize because we know parts count ?
                    topFoldersList.Add( gdo );
                    gdos[ i ] = gdo;
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

            
        }

        private Folder GetOrCreateFolderForSplittedPath( Folder root, String[] splittedObjectPath, List<(Guid, String)> foldersCache )
        {
            //Create folders hierarchy
            var parentFolder = root;
            for ( var i = 1; i < splittedObjectPath.Length; i++ )//Skip root folder (because root folder definitely exists)
            {
                var pathPart = splittedObjectPath[ i ];
                if(  i == splittedObjectPath.Length - 1 && System.IO.Path.HasExtension( pathPart) )             //Skip asset file name
                    continue;

                var folder = parentFolder.SubFolders.Find( f => f.Name == pathPart );
                if ( folder == null )
                {
                    var newFolderPath = String.Concat( parentFolder.Path, "/", pathPart );
                    var folderData = foldersCache.Find( f => f.Item2 == newFolderPath );
                    folder = new Folder( pathPart, folderData.Item1, parentFolder );
                    //_allFolders.Add( folder );
                }

                parentFolder = folder;
            }

            return parentFolder;
        }

        //Custom split with preservation of slash at the folders names end
        //Assets/Resources/Objects/myobj.asset -> [Assets/, Resources/, Objects/, myobj.asset]
        private String[] Split( String assetPath )
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

        private Int32 CharCount(  String str, Char ch )
        {
            var result    = 0;
            var strLength = str.Length;
            for ( int i = 0; i < strLength; i++ )
            {
                if ( str[ i ] == ch )
                    result++;
            }

            return result;
        }

        private static String GetDirectoryName( String path )
        {
            var lastSlashIndex = path.LastIndexOf( '/' );
            return path.Substring( 0, lastSlashIndex );
        }

        public Boolean StartsWith( IReadOnlyList<String> myFolder, IReadOnlyList<String> otherFolder )
        {
            if( otherFolder.Count > myFolder.Count )
                return false;

            for ( int i = 0; i < otherFolder.Count; i++ )
            {
                if( myFolder[i] != otherFolder[i] )
                    return false;
            }

            return true;
        }

        public IReadOnlyList<String> GetMostCommonFolder( ArraySegment<String> folder1, ArraySegment<String> folder2 )
        {
            if ( StartsWith( folder1, folder2 ) )
                return folder2;
            else if ( StartsWith( folder2, folder1 )) 
                return folder1;
            else
            {
                var length = Math.Min( folder1.Count, folder2.Count );
                for ( int i = 0; i < length; i++ )
                {
                    if( folder1[ i ] != folder2[ i ] )
                        return new ArraySegment<String>( folder1.Array, 0, i );
                }
            }

            throw new InvalidOperationException();
        }

        private readonly List<GDObject> _allObjects = new List<GDObject>();

        private struct GDObjectPath
        {
            public String               Path;
            public String[]             SplittedPath;
            public String               Guid;

            public ArraySegment<String> FolderPath => new ( SplittedPath, 0, SplittedPath.Length - 1 );

            

            
        }

        private struct FolderPath
        {
            public readonly String Path;

            public FolderPath( String path ) : this()
            {
                if( System.IO.Path.HasExtension( path ) )
                    Path = GetDirectoryName( path );
                else
                    Path = path;
            }

            public FolderPath( String[] splittedPath ) : this()
            {
                if( System.IO.Path.HasExtension( splittedPath[ _splittedPath.Length - 1 ] ) )
                    _splittedPath = splittedPath.Take( _splittedPath.Length - 1 ).ToArray();
                else
                    _splittedPath = splittedPath;
                Path = String.Join( '/', _splittedPath );
            }

            public String[] SplittedPath
            {
                get
                {
                    if( _splittedPath == null )
                        _splittedPath = Path.Split( '/' );
                    return _splittedPath;
                }
            }

            public FolderPath GetMostCommonFolder( FolderPath otherFolder )
            {
                if ( this.StartsWith( otherFolder ) )
                    return otherFolder;
                else if( otherFolder.StartsWith( this ) )
                    return this;
                else
                {
                    var length = Math.Min( SplittedPath.Length, otherFolder.SplittedPath.Length );
                    for ( int i = 0; i < length; i++ )
                    {
                        if( SplittedPath[ i ] != otherFolder.SplittedPath[ i ] )
                            return new FolderPath( String.Join( '/', SplittedPath, 0, i ) );
                    }
                }

                throw new InvalidOperationException();
            }

            public Boolean StartsWith( FolderPath otherFolder )
            {
                if( otherFolder.SplittedPath.Length > SplittedPath.Length )
                    return false;

                for ( int i = 0; i < otherFolder.SplittedPath.Length; i++ )
                {
                    if( SplittedPath[i] != otherFolder.SplittedPath[i] )
                        return false;
                }

                return true;
            }

            private String[] _splittedPath;
        }

        private class PathComparerWithoutFilename : IEqualityComparer<GDObjectPath>
        {
            public static readonly PathComparerWithoutFilename Instance = new ();

            public Boolean Equals(GDObjectPath x, GDObjectPath y )
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

            public Int32 GetHashCode( GDObjectPath obj )
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
            Debug.Log( "Root folder: " + folders.GetRootFolderPath() );
            folders.Parse();
            folders.Print();
        }
#endif
        
    }
}