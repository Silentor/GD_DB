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
            return DeserializeFolder( json, null, objectSerializer );
        }

#if UNITY_EDITOR

        private JObject SerializeFolder( Folder folder, ObjectsJsonSerializer objectSerializer, UInt64? hash = null )
        {
            var result = new JObject();
            if( hash.HasValue )
                result["hash"] = hash.Value;
            result["name"] = folder.Name;
            result["depth"] = folder.Depth;
            result["guid"] = folder.FolderGuid.ToString("D");

            var subFolders = new JArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                subFolders.Add( SerializeFolder( subFolder, objectSerializer ) );
            }

            result["subFolders"] = subFolders;

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

        private Folder DeserializeFolder( JsonReader json, Folder? parent, ObjectsJsonSerializer objectSerializer )
        {
            _deserFolderSampler.Begin();
            //json.Read();        Assert.IsTrue( json.TokenType == JsonToken.StartObject );
            json.Read();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName && (String)json.Value == "name" );      
            var name = json.ReadAsString()!;                      
            json.Read();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName && 
                                               (String)json.Value == "guid" );      
            var guid = Guid.ParseExact( json.ReadAsString()!, "D" );

            var folder = new Folder( name, guid )
            {
                Depth = parent != null ? parent.Depth + 1 : 0,
                Parent = parent
            };

            json.Read();        Assert.IsTrue( (String)json.Value == "subfolders" );
            json.Read();        Assert.IsTrue( json.TokenType  == JsonToken.StartArray  );
            json.Read();        var objectStartOrArrayEnd = json.TokenType;
            while( objectStartOrArrayEnd == JsonToken.StartObject )
            {
                var subFolder = DeserializeFolder( json, folder, objectSerializer );
                folder.SubFolders.Add( subFolder );
                json.Read();        objectStartOrArrayEnd = json.TokenType;
            }
            Assert.IsTrue( json.TokenType  == JsonToken.EndArray  );

            json.Read();        Assert.IsTrue( (String)json.Value == "objects" );
            json.Read();        Assert.IsTrue( json.TokenType     == JsonToken.StartArray  );
            json.Read();        objectStartOrArrayEnd = json.TokenType;
            while( objectStartOrArrayEnd == JsonToken.StartObject )
            {
                var gdo     = objectSerializer.Deserialize( json );
                folder.Objects.Add( gdo );
                json.Read();        objectStartOrArrayEnd = json.TokenType;
            }

            _deserFolderSampler.End();

            return folder;
        }
    }
}