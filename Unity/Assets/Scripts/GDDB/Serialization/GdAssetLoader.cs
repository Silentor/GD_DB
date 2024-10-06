using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB laoder from Scriptable Objects in Resources folder 
    /// </summary>
    public class GdAssetLoader : GdLoader
    {
        public GdAssetLoader( [NotNull] DBAsset database )
        {
            if ( !database ) throw new ArgumentNullException( nameof(database) );

            var timer = new System.Diagnostics.Stopwatch();

            var serializer = new DBAssetSerializer();
            var rootFolder = serializer.Deserialize( database );

            _db = new GdDb( rootFolder, rootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() );

            timer.Stop();
            Debug.Log( $"[{nameof(GdAssetLoader)}] Loaded gddb {_db.Name}, objects {_db.AllObjects.Count} from asset {database.name} for {timer.ElapsedMilliseconds} ms" );
        } 
    }
}
