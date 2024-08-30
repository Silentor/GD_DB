using System;
using System.Collections;
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

            CalculateDepth();
        }

        

        public void DebugParse( Folder presetHierarchy )
        {
            Root.SubFolders.Add( presetHierarchy );
            presetHierarchy.Parent = Root;

            CalculateDepth();
        }

        //Test some queries, like "Mobs/" - all gdos from Mobs folder
        public IEnumerable<GDObject> GetObjects( String path )
        {
            //Short path - return all objects
            if( String.IsNullOrEmpty( path ) )
            {
                foreach ( var folder in Root.EnumerateDFS(  ) )
                    foreach ( var obj in folder.Objects )
                        yield return obj.Asset;
                yield break;
            }

            var query        = ConvertPathToQuery( path );

            foreach ( var folder in  Root.EnumerateDFS(  ) )
            {
                foreach ( var gdObject in query.First().ProcessFolder( folder ) )
                {
                    yield return    gdObject;
                }
            }
        }

        public void Print( )
        {
            PrintRecursively( Root, 0 );
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

        private void CalculateDepth( )
        {
            foreach ( var folder in Root.EnumerateDFS(  ) )
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

        private IReadOnlyList<Query> ConvertPathToQuery( String path )
        {
            var splittedPath = Split( path );
            var result       = new List<Query>( splittedPath.Count );
            for ( int i = 0; i < splittedPath.Count; i++ )
            {
                var   partStr = splittedPath[ i ] ;
                Query query;
                if ( partStr.EndsWith( '/' ) )
                    query = new FolderQuery() { Term = partStr };
                else
                    query = new FileQuery() { Term = partStr };

                result.Add( query );
            }

            if ( result.Last() is not FileQuery )
            {
                var lastAllFilesQuery = new FileQuery();
                result.Add( lastAllFilesQuery );
            }

            for ( int i = 1; i < result.Count; i++ )
            {
                result[ i - 1 ].NextPart = result[ i ];
            }

            return result;
        }

        private abstract class Query
        {
            public Query  NextPart;
            public String Term;

            public abstract IEnumerable<GDObject> ProcessFolder( Folder folder );
        }

        private class FileQuery : Query
        {
            public override IEnumerable<GDObject> ProcessFolder(Folder folder )
            {
                if ( String.IsNullOrEmpty( Term ) )
                {
                    foreach ( var gdAsset in folder.Objects )
                    {
                        yield return gdAsset.Asset;
                    }
                }
                else
                {
                    foreach ( var gdAsset in folder.Objects )
                    {
                        if( gdAsset.Asset.name.Contains( Term ) )
                            yield return gdAsset.Asset;
                    }
                }
            }
        }

        private class FolderQuery : Query
        {
            public override IEnumerable<GDObject> ProcessFolder(Folder folder )
            {
                if ( Term == "**/" )
                {
                    if( NextPart is FileQuery )
                        foreach ( var f in folder.EnumerateDFS(  ) )
                        {
                            foreach ( var gdObject in NextPart.ProcessFolder( f ) )
                            {
                                yield return    gdObject;
                            }
                        }
                    else
                    {
                        foreach ( var subFolder in folder.EnumerateDFS(  ) )
                        {
                            foreach ( var gdObject in NextPart.ProcessFolder( subFolder ) )
                            {
                                yield return gdObject;
                            }
                        }
                    }
                }
                else if ( Term == folder.Name || Term == "*/")
                {
                    if( NextPart is FileQuery )
                        foreach ( var gdObject in NextPart.ProcessFolder( folder ) )
                        {
                            yield return gdObject;
                        }
                    else
                    {
                        foreach ( var subFolder in folder.SubFolders )
                        {
                            foreach ( var gdObject in NextPart.ProcessFolder( subFolder ) )
                            {
                                yield return gdObject;
                            }
                        }
                    }
                }
            }
        }

        [DebuggerDisplay("Name {GetHierarchyPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
        public class Folder : IEnumerable
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

            public IEnumerable<Folder> EnumerateDFS( Boolean includeSelf = true )
            {
                if( includeSelf )
                    yield return this;
                foreach ( var subFolder in SubFolders )
                {
                    foreach ( var subSubFolder in subFolder.EnumerateDFS(  ) )
                    {
                        yield return subSubFolder;
                    }
                }
            }

            public IEnumerator GetEnumerator( )
            {
                throw new NotImplementedException();
            }
        }

        [DebuggerDisplay("{AssetName}")]
        public class GDAsset
        {
            public String   AssetGuid;
            //public String   AssetName;
            public GDObject Asset;

            // public String GetName( )
            // {
            //     return Asset ? Asset.Name : AssetGuid;
            // }

            // public GDObject GetObject( )
            // {
            //     if ( !Asset )
            //     {
            //         var path = AssetDatabase.GUIDToAssetPath( AssetGuid );
            //         Asset = AssetDatabase.LoadAssetAtPath<GDObject>( path );
            //     }
            //
            //     return Asset;
            // }
        }
    }
}