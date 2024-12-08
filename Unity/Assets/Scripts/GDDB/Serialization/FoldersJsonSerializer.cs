using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class FoldersJsonSerializer
    {
        //Parameter of Deserialize(). Ignore deserialization of GDObjects. Only folders deserialized
        public static readonly IReadOnlyList<GDObject> IgnoreGDObjects = Array.Empty<GDObject>();

        private          CustomSampler _serFolderSampler   = CustomSampler.Create( "FoldersJsonSerializer.Serialize" );
        private readonly CustomSampler _deserFolderSampler = CustomSampler.Create( "FoldersJsonSerializer.Deserialize" );

#if UNITY_EDITOR

        public JObject Serialize( Folder root, ObjectsJsonSerializer objectSerializer, UInt64? hash = null )
        {
            _serFolderSampler.Begin();
            var rootJson = SerializeFolder( root, objectSerializer, hash );
            _serFolderSampler.End();
            return rootJson;
        }

#endif

        public Folder Deserialize( JsonReader json, ObjectsJsonSerializer objectSerializer, out UInt64? checksum )
        {
            // if( json.HasKey( "hash" ))
            //     checksum = json["hash"].AsULong;
            // else
            //     checksum = null;
            checksum = 0;
            json.EnsureNextToken( JsonToken.StartObject );
            return DeserializeFolder( json, null, objectSerializer );
        }

#if UNITY_EDITOR

        private JObject SerializeFolder( Folder folder, ObjectsJsonSerializer objectSerializer, UInt64? hash = null )
        {
            var result = new JObject();
            if( hash.HasValue )
                result["hash"] = hash.Value;
            result["name"] = folder.Name;
            //result["depth"] = folder.Depth;
            result["guid"] = folder.FolderGuid.ToString("D");

            var subFolders = new JArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                subFolders.Add( SerializeFolder( subFolder, objectSerializer ) );
            }

            result["subfolders"] = subFolders;

            var objects = new JArray();
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
                        var objJson = objectSerializer.Serialize( obj );
                        objects.Add( objJson );
                    }
                }
            }

            result["objects"] = objects;

            return result;
        }

#endif

        /// <summary>
        /// Already stands on StartObject token
        /// </summary>
        /// <param name="json"></param>
        /// <param name="parent"></param>
        /// <param name="objectSerializer"></param>
        /// <returns></returns>
        private Folder DeserializeFolder( JsonReader json, Folder? parent, ObjectsJsonSerializer objectSerializer )
        {
            _deserFolderSampler.Begin();
            json.EnsureToken( JsonToken.StartObject );
            var name    = json.ReadPropertyString( "name", false );
            var guidStr = json.ReadPropertyString( "guid", false );
            var guid    = Guid.ParseExact( guidStr, "D" );

            var folder = new Folder( name, guid )
            {
                Depth = parent != null ? parent.Depth + 1 : 0,
                Parent = parent
            };

            try
            {
                json.EnsureNextProperty( "subfolders" );
                json.EnsureNextToken( JsonToken.StartArray );
                json.Read();        var objectStartOrArrayEnd = json.TokenType;
                while( objectStartOrArrayEnd == JsonToken.StartObject )
                {
                    var subFolder = DeserializeFolder( json, folder, objectSerializer );
                    folder.SubFolders.Add( subFolder );
                    json.Read();        objectStartOrArrayEnd = json.TokenType;
                }
                json.EnsureToken( JsonToken.EndArray );
            }
            catch ( Exception e )
            {
                throw new JsonFolderException( folder, json, $"Error deserializing subfolders of folder {folder.Name}", e );
            }

            try
            {
                json.EnsureNextProperty( "objects" );
                json.EnsureNextToken( JsonToken.StartArray );
                json.Read();        var objectStartOrArrayEnd = json.TokenType;
                while( objectStartOrArrayEnd == JsonToken.StartObject )
                {
                    var gdo     = objectSerializer.Deserialize( json );
                    folder.Objects.Add( gdo );
                    json.Read();        objectStartOrArrayEnd = json.TokenType;
                }
            }
            catch ( Exception e )
            {
                throw new JsonFolderException( folder, json, $"Error deserializing objects of folder {folder.Name}", e );
            }

            json.EnsureNextToken( JsonToken.EndObject );
            _deserFolderSampler.End();

            return folder;
        }
    }
}