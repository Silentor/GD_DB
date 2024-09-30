using System;
using System.Linq;
using GDDB.Serialization;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// GD DB laoder from Scriptable Objects in Resources folder 
    /// </summary>
    public class GdAssetLoader : GdLoader
    {
        public GdAssetLoader( String name )
        {
            var timer = new System.Diagnostics.Stopwatch();

            var path = $"{name}.folders";
            var dbAsset = Resources.Load<DBAsset>( path );

            var serializer = new DBAssetSerializer();
            var rootFolder = serializer.Deserialize( dbAsset );

            _db = new GdDb( rootFolder, rootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() );

            timer.Stop();
            Debug.Log( $"[{nameof(GdAssetLoader)}]-[{nameof(GdAssetLoader)}] Loaded gddb {name} objects {_db.AllObjects.Count} from asset for {timer.ElapsedMilliseconds} ms" );
        } 
    }
}
