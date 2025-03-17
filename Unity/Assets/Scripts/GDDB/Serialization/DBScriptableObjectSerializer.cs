using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// Serialize folders tree to one unity asset
    /// </summary>
    public class DBScriptableObjectSerializer
    {

#if UNITY_EDITOR
        public DBScriptableObject Serialize( GdFolder rootFolder )
        {
            var timer = new System.Diagnostics.Stopwatch();

            var folders  = new List<SerializableFolder>();
            foreach ( var folder in rootFolder.EnumerateFoldersDFS(  ) )
            {
                var serializableFolder = new SerializableFolder()
                                         {
                                                 Name    = folder.Name,
                                                 Depth   = folder.Depth,
                                                 Objects = folder.Objects,
                                                 ObjectIds = folder.Objects.Select( o => new SerializableGuid( GetGDObjectGuid(o)) ).ToList(),
                                                 Guid    = new SerializableGuid { Guid = folder.FolderGuid },
                                         };
                folders.Add( serializableFolder );
            }

            var result = ScriptableObject.CreateInstance<DBScriptableObject>();
            result.Folders = folders; 
            result.Hash = rootFolder.GetFoldersChecksum();

            timer.Stop();
            Debug.Log( $"[{nameof(DBScriptableObjectSerializer)}]-[{nameof(Serialize)}] serialized db {rootFolder.Name} ({folders.SelectMany( f => f.Objects ).Count()} objects, {folders.Count} folders) to Scriptable object for {timer.ElapsedMilliseconds} ms" );

            return result;
        }

        private Guid GetGDObjectGuid( ScriptableObject gdobject )
        {
            if ( gdobject is GDObject gdo )
                return gdo.Guid;
            else if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( gdobject, out var guidString, out _ ) )
                return Guid.ParseExact( guidString, "N" );
            else return Guid.Empty;
        }

#endif

        public GdFolder Deserialize( DBScriptableObject dbso, List<GdDb.ObjectSearchIndex> allObjects )
        {
            if ( dbso.Folders.Count == 0 )
                return null;

            var timer = new System.Diagnostics.Stopwatch();

            var index = 0;
            var rootFolder = LoadFolder( dbso.Folders, ref index, null, allObjects );

            timer.Stop();
            Debug.Log( $"[{nameof(DBScriptableObjectSerializer)}]-[{nameof(Deserialize)}] deserialized db folders from Scriptable object {dbso.name} for {timer.ElapsedMilliseconds} ms" );

            return rootFolder;
        }

        private GdFolder LoadFolder( List<SerializableFolder> folders, ref Int32 index, GdFolder parent, List<GdDb.ObjectSearchIndex> allObjects  )
        {
            var myData = folders[ index ];
            var depth = myData.Depth;
            var name = myData.Name;
            var guid = myData.Guid.Guid;
            
            var folder = parent != null ? new GdFolder( name, guid, parent ) : new GdFolder( name, guid );
            folder.Depth = depth;
            folder.Objects.AddRange( myData.Objects );
            for ( int i = 0; i < myData.Objects.Count; i++ )                
                allObjects.Add( new GdDb.ObjectSearchIndex( myData.ObjectIds[i] , myData.Objects[i], folder ) );

            index++;
            if( index >= folders.Count )
                return folder;

            var nextFolder = folders[ index ];
            if( nextFolder.Depth > folder.Depth )              
            {
                while ( index < folders.Count && folders[ index ].Depth > folder.Depth )
                {
                    LoadFolder( folders, ref index, folder, allObjects );
                }
            }

            return folder;
        }
    }

    
}