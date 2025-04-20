using System;
using System.Collections.Generic;
using System.Linq;
using Gddb.Queries;
using JetBrains.Annotations;
using UnityEngine;

namespace Gddb
{
    public partial class GdDb
    {
        public String Name { get; } = "Default";

        public UInt64 Hash { get; } = 0;

        public         GdFolder                  RootFolder { get; }

        public virtual IReadOnlyList<GdObjectInfo> AllObjects { get; }

        public GdDb( GdFolder dbStructure, IReadOnlyList<GdObjectInfo> allObjects, UInt64 hash = 0 )
        {
            RootFolder = dbStructure;
            //Root       = allObjects.OfType<GDRoot>().Single( );
            AllObjects         = allObjects;
            Hash               = hash;
            _queryExecutor     = new Executor( this );
            _queryParser       = new Parser( _queryExecutor );

            _objectSearchIndex = allObjects.ToArray();
            Array.Sort( _objectSearchIndex );                   //Prepare gd object search index, sorted by guid
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
        /// <param name="query">Query path of folders and files, supports * and ? wildcard for folder/object names.
        /// null - return ALL object (like filter is absent).
        /// empty - return 0 objects (like filter is present but empty).
        /// </param>
        /// <returns></returns>
        public FindObjectResult FindObjects( String query, List<ScriptableObject> resultObjects, List<GdFolder> resultFolders = null )
        {
            var  parsedQuery = _queryParser.ParseObjectsQuery( query );
            _queryExecutor.FindObjects( parsedQuery, RootFolder, resultObjects, resultFolders );
            return new FindObjectResult( resultObjects, resultFolders );
        }   

        public void FindFolders( String query, List<GdFolder> resultFolders )
        {
            var  parsedQuery = _queryParser.ParseFoldersQuery( query );
            _queryExecutor.FindFolders( parsedQuery, RootFolder, resultFolders );
        }

        public GdFolder GetFolder( Guid folderId )
        {
            foreach ( var folder in RootFolder.EnumerateFoldersDFS(  ) )
            {
                if ( folder.FolderGuid == folderId )
                    return folder;
            }
        
            return null;
        }

        public GdFolder GetFolder( GdFolderRef folderRef )
        {
            return GetFolder( folderRef.Guid );
        } 
        
        public ScriptableObject GetObject( GdRef objectId )
        {
            var guid  = objectId.Guid;
            var index = Array.BinarySearch( _objectSearchIndex, new GdObjectInfo( guid, null, null ) );
            if( index >= 0 )
                return (GDObject)_objectSearchIndex[ index ].Object; 
        
            return null;
        }

        public GdFolder GetFolder( [NotNull] ScriptableObject gdobject )
        {
            if ( gdobject == null ) throw new ArgumentNullException( nameof(gdobject) );
            for ( int i = 0; i < _objectSearchIndex.Length; i++ )
            {
                if ( _objectSearchIndex[ i ].Object == gdobject )
                    return _objectSearchIndex[ i ].Folder;
            }

            return null;
        }

        // public GDObject GetObject( GdType type )
        // {
        //     return AllObjects.First( o => o.Type == type );
        // }

        public void Print( )
        {
            var foldersCount = 0;
            var objectsCount = 0;
            PrintRecursively( RootFolder, 0, ref foldersCount, ref objectsCount );
            Debug.Log( $"DB name {Name}, Folders {foldersCount}, objects {objectsCount}" );
        }

        private readonly Parser                     _queryParser;
        private readonly Executor      _queryExecutor;
        private readonly GdObjectInfo[]        _objectSearchIndex;
        
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

        public readonly struct FindObjectResult
        {
            public readonly List<ScriptableObject> Objects;
            public readonly List<GdFolder> Folders;

            public FindObjectResult([NotNull] List<ScriptableObject> objects, [CanBeNull] List<GdFolder> folders )
            {
                Objects = objects ?? throw new ArgumentNullException( nameof(objects) );
                Folders = folders;
            }

            public FindObjectResult FindObjectType<TGdObjType>( ) where TGdObjType : ScriptableObject
            {
                return FindObjectType( typeof(TGdObjType) );
            }

            public FindObjectResult FindObjectType( [NotNull] Type objectType )
            {
                if ( objectType == null ) throw new ArgumentNullException( nameof(objectType) );
                if( !typeof(ScriptableObject).IsAssignableFrom( objectType ) )
                    throw new ArgumentException( $"GDObject type must be derived from ScriptableObject, but was {objectType.Name}", nameof(objectType) );

                if ( objectType == typeof(ScriptableObject) )
                    return this;

                if( Folders != null )
                {
                    for ( var i = Objects.Count - 1; i >= 0; i-- )
                    {
                        if ( !objectType.IsAssignableFrom( Objects[ i ].GetType() ) )
                        {
                            Objects.RemoveAt( i );
                            Folders.RemoveAt( i );
                        }
                    }
                }
                else
                {
                    for ( var i = Objects.Count - 1; i >= 0; i-- )
                    {
                        if ( !objectType.IsAssignableFrom( Objects[ i ].GetType() ) )
                        {
                            Objects.RemoveAt( i );
                        }                    
                    }
                }

                return this;
            }

            public FindObjectResult FindComponents( [NotNull] IReadOnlyList<Type> components )
            {
                if ( components == null ) throw new ArgumentNullException( nameof(components) );
                foreach ( var component in components )
                {
                    if( !typeof(GDComponent).IsAssignableFrom( component ) )
                        throw new ArgumentException( $"GDObject type must be derived from ScriptableObject, but was {component.Name}", nameof(component) );
                }

                if ( Folders != null )
                {
                    for ( var i = Objects.Count - 1; i >= 0; i-- )
                    {
                        if ( Objects[i] is not GDObject gdo || !gdo.HasComponents( components ) )
                        {
                            Objects.RemoveAt( i );
                            Folders.RemoveAt( i );
                        }                    
                    }
                }
                else
                {
                    for ( var i = Objects.Count - 1; i >= 0; i-- )
                    {
                        if ( Objects[i] is not GDObject gdo || !gdo.HasComponents( components ) )
                        {
                            Objects.RemoveAt( i );
                        }                    
                    }
                }

                return this;
            }

            public FindObjectResult FindComponents( [NotNull] params Type[] components )
            {
                return FindComponents( (IReadOnlyList<Type>)components );
            }

            public FindObjectResult FindComponents<TGdComp>(  )
            {
                return FindComponents( typeof(TGdComp) );
            }
        }
    }
}