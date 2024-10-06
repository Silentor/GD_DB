using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GDDB.Serialization
{
    public class FoldersSerializer
    {
        public JObject Serialize( Folder root, Int32? hash = null )
        {
            var rootJson = SerializeFolder( root, hash );
            return rootJson;
        }

        public Folder Deserialize( JObject json, IReadOnlyList<GDObject> objects, out Int32? hash )
        {
            if( json.TryGetValue( "hash", out var hashToken ) && hashToken.Type == JTokenType.Integer )
                hash = hashToken.Value<Int32>();
            else
                hash = null;
            return DeserializeFolder( json, null, objects );
        }

        private JObject SerializeFolder( Folder folder, Int32? hash = null )
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

            var jobjects = json["objects"];
            foreach ( var objJson in jobjects )
            {
                var objId = Guid.ParseExact( objJson[ "guid" ].Value<String>() , "D" );
                folder.Objects.Add( objects.First( o => o.Guid == objId ) );                 //todo Optimize search by parallel iteration
            }

            return folder;
        }
    }
}