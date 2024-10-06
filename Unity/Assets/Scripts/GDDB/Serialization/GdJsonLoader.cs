using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB loader from JSON file in Resources folder or from any TextReader
    /// </summary>
    public class GdJsonLoader : GdLoader
    {
        public GdJsonLoader( String jsonStr, IGdAssetResolver referencedAssets = null )
        {
            var serializer = new DBJsonSerializer();
            var data       = serializer.Deserialize( jsonStr, referencedAssets ?? NullGdAssetResolver.Instance );
            _db = new GdDb( data.rootFolder, data.objects );

            Debug.Log( $"[{nameof(GdJsonLoader)}]-[{nameof(GdJsonLoader)}] Loaded GDDB from json string" );
        }
    }
}
