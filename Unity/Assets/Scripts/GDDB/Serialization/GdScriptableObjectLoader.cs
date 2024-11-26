using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB loader from Scriptable Object format 
    /// </summary>
    public class GdScriptableObjectLoader : GdLoader
    {
        public GdScriptableObjectLoader( [NotNull] DBAsset database )
        {
            if ( !database ) throw new ArgumentNullException( nameof(database) );

            var timer = new System.Diagnostics.Stopwatch();

            var serializer = new DBScriptableObjectSerializer();
            var rootFolder = serializer.Deserialize( database );

            _db = new GdDb( rootFolder, rootFolder.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() );

            timer.Stop();
            Debug.Log( $"[{nameof(GdScriptableObjectLoader)}] Loaded gddb {_db.Name} from Scriptable Object {database.name}, objects {_db.AllObjects.Count}, folders {_db.RootFolder.EnumerateFoldersDFS(  ).Count()} for {timer.ElapsedMilliseconds} ms" );
        } 
    }
}
