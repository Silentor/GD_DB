using System;
using System.Collections.Generic;
using SimpleJSON;
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

        public JSONObject Serialize( Folder root, ObjectsJsonSerializer objectSerializer, UInt64? hash = null )
        {
            _serFolderSampler.Begin();
            var rootJson = SerializeFolder( root, objectSerializer, hash );
            _serFolderSampler.End();
            return rootJson;
        }

#endif

        public Folder Deserialize( JSONObject json, ObjectsJsonSerializer objectSerializer, out UInt64? checksum )
        {
            if( json.HasKey( "hash" ))
                checksum = json["hash"].AsULong;
            else
                checksum = null;
            return DeserializeFolder( json, null, objectSerializer );
        }

#if UNITY_EDITOR

        private JSONObject SerializeFolder( Folder folder, ObjectsJsonSerializer objectSerializer, UInt64? hash = null )
        {
            var result = new JSONObject();
            if( hash.HasValue )
                result["hash"] = hash.Value;
            result["name"] = folder.Name;
            result["depth"] = folder.Depth;
            result["guid"] = folder.FolderGuid.ToString("D");

            var subFolders = new JSONArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                subFolders.Add( SerializeFolder( subFolder, objectSerializer ) );
            }

            result["subFolders"] = subFolders;

            var objects = new JSONArray();
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

        private Folder DeserializeFolder( JSONObject json, Folder? parent, ObjectsJsonSerializer objectSerializer )
        {
            _deserFolderSampler.Begin();
            var folder = new Folder( json["name"], Guid.ParseExact( json["guid"], "D" ) )
            {
                Depth = json["depth"],
                Parent = parent
            };

            var subFolders = json["subFolders"];
            foreach ( var subFolderJson in subFolders )
            {
                var subFolder = DeserializeFolder( ( JSONObject ) subFolderJson, folder, objectSerializer );
                folder.SubFolders.Add( subFolder );
            }

            var jobjects = json["objects"];
            for ( int i = 0; i < jobjects.Count; i++ )
            {
                var objJson = jobjects[i].AsObject;
                var gdo     = objectSerializer.Deserialize( objJson );
                folder.Objects.Add( gdo );
            }

            _deserFolderSampler.End();

            return folder;
        }
    }
}