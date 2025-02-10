using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GDDB.Queries;
using UnityEngine;
using UnityEngine.Assertions;

namespace GDDB
{
    public partial class GdDb
    {
        public String Name { get; } = "Default";

        public UInt64 Hash { get; } = 0;

        public         GdFolder                  RootFolder { get; }

        public virtual IReadOnlyList<ScriptableObject> AllObjects { get; }

        public GdDb( GdFolder dbStructure, IReadOnlyList<ScriptableObject> allObjects, UInt64 hash = 0 )
        {
            RootFolder = dbStructure;
            //Root       = allObjects.OfType<GDRoot>().Single( );
            AllObjects = allObjects;
            Hash       = hash;
            _queryExecutor = new Executor( this );
            _queryParser   = new Parser( _queryExecutor );
        }

        public IEnumerable<T> GetComponents<T>() where T : GDComponent
        {
            foreach ( var gdObj in AllObjects.OfType<GDObject>() )
            {
                foreach ( var component in gdObj.Components )
                {
                    if ( component is T t )
                        yield return t;
                }
            }
        }

        
        /// <summary>
        /// Query objects by folders/objects path. Glob syntax
        /// </summary>
        /// <param name="path">Query path of folders and files, supports * and ? wildcard for folder/object names</param>
        /// <returns></returns>
        public void FindObjects( String path, List<ScriptableObject> resultObjects, List<GdFolder> resultFolders = null )
        {
            var  query = _queryParser.ParseObjectsQuery( path );
            _queryExecutor.FindObjects( query, resultObjects, resultFolders );
        }   

        public void FindObjects( String path, Type[] components, List<ScriptableObject> resultObjects, List<GdFolder> resultFolders = null )
        {
            FindObjects( path, resultObjects, resultFolders );

            if ( resultFolders != null )
            {
                for ( int i = 0; i < resultObjects.Count; i++ )
                {
                    if ( resultObjects[ i ] is GDObject gdo && gdo.HasComponents( components ) )
                        continue;
                    else
                    {
                        resultObjects.RemoveAt( i );
                        resultFolders.RemoveAt( i );
                        i--;
                    }
                }
            }
            else
            {
                for ( int i = 0; i < resultObjects.Count; i++ )
                {
                    if ( resultObjects[ i ] is GDObject gdo && gdo.HasComponents( components ) )
                        continue;
                    else
                    {
                        resultObjects.RemoveAt( i );
                        i--;
                    }
                }
            }
        }

        // public GdFolder GetFolder( GdId folderId )
        // {
        //     foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
        //     {
        //         if ( folder.FolderGuid == folderId.GUID )
        //             return folder;
        //     }
        //
        //     return null;
        // }

        // public GDObject GetObject( GdId objectId )
        // {
        //     var guid = objectId.GUID;
        //     foreach ( var obj in AllObjects )
        //     {
        //         if( obj is GDObject gdo && gdo.Guid == guid )
        //             return gdo;
        //     }
        //
        //     return null;
        // }

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

        private readonly Parser _queryParser;
        private readonly Executor _queryExecutor;
        
        private void PrintRecursively(GdFolder folder, int indent, ref Int32 foldersCount, ref Int32 objectsCount )
        {
            foldersCount++;
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}/");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2, ref foldersCount, ref objectsCount );
            }

            foreach ( var obj in folder.Objects )
            {
                objectsCount++;
                if( obj is GDObject gdo )
                    Debug.Log($"  {indentStr}{gdo.name}, type {obj.GetType().Name}, components {gdo.Components.Count}");
                else
                    Debug.Log($"  {indentStr}{obj.name}, type {obj.GetType().Name}");
            }
        }
    }
}