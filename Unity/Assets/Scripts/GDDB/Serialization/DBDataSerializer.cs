using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class DBDataSerializer
    {
        private readonly CustomSampler _deserializeSampler = CustomSampler.Create( $"{nameof(DBDataSerializer)}.{nameof(Deserialize)}" );

#if UNITY_EDITOR
        public void Serialize(    WriterBase writer, Folder rootFolder, IGdAssetResolver assetsResolver )
        {
            var hash  = rootFolder.GetFoldersChecksum();

            var timer = System.Diagnostics.Stopwatch.StartNew();

            var objectsSerializer = new GDObjectSerializer( writer );
            var folderSerializer  = new FolderSerializer( );

            writer.WriteStartObject();
            writer.WritePropertyName( ".hash" );
            writer.WriteValue( hash );
            writer.WritePropertyName( ".folders" );
            folderSerializer.Serialize( rootFolder, objectsSerializer, writer );
            writer.WriteEndObject();

            timer.Stop();


            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] serialized db to format {writer.GetType().Name}, objects {objectsSerializer.ObjectsWritten}, folders {rootFolder.EnumerateFoldersDFS(  ).Count()} referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );
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
            _deserializeSampler.Begin();
            var       timer             = System.Diagnostics.Stopwatch.StartNew();
            var       objectsSerializer = new GDObjectDeserializer( reader );
            var       foldersSerializer = new FolderSerializer();

            reader.ReadStartObject();
            var propName = reader.ReadPropertyName();
            if ( propName == ".version" )        //Not implemented for now
            {
                reader.SkipProperty(  );
                propName = reader.ReadPropertyName();
            }

            hash = null;
            if ( propName == ".hash" )
            {
                hash = reader.ReadUInt64Value( );
            }

            //Can be another properties in the future...


            reader.SeekPropertyName( ".folders" );
            var       rootFolder        = foldersSerializer.Deserialize( reader, objectsSerializer );
            reader.ReadEndObject();

            objectsSerializer.ResolveGDObjectReferences();

            timer.Stop();
            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Deserialize)}] deserialized db from {reader.GetType().Name}, objects {objectsSerializer.LoadedObjects.Count}, referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );
            _deserializeSampler.End();

            return (rootFolder, objectsSerializer.LoadedObjects);
        }
    }
}