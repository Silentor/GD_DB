using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GDDB
{
    public partial class GdDb
    {
        public         GDRoot                  Root       { get; }
        public         Folder                  RootFolder { get; }

        public virtual IReadOnlyList<GDObject> AllObjects { get; }

        public GdDb( Folder dbStructure, IReadOnlyList<GDObject> allObjects )
        {
            RootFolder = dbStructure;
            Root       = allObjects.OfType<GDRoot>().Single( );
            AllObjects = allObjects;
        }

        public IEnumerable<T> GetComponents<T>() where T : GDComponent
        {
            foreach ( var gdObject in AllObjects )
            {
                foreach ( var component in gdObject.Components )
                {
                    if ( component is T t )
                        yield return t;
                }
            }
        }

        //Test some queries, like "Mobs/" - all gdos from Mobs folder
        public IEnumerable<GDObject> GetObjects( String path )
        {
            //Short path - return all objects
            if( String.IsNullOrEmpty( path ) )
            {
                foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
                    foreach ( var obj in folder.Objects )
                        yield return obj.Asset;
                yield break;
            }

            var query        = ConvertPathToQuery( path );

            foreach ( var folder in  RootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdObject in query.First().ProcessFolder( folder ) )
                {
                    yield return    gdObject;
                }
            }
        }
      
        // public GDObject GetObject( GdType type )
        // {
        //     return AllObjects.First( o => o.Type == type );
        // }

        // public IEnumerable<GDObject> GetObjects( Int32 category1 )
        // {
        //     return AllObjects.Where( o => o.Type[0] == category1 );
        // }
        //
        // public IEnumerable<GDObject> GetObjects( Int32 category1, Int32 category2 )
        // {
        //     return AllObjects.Where( o => o.Type[0] == category1 && o.Type[1] == category2 );
        // }

        public void Print( )
        {
            var foldersCount = 0;
            var objectsCount = 0;
            PrintRecursively( RootFolder, 0, ref foldersCount, ref objectsCount );
            Debug.Log( $"DB name {Root.Id}, Folders {foldersCount}, objects {objectsCount}" );
        }

        //Custom split with preservation of slash at the folders names end
        private IReadOnlyList<String> Split( String assetPath )
        {
            if ( String.IsNullOrEmpty( assetPath ) )
                return Array.Empty<String>();

            var start      = 0;
            var slashIndex = 0;
            var result     = new List<String>();
            while ( start < assetPath.Length )
            {
                slashIndex = assetPath.IndexOf( '/', start );
                if ( slashIndex < 0 )
                {
                    result.Add( assetPath.Substring( start, assetPath.Length - start ) );
                    return result;
                }
                result.Add( assetPath.Substring( start, slashIndex - start + 1 ) );
                start = slashIndex + 1;
            }

            return result;
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

        private void PrintRecursively(Folder folder, int indent, ref Int32 foldersCount, ref Int32 objectsCount )
        {
            foldersCount++;
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2, ref foldersCount, ref objectsCount );
            }

            foreach ( var obj in folder.Objects )
            {
                objectsCount++;
                Debug.Log($"  {indentStr}{obj.Asset.Name}, type {obj.Asset.GetType().Name}, components {obj.Asset.Components.Count}");
            }
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
                        foreach ( var f in folder.EnumerateFoldersDFS(  ) )
                        {
                            foreach ( var gdObject in NextPart.ProcessFolder( f ) )
                            {
                                yield return    gdObject;
                            }
                        }
                    else
                    {
                        foreach ( var subFolder in folder.EnumerateFoldersDFS(  ) )
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

    }
}