using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class DBDataSerializer
    {
        private readonly CustomSampler _deserializeSampler = CustomSampler.Create( $"{nameof(DBDataSerializer)}.{nameof(Deserialize)}" );
        private readonly CustomSampler _compressAnalyzeSampler = CustomSampler.Create( $"{nameof(DBDataSerializer)}.CompressAnalyze" );

#if UNITY_EDITOR
        public void Serialize( WriterBase writer, Folder rootFolder, IGdAssetResolver assetsResolver )
        {
            var hash  = rootFolder.GetFoldersChecksum();

            var timer = System.Diagnostics.Stopwatch.StartNew();

            CompressAnalyzer.TokenData[] tokensToCompress = null;
            var isCompressible = writer is BinaryWriter;
            isCompressible = false;
            if ( isCompressible )
            {
                _compressAnalyzeSampler.Begin();
                //Implement type compression processing
                //First pass
                var buffer            = new MemoryStream();
                var bufferWriter      = new BinaryWriter( buffer );
                var compressObjSerializer = new GDObjectSerializer( bufferWriter );
                var compressFolderSerializer  = new FolderSerializer( );
                compressFolderSerializer.Serialize( rootFolder, compressObjSerializer, bufferWriter );
                Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] First pass serialized, time {timer.ElapsedMilliseconds} ms" );
                var compressor       = new CompressAnalyzer();
                tokensToCompress = compressor.GetCommonDataTokens( new BinaryReader( buffer.ToArray() ) ).Take( 100 ).ToArray();
                _compressAnalyzeSampler.End();
                Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] Compressions analysis ended, time {timer.ElapsedMilliseconds} ms" );
            }

            var objectsSerializer = new GDObjectSerializer( writer );
            var folderSerializer  = new FolderSerializer( );

            writer.WriteStartObject();
            writer.WritePropertyName( ".hash" );
            writer.WriteValue( hash );
            if ( isCompressible && tokensToCompress.Length > 0 )
            {
                //Save aliases to output
                writer.WritePropertyName( ".aliases" );
                writer.WriteStartArray();
                for ( int i = 0; i < tokensToCompress.Length; i++ )
                {
                    writer.WriteStartArray();
                    writer.WriteValue( (Byte)i );
                    writer.WriteValue( (Byte)tokensToCompress[i].Token );
                    writer.WriteValue( tokensToCompress[i].Value );
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();

                //Actually set aliases for compression
                for ( int i = 0; i < tokensToCompress.Length; i++ )
                {
                    writer.SetAlias( (Byte)i, tokensToCompress[i].Token, tokensToCompress[i].Value );
                    Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] set alias id {i}, token {tokensToCompress[i].Token}, value '{tokensToCompress[i].Value}'" );
                }
            }

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
                propName = reader.ReadPropertyName();
            }

            if( propName == ".aliases" )              //Read compression aliases if data is compressed
            {
                var aliases = new List<(Byte, CompressAnalyzer.TokenData)>();
                reader.ReadStartArray();
                while ( reader.ReadNextToken() != EToken.EndArray )
                {
                    reader.EnsureStartArray();
                    var id    = reader.ReadUInt8Value();
                    var token = (EToken)reader.ReadUInt8Value();
                    var value = reader.ReadStringValue();
                    reader.ReadEndArray();
                    aliases.Add( (id, new CompressAnalyzer.TokenData { Token = token, Value = value } ));
                }
                reader.EnsureEndArray();

                foreach ( var alias in aliases )                    
                    reader.SetAlias( alias.Item1, alias.Item2.Token, alias.Item2.Value );

                propName = reader.ReadPropertyName();
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