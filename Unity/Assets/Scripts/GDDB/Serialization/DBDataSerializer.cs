using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GDDB.Serialization
{
    public class DBDataSerializer
    {
#if UNITY_EDITOR
        public void Serialize( WriterBase writer, Folder rootFolder, IReadOnlyList<GDObject> objects, IGdAssetResolver assetsResolver )
        {
            var timer             = System.Diagnostics.Stopwatch.StartNew();

            var objectsSerializer = new GDObjectSerializer( writer );
            var folderSerializer  = new FolderSerializer( );
            folderSerializer.Serialize( rootFolder, objectsSerializer, writer );

            timer.Stop();
            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] serialized db to json, objects {objects.Count}, folders {rootFolder.EnumerateFoldersDFS(  ).Count()} referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );
        }

#endif

        // public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( String jsonString, IGdAssetResolver assetsResolver )
        // {
        //     return Deserialize( new StringReader( jsonString ), assetsResolver );
        // }
        //
        // public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( Stream jsonFile, IGdAssetResolver assetsResolver )
        // {
        //     return Deserialize( new StreamReader( jsonFile, Encoding.UTF8, false, 1024, true ), assetsResolver );
        // }

        public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( ReaderBase reader, IGdAssetResolver assetsResolver, out UInt64? hash )
        {
            var       timer             = System.Diagnostics.Stopwatch.StartNew();
            var       objectsSerializer = new GDObjectDeserializer( reader );
            var       foldersSerializer = new FolderSerializer();
            var       rootFolder        = foldersSerializer.Deserialize( reader, objectsSerializer, out hash );
            objectsSerializer.ResolveGDObjectReferences();

            timer.Stop();
            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Deserialize)}] deserialized db from {reader.GetType().Name}, objects {objectsSerializer.LoadedObjects.Count}, referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );

            return (rootFolder, objectsSerializer.LoadedObjects);
        }
    }
}