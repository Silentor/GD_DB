using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GDDB.Serialization
{
    public class DBJsonSerializer
    {

#if UNITY_EDITOR
        public String Serialize( Folder rootFolder, IReadOnlyList<GDObject> objects, IGdAssetResolver assetsResolver )
        {
            var timer             = System.Diagnostics.Stopwatch.StartNew();

            var folderSerializer  = new FoldersJsonSerializer();
            var foldersJson       = folderSerializer.Serialize( rootFolder );
            var objectsSerializer = new ObjectsJsonSerializer();
            var objectsJson       = objectsSerializer.Serialize( objects, assetsResolver );
            var json = new JObject
                       {
                               { "folders", foldersJson },
                               { "objects", objectsJson },
                       };
            var result = json.ToString();

            timer.Stop();
            Debug.Log( $"[{nameof(DBJsonSerializer)}]-[{nameof(Serialize)}] serialized db to json, objects {objects.Count}, folders {rootFolder.EnumerateFoldersDFS(  ).Count()} referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );

            return result;
        }
#endif

        public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( String json, IGdAssetResolver assetsResolver )
        {
            var timer             = System.Diagnostics.Stopwatch.StartNew();

            var dom = JObject.Parse( json );
            var objectsSerializer = new ObjectsJsonSerializer();
            var objectsJson = (JArray)dom["objects"];
            var objects    = objectsSerializer.Deserialize( objectsJson, assetsResolver );
            var foldersSerializer = new FoldersJsonSerializer();
            var foldersJson = (JObject)dom["folders"];
            var rootFolder = foldersSerializer.Deserialize( foldersJson, objects, out _ );

            timer.Stop();
            Debug.Log( $"[{nameof(DBJsonSerializer)}]-[{nameof(Deserialize)}] deserialized db from json string, objects {objects.Count}, referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );

            return (rootFolder, objects);
        }
    }
}