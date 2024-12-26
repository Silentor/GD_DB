using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB loader from data stream or buffer
    /// </summary>
    public class GdFileLoader : GdLoader
    {
        /// <summary>
        /// Load binary GDDB from stream
        /// </summary>
        /// <param name="binaryStream"></param>
        /// <param name="referencedAssets"></param>
        public GdFileLoader( Stream binaryStream, IGdAssetResolver referencedAssets = null )
        {
            var serializer     = new DBDataSerializer();
            var assetsResolver = referencedAssets ?? NullGdAssetResolver.Instance;
            var reader         = new BinaryReader( binaryStream );
            var data           = serializer.Deserialize( reader, assetsResolver );
            _db = new GdDb( data.rootFolder, data.objects );

            Debug.Log( $"[{nameof(GdFileLoader)}]-[{nameof(GdFileLoader)}] Loaded GDDB ({_db.AllObjects.Count} objects) from binary stream length {binaryStream.Length}, Unity assets referenced {assetsResolver.Count}" );
        }

        /// <summary>
        /// Load JSON from string buffer
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <param name="referencedAssets"></param>
        public GdFileLoader( String jsonStr, IGdAssetResolver referencedAssets = null )
        {
            var       serializer     = new DBDataSerializer();
            var       assetsResolver = referencedAssets ?? NullGdAssetResolver.Instance;
            using var strReader      = new StringReader( jsonStr );
            using var jsonTextReader = new JsonTextReader( strReader );
            var       reader         = new JsonNetReader( jsonTextReader );
            var       data           = serializer.Deserialize( reader, assetsResolver );
            _db = new GdDb( data.rootFolder, data.objects );

            Debug.Log( $"[{nameof(GdFileLoader)}]-[{nameof(GdFileLoader)}] Loaded GDDB ({_db.AllObjects.Count} objects) from json string length {jsonStr.Length}, Unity assets referenced {assetsResolver.Count}" );
        }
    }
}
