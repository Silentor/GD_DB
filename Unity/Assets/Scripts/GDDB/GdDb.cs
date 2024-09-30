using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace GDDB
{
    public partial class GdDb
    {
        public String Name { get; } = "Default";

        //public         GDRoot                  Root       { get; }
        public         Folder                  RootFolder { get; }

        public virtual IReadOnlyList<GDObject> AllObjects { get; }

        public GdDb( Folder dbStructure, IReadOnlyList<GDObject> allObjects )
        {
            RootFolder = dbStructure;
            //Root       = allObjects.OfType<GDRoot>().Single( );
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

        
        /// <summary>
        /// Query objects by folders/objects path
        /// </summary>
        /// <param name="path">Query path of folders and files, supports * wildcard for folder name</param>
        /// <returns></returns>
        /// <remarks>
        /// <example>
        /// null or empty path - return all objects
        /// Humans/ - return all objects in Humans folder(s) (but not from subfolders)
        /// Mobs/Orcs/ - return all objects in Orcs folder that is in Mobs folder
        /// Mobs/*/ - return all objects in all folders of Mobs folder(s)
        /// Mobs// - return all objects in Mobs folder and all subfolders 
        /// CommonMobs - return all object(s) with name CommonMobs
        /// Wildcards is supported
        /// </example>
        /// </remarks>
        public IEnumerable<GDObject> GetObjects( String path )
        {
            //Short path - return all objects
            if( String.IsNullOrEmpty( path ) )
            {
                foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
                    foreach ( var obj in folder.Objects )
                        yield return obj;
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

        public IEnumerable<GDObject> GetObjects( String path, Type componentType )
        {
            foreach ( var gdObject in GetObjects( path ) )
            {
                if ( gdObject.HasComponent( componentType ) )
                    yield return gdObject;
            }
        }

        public IEnumerable<(Folder, GDObject)> GetObjectsAndFolders( String path )
        {
            //Short path - return all objects
            if( String.IsNullOrEmpty( path ) )
            {
                foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
                    foreach ( var obj in folder.Objects )
                        yield return (folder, obj);
                yield break;
            }

            var query        = ConvertPathToQuery( path );

            foreach ( var folder in  RootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdObject in query.First().ProcessFolder( folder ) )
                {
                    yield return   (folder, gdObject);
                }
            }
        }

        public IEnumerable<(Folder, GDObject)> GetObjectsAndFolders( String path, params Type[] componentType )
        {
            foreach ( var gdObject in GetObjectsAndFolders( path ) )
            {
                if ( gdObject.Item2.HasComponents( componentType ) )
                    yield return gdObject;
            }
        }

        public Folder GetFolder( GdId folderId )
        {
            foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
            {
                if ( folder.FolderGuid == folderId.GUID )
                    return folder;
            }

            return null;
        }

        public GDObject GetObject( GdId objectId )
        {
            var guid = objectId.GUID;
            foreach ( var gdObject in AllObjects )
            {
                if( gdObject.Guid == guid )
                    return gdObject;
            }

            return null;
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
            Debug.Log( $"DB name {Name}, Folders {foldersCount}, objects {objectsCount}" );
        }

        private readonly Regex _pathPartsQuery = new (@"(^[^\/]*\/+)|([^\/]+\/+)|([^\/]*$)", RegexOptions.Singleline | RegexOptions.ExplicitCapture ); 
        

        //Custom split with preservation of slash at the folders names end
        private IReadOnlyList<String> Split( String assetPath )
        {
            if ( String.IsNullOrEmpty( assetPath ) )
                return Array.Empty<String>();

            var matches = _pathPartsQuery.Matches( assetPath );
            var result  = matches.Select( m => m.Value ).ToArray();
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
                if ( partStr.EndsWith( "/" ) )
                {
                    query = new FolderQuery( partStr );
                }
                else
                {
                    query = new FileQuery( partStr );
                }

                result.Add( query );
                if( i > 0 )
                    result[ i - 1 ].NextPart = query;
                if( query is FileQuery )
                    break;
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
                Debug.Log($"  {indentStr}{obj.Name}, type {obj.GetType().Name}, components {obj.Components.Count}");
            }
        }

        private abstract class Query 
        {
            public Query  NextPart;
            public readonly String Term;

            public abstract IEnumerable<GDObject> ProcessFolder( Folder folder );

            protected Query( String term )
            {
                Term = term;
            }

            protected readonly Regex IsWildcardsPresent = new (@"[\*\?]", RegexOptions.Singleline | RegexOptions.ExplicitCapture );

            //https://www.codeproject.com/Articles/11556/Converting-Wildcards-to-Regexes
            public static string WildcardToRegex( string pattern )
            {
                return "^" + Regex.Escape(pattern).
                                   Replace("\\*", ".*").
                                   Replace("\\?", ".") + "$";
            }
        }

        private class FileQuery : Query
        {
            public readonly Regex FileNameRegex;

            public FileQuery( String term ) : base( term )
            {
                if( String.IsNullOrEmpty( term ) )
                    term = "*";
                FileNameRegex = new Regex( WildcardToRegex( term ) );
            }

            public override IEnumerable<GDObject> ProcessFolder(Folder folder )
            {
                foreach ( var gdAsset in folder.Objects )
                {
                    if( FileNameRegex.IsMatch( gdAsset.Name ) )
                        yield return gdAsset;
                }
            }
        }

        private class FolderQuery : Query
        {
            public readonly String FolderName; 
            public readonly String Delim; 
            public readonly Regex FolderNameRegex;

            public FolderQuery( String term ) : base( term )
            {
                var delimIndex = term.IndexOf( '/' );
                Assert.IsTrue( delimIndex >= 0 );
                FolderName = term[ ..delimIndex ];
                Delim      = term[ delimIndex.. ];
                FolderNameRegex = new Regex( WildcardToRegex( FolderName ) );
            }

            public override IEnumerable<GDObject> ProcessFolder(Folder folder )
            {
                if ( Delim == "/" )
                {
                    if ( FolderNameRegex.IsMatch( folder.Name ) )
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
                else if( Delim == "//" )
                {
                    if ( FolderNameRegex.IsMatch( folder.Name ) )
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
            }
        }
    }
}