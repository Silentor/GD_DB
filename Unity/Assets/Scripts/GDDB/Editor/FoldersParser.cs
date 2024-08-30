using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    /// <summary>
    /// Creates gddb DOM from folders and gdo assets structure
    /// </summary>
    public class FoldersParser
    {
        public readonly Folder Root = new Folder{Name = "Assets/", };

        public void Parse( )
        {
            //var folders = AssetDatabase.FindAssets("t:Folder");
            var gdos = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            foreach ( var gdoGuid in gdos )
            {
                var path        = AssetDatabase.GUIDToAssetPath(gdoGuid);
                //var gdo = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                //Debug.Log( $"id {gdoGuid}, path {path}, asset {gdo}" );

                var splittedPath = Split( path );
                var folderForAsset = GetFolderForSplittedPath( Root, splittedPath );
                folderForAsset.Objects.Add( new GDAsset { AssetGuid = gdoGuid, Asset = AssetDatabase.LoadAssetAtPath<GDObject>( path ) } );
            }

            //Skip unused folders?

            //Calculate depth
            foreach ( var folder in EnumerateFoldersDFS( Root ) )
            {
                var depth = 0;
                var checkFolder = folder;
                while ( checkFolder.Parent != null )
                {
                    depth++;
                    checkFolder = checkFolder.Parent;
                }
                folder.Depth = depth;
            }
        }

        //Test some queries, like "Mobs/" - all gdos from Mobs folder
        public IEnumerable<GDObject> GetObjects( String path )
        {
            //Short path - return all objects
            if( String.IsNullOrEmpty( path ) )
            {
                foreach ( var folder in EnumerateFoldersDFS( Root ) )
                    foreach ( var obj in folder.Objects )
                        yield return obj.Asset;
                yield break;
            }
            var splittedPath = Split( path ).ToArray();
            Assert.IsTrue( splittedPath.Length > 0, "Path is empty, must be handled by fast path" );
            var dbFolders         = EnumerateFoldersDFS( Root ).ToArray();
            var pathIndex         = 0;
            var dbIndex           = 0;
            var infiniteLoopGuard = 0;
            var dbFolder          = dbFolders[ 0 ];
            while( dbIndex < dbFolders.Length )   
            {
                if( infiniteLoopGuard++ > 1000 )
                    throw new Exception( "Infinite loop" );
            
                var pathPart = pathIndex < splittedPath.Length ? splittedPath[ pathIndex ] : null; 

                if ( pathPart == null )         //End of path. get files from current db folder
                {
                    foreach ( var obj in dbFolder.Objects )
                    {
                        yield return obj.Asset;
                    }

                    //Continue search from next db folder, maybe there is another folder with same path
                    pathIndex = 0;              
                    dbIndex++;
                }
                else if ( pathPart.EndsWith( '/' ) )     //Check folder
                {
                    dbFolder = dbFolders[ dbIndex ];
                    if( pathPart == dbFolder.Name                                                           //Compare name 
                        && (pathIndex == 0 || dbFolder.Depth == dbFolders[ dbIndex - 1 ].Depth + 1 ))       //Compare structure because path can be like "Mobs/Elves/"
                    {
                        pathIndex++;
                    }
                    else
                    {
                        pathIndex = 0;
                    }

                    dbIndex++;
                }
                else     //GD object name mask, find objects in current db folder  (primitive)
                {
                    foreach ( var obj in dbFolder.Objects )
                    {
                        if ( obj.Asset.Name.StartsWith( pathPart ) )
                            yield return obj.Asset;
                    }

                    //Continue search from next db folder, maybe there is another folder with same path
                    pathIndex = 0;              
                    dbIndex++;
                }
            }
        }

        public void Print( )
        {
            PrintRecursively( Root, 0 );
        }

        private IEnumerable<Folder> EnumerateFoldersDFS( Folder folder )
        {
            yield return folder;
            foreach ( var subFolder in folder.SubFolders )
            {
                foreach ( var subSubFolder in EnumerateFoldersDFS( subFolder ) )
                {
                    yield return subSubFolder;
                }
            }
        }

        private void PrintRecursively(Folder folder, int indent )
        {
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2 );
            }

            foreach ( var obj in folder.Objects )
            {
                Debug.Log($"  {indentStr}{obj.Asset.Name}");
            }
        }

        private Folder GetFolderForSplittedPath( Folder root, IReadOnlyList<String> splittedPath )
        {
            var existFolder = root;
            foreach ( var pathFolder in splittedPath.Skip( 1 ) )    //Skip "Assets"
            {
                if ( pathFolder == splittedPath.Last() )
                    break;

                var folder = existFolder.SubFolders.Find( f => f.Name == pathFolder );
                if ( folder == null )
                {
                    folder = new Folder { Name = pathFolder, Parent = existFolder };
                    existFolder.SubFolders.Add( folder );
                }

                existFolder = folder;
            }

            return existFolder;
        }

        //Custom split with preservation of slash at the parts end
        private IReadOnlyList<String> Split( String path )
        {
            if ( String.IsNullOrEmpty( path ) )
                return Array.Empty<String>();

            var start      = 0;
            var slashIndex = 0;
            var result     = new List<String>();
            while ( start < path.Length )
            {
                slashIndex = path.IndexOf( '/', start );
                if ( slashIndex < 0 )
                {
                    result.Add( path.Substring( start, path.Length - start ) );
                    return result;
                }
                result.Add( path.Substring( start, slashIndex - start + 1 ) );
                start = slashIndex + 1;
            }

            return result;
        }


        [DebuggerDisplay("Name {GetHierarchyPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
        public class Folder
        {
            public String Name;
            public String Path;
            public Folder Parent;
            public Int32  Depth;

            public readonly List<Folder>  SubFolders = new ();
            public readonly List<GDAsset> Objects    = new();
            
            public String GetHierarchyPath( )
            {
                var path = Name;
                var parent = Parent;
                while ( parent != null )
                {
                    path = parent.Name + path;
                    parent = parent.Parent;
                }

                return path;
            }
            
            
        }

        [DebuggerDisplay("{GetName()}")]
        public class GDAsset
        {
            public String   AssetGuid;
            public GDObject Asset;

            public String GetName( )
            {
                return Asset ? Asset.Name : AssetGuid;
            }
        }
    }
}