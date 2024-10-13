using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GDDB.Serialization
{
    public class FoldersJsonSerializer
    {
        //Parameter of Deserialize(). Ignore deserialization of GDObjects. Only folders deserialized
        public static readonly IReadOnlyList<GDObject> IgnoreGDObjects = Array.Empty<GDObject>();

        public JObject Serialize( Folder root, UInt64? hash = null )
        {
            var rootJson = SerializeFolder( root, hash );
            return rootJson;
        }

        public Folder Deserialize( JObject json, IReadOnlyList<GDObject> objects, out UInt64? checksum )
        {
            if( json.TryGetValue( "hash", out var hashValue ) && hashValue.Type == JTokenType.Integer )
                checksum = (UInt64)hashValue;
            else
                checksum = null;
            return DeserializeFolder( json, null, objects );
        }

        private JObject SerializeFolder( Folder folder, UInt64? hash = null )
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
                subFolders.Add( SerializeFolder( subFolder ) );
            }

            result["subFolders"] = subFolders;

            var objects = new JArray();
            foreach ( var obj in folder.Objects )
            {
                var objJson = new JObject();
                objJson[ "guid" ] = obj.Guid.ToString("D");
                objects.Add( objJson );
            }

            result["objects"] = objects;

            return result;
        }

        private Folder DeserializeFolder( JObject json, Folder? parent, IReadOnlyList<GDObject> objects )
        {
            var folder = new Folder( json["name"].Value<String>(), Guid.ParseExact( json["guid"].Value<String>(), "D" ) )
            {
                Depth = json["depth"].Value<Int32>(),
                Parent = parent
            };

            var subFolders = json["subFolders"];
            foreach ( var subFolderJson in subFolders )
            {
                var subFolder = DeserializeFolder( ( JObject ) subFolderJson, folder, objects );
                folder.SubFolders.Add( subFolder );
            }

            if ( objects != IgnoreGDObjects )
            {
                var jobjects = json["objects"];
                foreach ( var objJson in jobjects )
                {
                    var objId = Guid.ParseExact( objJson[ "guid" ].Value<String>() , "D" );
                    folder.Objects.Add( objects.First( o => o.Guid == objId ) );                 //todo Optimize search by parallel iteration
                }
            }

            return folder;
        }
    }
}