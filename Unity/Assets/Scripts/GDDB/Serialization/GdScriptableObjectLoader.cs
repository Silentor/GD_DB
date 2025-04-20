using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Gddb.Serialization
{
    /// <summary>
    /// GD DB loader from Scriptable Object format 
    /// </summary>
    public class GdScriptableObjectLoader : GdLoader
    {
        public GdScriptableObjectLoader( [NotNull] DBScriptableObject database )
        {
            if ( !database ) throw new ArgumentNullException( nameof(database) );

            var timer = new System.Diagnostics.Stopwatch();

            var serializer = new DBScriptableObjectSerializer();
            var allObjects = new List<GdObjectInfo>();
            var rootFolder = serializer.Deserialize( database, allObjects );

            _db = new GdDb( rootFolder, allObjects, database.Hash );

            timer.Stop();
            Debug.Log( $"[{nameof(GdScriptableObjectLoader)}] Loaded gddb {_db.Name} from Scriptable Object {database.name}, objects {_db.AllObjects.Count}, folders {_db.RootFolder.EnumerateFoldersDFS(  ).Count()} for {timer.ElapsedMilliseconds} ms" );
        } 
    }
}
