using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gddb.Editor
{
#if UNITY_EDITOR

    /// <summary>
    /// Creates GDDB DOM tree from folders and files
    /// Uses AssetDatabase, works only in Editor
    /// </summary>
    public class GdDbAssetsParser
    {
        public GdFolder Root           { get; private set; }

        public String RootFolderPath    { get; private set; }

        public IReadOnlyList<GdObjectInfo> AllObjects =>  _allObjects;
        public IReadOnlyList<GdFolder>               AllFolders =>  _allFolders;

        /// <summary>
        /// Assets-based paths of disabled Gddb folders 
        /// </summary>
        public IReadOnlyList<String>                 DisabledFolders => _disabledFolders;

        public Int32 DisabledObjectsCount { get; }

        public const String GddbFolderDisabledLabel = "GddbDisabled";

        /// <summary>
        /// Parse physical folder structure
        /// </summary>
        public GdDbAssetsParser( )
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var disabledObjects = 0;

            _allObjects.Clear();
            _allFolders.Clear();
            _disabledFolders.Clear();

            //Find GD Root object
            var rootsIds = AssetDatabase.FindAssets("t:GDRoot", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            var gdroots  = new List<GDRoot>();
            foreach ( var rootsId in rootsIds )
            {
                var path = AssetDatabase.GUIDToAssetPath( rootsId );
                var root = AssetDatabase.LoadAssetAtPath<GDRoot>( path );
                if( root.EnabledObject )
                    gdroots.Add( root );
            }

            if( gdroots.Count == 0 )
            {
                Debug.LogError( $"[{nameof(GdDbAssetsParser)}] No database assets found. Please add GDRoot object to the folder with game design data files (ScriptableObjects and GDObjects)" );
                return;
            }
            else if( gdroots.Count > 1 )     //todo implement several roots support
            {
                Debug.LogError( $"[{nameof(GdDbAssetsParser)}] Multiple database assets found, only one supported. Will use {AssetDatabase.GetAssetPath( gdroots[0] )} as a root" );
            }

            var rootObject     = gdroots[0];
            var rootObjectPath = AssetDatabase.GetAssetPath( rootObject );
            var rootFolderPath = GetDirectoryName( rootObjectPath );

            if ( rootFolderPath == "Assets" )
            {
                Debug.LogError( $"[{nameof(GdDbAssetsParser)}] Placing GDRoot object to Assets folder is not supported. Its a bad idea, mark all your assets as game design data base. Please select some subfolder for your game data base files." );
                return;
            }

            //Collect all GDObjects under root
            var gdoids        = AssetDatabase.FindAssets("t:ScriptableObject", new []{rootFolderPath});   //Skip packages, consider collect GDObjects from Packages?
            var gdos = new GDObjectPath[gdoids.Length];
            for ( var i = 0; i < gdoids.Length; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath( gdoids[ i ] );
                gdos[i] = new GDObjectPath
                          {
                                  Path = path,
                                  Guid = gdoids[i]
                          };    
            }

            //Add GDObjects and build hierarchy
            var rootFolder           = new GdFolder( GetFileName( rootFolderPath ), GetFolderGuid( rootFolderPath ) );
            var rootFolderPartsCount = CharCount( rootFolderPath, '/' ) + 1;  
            var rootFolderParentPath = GetDirectoryName( rootFolderPath );
            _allObjects.Capacity      = gdos.Length;
            _allFolders.Add( rootFolder );
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var gdoData  = gdos[ i ];
                var gdobject = AssetDatabase.LoadAssetAtPath<ScriptableObject>( gdoData.Path );
                if ( IsGdObjectDisabled( gdobject, gdoData.Path ) )
                {
                    disabledObjects++;
                    continue;
                }
                
                var folder  = GetOrCreateFolderForSplittedPath( rootFolder, rootFolderParentPath, rootFolderPartsCount, gdoData.Path );
                if ( folder == null )        //Disabled folder found
                {
                    disabledObjects++;
                    continue;
                }

                folder.Objects.Add( gdobject );
                _allObjects.Add( new GdObjectInfo( Guid.ParseExact( gdos[i].Guid, "N" ), gdobject, folder ) );
            }

            CalculateDepth( rootFolder );
            Root                 = rootFolder;
            RootFolderPath       = rootFolderPath;
            DisabledObjectsCount = disabledObjects;

            timer.Stop();
            Debug.Log( $"[{nameof(GdDbAssetsParser)}] found GD database at {rootFolderPath}: processed {gdos.Length} GDObject assets, added {_allObjects.Count} GDObjects and {_allFolders.Count} folders, disabled {disabledObjects} objects, time {timer.ElapsedMilliseconds} ms" );
        }

        private void CalculateDepth( GdFolder root )
        {
            root.Depth = 0;
            foreach ( var folder in root.EnumerateFoldersDFS( false ) )
            {
                if( folder.Parent != null )
                    folder.Depth = folder.Parent.Depth + 1;
            }
        }

        private GdFolder GetOrCreateFolderForSplittedPath( GdFolder root, String rootFolderParentPath, Int32 rootFolderParts, String objectPath )
        {
            var splittedPath = objectPath.Split( '/' );

            //Create folders hierarchy for the object from the top
            var parentFolder = root;
            for ( var i = rootFolderParts; i < splittedPath.Length - 1; i++ )  //Skip root folder and asset filename name
            {
                var folderName = splittedPath[ i ];
                var folder = parentFolder.SubFolders.Find( f => f.Name == folderName );
                if ( folder == null )
                {
                    var newFolderAssetsBasePath = String.Concat( rootFolderParentPath, "/", parentFolder.GetPath(), "/", folderName );
                    //Check if new folder is disabled
                    if ( IsFolderSelfDisabled( newFolderAssetsBasePath ) )
                    {
                        _disabledFolders.Add( newFolderAssetsBasePath );
                        return null;                            //Do not create disabled folders
                    }

                    var newFolderGuid           = AssetPath2Guid( newFolderAssetsBasePath );
                    folder = new GdFolder( folderName, newFolderGuid, parentFolder );
                    _allFolders.Add( folder );
                }

                parentFolder = folder;
            }

            return parentFolder;
        }

        private Guid GetFolderGuid( String assetPath )
        {
            var result = AssetPath2Guid( assetPath  );
            return result;
        } 

        private String[] Split( String assetPath )
        {
            if ( String.IsNullOrEmpty( assetPath ) )
                return Array.Empty<String>();

            return assetPath.Split( '/' );
        }

        private PathPartsEnumerator EnumeratePath( String path )
        {
            return new PathPartsEnumerator( path );
        }

        private static Guid AssetPath2Guid( String assetPath )
        {
            var unityGuid = AssetDatabase.AssetPathToGUID( assetPath );
            var clrGuid   = Guid.ParseExact( unityGuid, "N" );
            //var clrGuid   = UnsafeUtility.As<GUID, Guid>( ref unityGuid ); do not work directly
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
            if ( lastSlashIndex < 0 )
                return path;
            return path[ ..lastSlashIndex ];
        }

        private static String GetFileName( String path )
        {
            var lastSlashIndex = path.LastIndexOf( '/' );
            if ( lastSlashIndex < 0 )
                return path;
            return path[ (lastSlashIndex + 1).. ];
        }

        private Boolean IsGdObjectDisabled( ScriptableObject gdObject, String path )
        {
            if ( gdObject is GDObject gdo && !gdo.EnabledObject )
                return true;

            foreach ( var disabledFolder in _disabledFolders )
            {
                if ( path.StartsWith( disabledFolder ) )
                    return true;
            }

            return false;
        }

        public static Boolean IsFolderSelfDisabled( String folderPath )
        {
            var folderGuid = AssetDatabase.GUIDFromAssetPath( folderPath );
            var labels     = AssetDatabase.GetLabels( folderGuid );
            return Array.IndexOf( labels, GddbFolderDisabledLabel ) >= 0;
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

        private  ArraySegment<String> GetMostCommonFolder( ArraySegment<String> mostCommonFolder, PathPartsEnumerator anotherPath )
        {
            var i = 0;
            while ( anotherPath.MoveNext() && i < mostCommonFolder.Count )
            {
                var anotherPart = anotherPath.Current;
                var existingPart = mostCommonFolder[ i ].AsSpan();
                if ( anotherPart != existingPart )
                    break;
                i++;
            }

            return new ArraySegment<String>( mostCommonFolder.Array, 0, i ); 
        }

        private readonly List<GdObjectInfo> _allObjects      = new ();
        private readonly List<GdFolder>               _allFolders      = new ();
        private readonly List<String>                 _disabledFolders = new();

        private struct GDObjectPath
        {
            public String               Path;
            public String               Guid;
        }


        // private class PathComparerWithoutFilename : IEqualityComparer<GDObjectPath>
        // {
        //     public static readonly PathComparerWithoutFilename Instance = new ();
        //
        //     public Boolean Equals(GDObjectPath x, GDObjectPath y )
        //     {
        //         if( x.SplittedPath.Length != y.SplittedPath.Length )
        //             return false;
        //
        //         for ( int i = 0; i < Math.Min( x.SplittedPath.Length, y.SplittedPath.Length ); i++ )
        //         {
        //             var xPart =  i == x.SplittedPath.Length - 1 && Path.HasExtension( x.SplittedPath[ i ] ) ? String.Empty : x.SplittedPath[ i ];
        //             var yPart =  i == y.SplittedPath.Length - 1 && Path.HasExtension( y.SplittedPath[ i ] ) ? String.Empty : y.SplittedPath[ i ];
        //
        //             if ( xPart != yPart )
        //                 return false;    
        //         }
        //
        //         return true;
        //     }
        //
        //     public Int32 GetHashCode( GDObjectPath obj )
        //     {
        //         var splittedPath = obj.SplittedPath;
        //         var pathHash = new HashCode();
        //         for ( var i = 0; i < splittedPath.Length; i++ )
        //         {
        //             if( i == splittedPath.Length - 1 && Path.HasExtension( splittedPath[ i ] ) )
        //                 break;
        //
        //             pathHash.Add( splittedPath[i] );
        //         }
        //
        //         return pathHash.ToHashCode();
        //     }
        // }

        public struct PathPartsEnumerator 
        {
            private readonly String  _path;
            private readonly Boolean _skipLastPart;
            private          Int32   _delimiterIndex;
            private          Int32   _prevIndex;
 

            public PathPartsEnumerator( String path, Boolean skipLastPart = false )
            {
                _path              = path;
                _skipLastPart = skipLastPart;
                _delimiterIndex    = -1;
                _prevIndex         = -1;
            }

            public Boolean MoveNext( )
            {
                if ( _path.Length == 0 || _delimiterIndex >= _path.Length )
                    return false;

                _prevIndex      = _delimiterIndex + 1;
                _delimiterIndex = _path.IndexOf( '/', _prevIndex );
                if ( _delimiterIndex == -1 )        //Process last path part  (possibly file name)
                {
                    if ( !_skipLastPart )
                    {
                        _delimiterIndex = _path.Length;
                        return true;
                    }
                    else
                        return false;
                }

                return true;
            }

            public String Current => _path.Substring( _prevIndex, _delimiterIndex - _prevIndex );  //todo use Span
        }

        
    }

#endif
}