#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class FolderSerializer
    {
        public const String FoldersTag = ".folders";
        public const String ObjectsTag = ".objs";
        public const String NameTag    = GDObjectSerializationCommon.NameTag;
        public const String IdTag      = GDObjectSerializationCommon.IdTag;

        private readonly CustomSampler _serFolderSampler   = CustomSampler.Create( "FolderSerializer.Serialize" );
        private readonly CustomSampler _deserFolderSampler = CustomSampler.Create( "FolderSerializer.Deserialize" );

#if UNITY_EDITOR

        public void Serialize( Folder root, GDObjectSerializer objectSerializer, WriterBase writer, UInt64? hash = null )
        {
            _serFolderSampler.Begin();
            writer.SetPropertyNameAlias( 0, NameTag );
            writer.SetPropertyNameAlias( 1, IdTag );
            writer.SetPropertyNameAlias( 2, FoldersTag );
            writer.SetPropertyNameAlias( 3, ObjectsTag );
            SerializeFolder( writer, root, objectSerializer, hash );
            _serFolderSampler.End();
        }

#endif

        public Folder Deserialize( ReaderBase reader, GDObjectDeserializer? objectSerializer, out UInt64? checksum )
        {
            reader.SetPropertyNameAlias( 0, NameTag );
            reader.SetPropertyNameAlias( 1, IdTag );
            reader.SetPropertyNameAlias( 2, FoldersTag );
            reader.SetPropertyNameAlias( 3, ObjectsTag );

            checksum = 0;
            reader.ReadStartObject();
            return DeserializeFolder( reader, null, objectSerializer, out checksum );
        }

#if UNITY_EDITOR

        private void SerializeFolder( WriterBase writer, Folder folder, GDObjectSerializer objectSerializer, UInt64? hash = null )
        {
            writer.WriteStartObject();

            if ( hash.HasValue )
            {
                writer.WritePropertyName( ".hash" ).WriteValue( hash.Value );
            }
            writer.WritePropertyName( NameTag ).WriteValue( folder.Name );
            writer.WritePropertyName( IdTag ).WriteValue( folder.FolderGuid );

            writer.WritePropertyName( FoldersTag );
            writer.WriteStartArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                SerializeFolder( writer, subFolder, objectSerializer );
            }
            writer.WriteEndArray();

            writer.WritePropertyName( ObjectsTag );
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
                        objectSerializer.Serialize( obj );
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
        private Folder DeserializeFolder( ReaderBase reader, Folder? parent, GDObjectDeserializer? objectSerializer, out UInt64? hash )
        {
            _deserFolderSampler.Begin();
            reader.EnsureStartObject();

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

            reader.SeekPropertyName( NameTag );
            var name    = reader.ReadStringValue();
            var guid = reader.ReadPropertyGuid( IdTag );

            var folder = new Folder( name, guid )
            {
                Depth = parent != null ? parent.Depth + 1 : 0,
                Parent = parent
            };

            try
            {
                reader.SeekPropertyName( FoldersTag );
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
                reader.ReadPropertyName( ObjectsTag );
                if ( objectSerializer != null )
                {
                    reader.ReadStartArray();
                    while ( reader.ReadNextToken() != EToken.EndArray )
                    {
                        var gdo     = objectSerializer.Deserialize( );
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