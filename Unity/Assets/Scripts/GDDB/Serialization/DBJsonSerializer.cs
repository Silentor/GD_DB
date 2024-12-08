using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class DBJsonSerializer
    {
        private readonly CustomSampler _jsonParserTimer = CustomSampler.Create( "DBJsonSerializer.SimpleJSON.Parse" );

#if UNITY_EDITOR
        public JObject Serialize( Folder rootFolder, IReadOnlyList<GDObject> objects, IGdAssetResolver assetsResolver )
        {
            var timer             = System.Diagnostics.Stopwatch.StartNew();

            var objectsSerializer = new ObjectsJsonSerializer();
            var folderSerializer  = new FoldersJsonSerializer();
            var foldersJson       = folderSerializer.Serialize( rootFolder, objectsSerializer );

            timer.Stop();
            Debug.Log( $"[{nameof(DBJsonSerializer)}]-[{nameof(Serialize)}] serialized db to json, objects {objects.Count}, folders {rootFolder.EnumerateFoldersDFS(  ).Count()} referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );

            return foldersJson;
        }
#endif

        public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( String json, IGdAssetResolver assetsResolver )
        {
            var timer             = System.Diagnostics.Stopwatch.StartNew();

            using var stringReader      = new System.IO.StringReader( json );
            using var jsonReader        = new Newtonsoft.Json.JsonTextReader( stringReader );
            var       objectsSerializer = new ObjectsJsonSerializer();
            var       foldersSerializer = new FoldersJsonSerializer();
            var       rootFolder        = foldersSerializer.Deserialize( jsonReader, objectsSerializer, out _ );
            objectsSerializer.ResolveGDObjectReferences();

            timer.Stop();
            Debug.Log( $"[{nameof(DBJsonSerializer)}]-[{nameof(Deserialize)}] deserialized db from json string, objects {objectsSerializer.LoadedObjects.Count}, referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );

            return (rootFolder, objectsSerializer.LoadedObjects);
        }
    }
}