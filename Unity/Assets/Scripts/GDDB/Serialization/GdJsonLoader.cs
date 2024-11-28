using System;
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
            var serializer      = new DBJsonSerializer();
            var assetsResolver = referencedAssets ?? NullGdAssetResolver.Instance;
            var data            = serializer.Deserialize( jsonStr, assetsResolver );
            _db = new GdDb( data.rootFolder, data.objects );

            Debug.Log( $"[{nameof(GdJsonLoader)}]-[{nameof(GdJsonLoader)}] Loaded GDDB ({_db.AllObjects.Count} objects) from json string length {jsonStr.Length}, Unity assets referenced {assetsResolver.Count}" );
        }
    }
}
