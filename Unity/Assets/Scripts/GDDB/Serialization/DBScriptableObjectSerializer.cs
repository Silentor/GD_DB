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
        public DBAsset Serialize( Folder rootFolder )
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
                                                 Guid    = new SerializableGuid { Guid = folder.FolderGuid },
                                         };
                folders.Add( serializableFolder );
            }

            var result = ScriptableObject.CreateInstance<DBAsset>();
            result.Folders = folders; 
            result.Hash = rootFolder.GetFoldersStructureChecksum();

            timer.Stop();
            Debug.Log( $"[{nameof(DBScriptableObjectSerializer)}]-[{nameof(Serialize)}] serialized db {rootFolder.Name} ({folders.SelectMany( f => f.Objects ).Count()} objects, {folders.Count} folders) to Scriptable object for {timer.ElapsedMilliseconds} ms" );

            return result;
        }       

        public Folder Deserialize( DBAsset asset )
        {
            if ( asset.Folders.Count == 0 )
                return null;

            var timer = new System.Diagnostics.Stopwatch();

            var index = 0;
            var rootFolder = LoadFolder( asset.Folders, ref index, null );

            timer.Stop();
            Debug.Log( $"[{nameof(DBScriptableObjectSerializer)}]-[{nameof(Deserialize)}] deserialized db folders from Scriptable object {asset.name} for {timer.ElapsedMilliseconds} ms" );

            return rootFolder;
        }

        private Folder LoadFolder( List<SerializableFolder> folders, ref Int32 index, Folder parent )
        {
            var myData = folders[ index ];
            var depth = myData.Depth;
            var name = myData.Name;
            var guid = myData.Guid.Guid;
            
            var folder = parent != null ? new Folder( name, guid, parent ) : new Folder( name, guid );
            folder.Depth = depth;
            folder.Objects.AddRange( myData.Objects );

            index++;
            if( index >= folders.Count )
                return folder;

            var nextFolder = folders[ index ];
            if( nextFolder.Depth > folder.Depth )              
            {
                while ( index < folders.Count && folders[ index ].Depth > folder.Depth )
                {
                    LoadFolder( folders, ref index, folder );
                }
            }

            return folder;
        }
    }

    
}