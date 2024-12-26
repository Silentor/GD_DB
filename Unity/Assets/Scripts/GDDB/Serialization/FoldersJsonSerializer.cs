#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class FoldersJsonSerializer
    {
        private readonly CustomSampler _serFolderSampler   = CustomSampler.Create( "FoldersJsonSerializer.Serialize" );
        private readonly CustomSampler _deserFolderSampler = CustomSampler.Create( "FoldersJsonSerializer.Deserialize" );

#if UNITY_EDITOR

        public void Serialize( Folder root, ObjectsDataSerializer objectSerializer, WriterBase writer, UInt64? hash = null )
        {
            _serFolderSampler.Begin();
            SerializeFolder( writer, root, objectSerializer, hash );
            _serFolderSampler.End();
        }

#endif

        public Folder Deserialize( ReaderBase reader, ObjectsJsonDeserializer? objectSerializer, out UInt64? checksum )
        {
            // if( json.HasKey( "hash" ))
            //     checksum = json["hash"].AsULong;
            // else
            //     checksum = null;
            checksum = 0;
            reader.ReadStartObject();
            return DeserializeFolder( reader, null, objectSerializer, out checksum );
        }

#if UNITY_EDITOR

        private void SerializeFolder( WriterBase writer, Folder folder, ObjectsDataSerializer objectSerializer, UInt64? hash = null )
        {
            writer.WriteStartObject();

            if ( hash.HasValue )
            {
                writer.WritePropertyName( "hash" ).WriteValue( hash.Value );
            }
            writer.WritePropertyName( "name" ).WriteValue( folder.Name );
            writer.WritePropertyName( "guid" ).WriteValue( folder.FolderGuid.ToString("D") );

            writer.WritePropertyName( "subfolders" );
            writer.WriteStartArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                SerializeFolder( writer, subFolder, objectSerializer );
            }
            writer.WriteEndArray();

            writer.WritePropertyName( "objects" );
            writer.WriteStartArray();
            if( objectSerializer != null )
            {
                foreach ( var obj in folder.Objects )
                {
                    //Guid string write
                    //objects.Add( obj.Guid.ToString("D") );

                    //Guid object write
                    //var objJson = new JSONObject();
                    //objJson[ "guid" ] = obj.Guid.ToString("D");
                    //objects.Add( objJson );

                    //Full embed object write
                    if ( obj.EnabledObject )
                    {
                        objectSerializer.Serialize( obj, writer );
                    }
                }
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

#endif

        /// <summary>
        /// Already stands on StartObject token
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parent"></param>
        /// <param name="objectSerializer"></param>
        /// <returns></returns>
        private Folder DeserializeFolder( ReaderBase reader, Folder? parent, ObjectsJsonDeserializer? objectSerializer, out UInt64? hash )
        {
            _deserFolderSampler.Begin();
            reader.EnsureStartObject();

            var propName = reader.ReadPropertyName();
            if ( propName == "version" )        //Not implemented for now
            {
                reader.SkipProperty(  );
                propName = reader.ReadPropertyName();
            }

            hash = null;
            if ( propName == "hash" )
            {
                hash = reader.ReadUInt64Value( );
            }

            //Can be another properties in the future...

            reader.SeekPropertyName( "name" );
            var name    = reader.ReadStringValue();
            var guidStr = reader.ReadPropertyString( "guid" );
            var guid    = Guid.ParseExact( guidStr, "D" );

            var folder = new Folder( name, guid )
            {
                Depth = parent != null ? parent.Depth + 1 : 0,
                Parent = parent
            };

            try
            {
                reader.SeekPropertyName( "subfolders" );
                reader.ReadStartArray();
                while ( reader.ReadNextToken() != EToken.EndArray )
                {
                    var subFolder = DeserializeFolder( reader, folder, objectSerializer, out _ );
                    folder.SubFolders.Add( subFolder );
                }
                reader.EnsureEndArray();
            }
            catch ( Exception e )
            {
                throw new ReaderFolderException( folder, reader, $"Error deserializing subfolders of folder {folder.Name}", e );
            }

            try
            {
                reader.ReadPropertyName( "objects" );
                if ( objectSerializer != null )
                {
                    reader.ReadStartArray();
                    while ( reader.ReadNextToken() != EToken.EndArray )
                    {
                        var gdo     = objectSerializer.Deserialize( reader );
                        folder.Objects.Add( gdo );
                    }
                }
                else
                    reader.SkipProperty();
            }
            catch ( Exception e )
            {
                throw new ReaderFolderException( folder, reader, $"Error deserializing objects of folder {folder.Name}", e );
            }

            reader.ReadEndObject();

            _deserFolderSampler.End();

            return folder;
        }
    }
}