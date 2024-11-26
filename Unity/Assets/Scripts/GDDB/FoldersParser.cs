﻿using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GDDB.Editor
{
#if UNITY_EDITOR

    /// <summary>
    /// Creates GDDB DOM from folders and GDO assets structure. Starts from Assets/ folder
    /// Works only in Editor
    /// </summary>
    public class FoldersParser
    {
        public Folder Root           { get; private set; }

        public IReadOnlyList<GDObject> AllObjects   =>  _allObjects;
        public IReadOnlyList<Folder> AllFolders     =>  _allFolders;

        /// <summary>
        /// Equivalent to Unity's Assets folder.
        /// </summary>
        public static readonly Guid AssetsFolderGuid = Guid.ParseExact( "00000000-0000-0000-1000-000000000000", "D" );


        /// <summary>
        /// Parse physical folder structure
        /// </summary>
        public Boolean Parse( )
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var disabledObjects = 0;

            _allObjects.Clear();

            //Get all GDObjects
            var gdoids        = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            if( gdoids.Length == 0 )
            {
                Debug.LogError( $"[{nameof(FoldersParser)}] No GDObjects found, impossible to parse game data base" );
                return false;
            }

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

            //Assign Assets folder guid for consistency (Unity is not counts Assets as a folder asset  )
            var assetsFolder = new Folder( "Assets", AssetsFolderGuid );
            var foldersCache = new List<(Guid, String )> { (assetsFolder.FolderGuid, assetsFolder.Name ) };

            //Add GDObjects to hierarchy
            var addedObjectCount = 0;
            _allObjects.Capacity = gdos.Length;
            for ( var i = 0; i < gdos.Length; i++ )
            {
                var gdoData  = gdos[ i ];
                var gdobject = AssetDatabase.LoadAssetAtPath<GDObject>( gdoData.Path );
                if ( !gdobject.EnabledObject )
                {
                    disabledObjects++;
                    continue;
                }
                
                var folder  = GetOrCreateFolderForSplittedPath( assetsFolder, Split( gdoData.Path ), foldersCache );
                folder.Objects.Add( gdobject );
                _allObjects.Add( gdobject );
                addedObjectCount++;
            }

            if( addedObjectCount == 0 )
            {
                _allObjects.Clear();
                _allFolders.Clear();
                Root  = assetsFolder;
                Debug.LogWarning( $"[{nameof(FoldersParser)}] No enabled GDObjects found, impossible to parse game data base" );
                return false;
            }

            //Calculate GDB root folder
            foreach ( var folder in assetsFolder.EnumerateFoldersDFS(  ) )
            {
                if ( folder.Objects.Count > 0 || folder.SubFolders.Count > 1 )
                {
                    Root        = folder;
                    Root.Parent = null;
                    break;
                }
            }

            CalculateDepth( Root );

            _allFolders.Clear();
            foreach ( var folder in Root.EnumerateFoldersDFS(  ) )            
                _allFolders.Add( folder );

            timer.Stop();
            Debug.Log( $"[{nameof(FoldersParser)}]-[{nameof(Parse)}] processed {gdos.Length} GDObject assets, added {addedObjectCount} GDObjects and {_allFolders.Count} folders, disabled {disabledObjects} objects, root folder {Root.Name}, time {timer.ElapsedMilliseconds} ms" );

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

        private void CalculateDepth( Folder root )
        {
            root.Depth = 0;
            foreach ( var folder in root.EnumerateFoldersDFS( false ) )
            {
                if( folder.Parent != null )
                    folder.Depth = folder.Parent.Depth + 1;
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

        /// <summary>
        /// Get most common folder for all GDObjects
        /// </summary>
        /// <param name="gdos"></param>
        /// <returns></returns>
        private String GetGdDbRootFolder( out GDObjectPath[] gdos )
        {
            //Get all GDObjects
            var gdoids        = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            if( gdoids.Length == 0 )
            {
                Debug.LogError( $"[{nameof(FoldersParser)}] No GDObjects found, impossible to parse game data base" );
                gdos = Array.Empty<GDObjectPath>();
                return null;
            }

            gdos = new GDObjectPath[gdoids.Length];
            for ( var i = 0; i < gdoids.Length; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath( gdoids[ i ] );
                gdos[i] = new GDObjectPath
                          {
                                  Path      = path,
                                  Guid      = gdoids[i]
                          };    
            }

            var mostCommonFolderArray = GetDirectoryName( gdos[0].Path ).Split( '/' );
            var mostCommonFolder      = new ArraySegment<String>( mostCommonFolderArray ); 
            for ( var i = 1; i < gdos.Length; i++ )
            {
                mostCommonFolder = GetMostCommonFolder( mostCommonFolder, new PathPartsEnumerator( gdos[i].Path, true ) );    
            }

            var result = String.Join( '/', mostCommonFolder );

            return result;
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
                    var newFolderPath = String.Concat( parentFolder.GetPath(), "/", pathPart );
                    var newFolderGuid = GetFolderGuid( newFolderPath, foldersCache );
                    folder = new Folder( pathPart, newFolderGuid, parentFolder );
                    //_allFolders.Add( folder );
                }

                parentFolder = folder;
            }

            return parentFolder;
        }

        private Guid GetFolderGuid( String folderPath, List<(Guid, String)> foldersCache )
        {
            Guid result;
            var  folderData = foldersCache.Find( f => f.Item2 == folderPath );
            if ( folderData.Item1 == Guid.Empty )
            {
                result = AssetPath2Guid( folderPath  );
                foldersCache.Add( (result, folderPath) );
            }
            else
                result = folderData.Item1;

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
            var unityGuid = AssetDatabase.GUIDFromAssetPath( assetPath );
            var guidStr   = unityGuid.ToString( );
            var clrGuid   = Guid.ParseExact( guidStr, "N" );
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

        public  ArraySegment<String> GetMostCommonFolder( ArraySegment<String> mostCommonFolder, PathPartsEnumerator anotherPath )
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

        private readonly List<GDObject> _allObjects = new ();
        private readonly List<Folder> _allFolders = new ();

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

        [MenuItem( "GDDB/Print hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
            var  parser  = new FoldersParser();
            if ( parser.Parse() )
            {
                Debug.Log( "Root folder: " + parser.Root.GetPath() );
                parser.Print();
            }
            Debug.Log( "No GDDB assets found" );
        }
    }

#endif
}