#nullable enable
using System;
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

        public void Serialize( GdFolder root, GDObjectSerializer objectSerializer, WriterBase writer )
        {
            _serFolderSampler.Begin();
            SerializeFolder( writer, root, objectSerializer );
            _serFolderSampler.End();
        }

#endif

        public GdFolder Deserialize( ReaderBase reader, GDObjectDeserializer? objectSerializer )
        {
            reader.ReadStartObject();
            return DeserializeFolder( reader, null, objectSerializer );
        }

#if UNITY_EDITOR

        private void SerializeFolder( WriterBase writer, GdFolder folder, GDObjectSerializer objectSerializer )
        {
            writer.WriteStartObject();

            // if ( hash.HasValue )
            // {
            //     writer.WritePropertyName( ".hash" ).WriteValue( hash.Value );
            // }
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
                    if ( obj is not GDObject gdo || gdo.EnabledObject )
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
        private GdFolder DeserializeFolder( ReaderBase reader, GdFolder? parent, GDObjectDeserializer? objectSerializer )
        {
            _deserFolderSampler.Begin();
            reader.EnsureStartObject();

            reader.SeekPropertyName( NameTag );
            var name    = reader.ReadStringValue();
            var guid = reader.ReadPropertyGuid( IdTag );

            var folder = new GdFolder( name, guid )
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
                    var subFolder = DeserializeFolder( reader, folder, objectSerializer );
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