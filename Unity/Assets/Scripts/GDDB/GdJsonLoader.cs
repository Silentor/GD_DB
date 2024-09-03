using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// GD DB loader from JSON file in Resources folder or from any TextReader
    /// </summary>
    public class GdJsonLoader : GdLoader
    {
        public GdJsonLoader( String name )
        {
            var structureJsonPath = $"{name}.structure";
            var structureJson = Resources.Load<TextAsset>( structureJsonPath );
            if( !structureJson )
                throw new ArgumentException( $"GdDB name {name} is incorrect, structure file {structureJsonPath} not found " );

            var objectsJsonPath = $"{name}.objects";
            var objectsJson = Resources.Load<TextAsset>( objectsJsonPath );
            if( !objectsJson )
                throw new ArgumentException( $"GdDB name {name} is incorrect, objects file {objectsJsonPath} not found " );

            var assetsPath = $"{name}.assets";
            var referencedAssets  = Resources.Load<GdAssetReference>( assetsPath );
            if( !referencedAssets )
                throw new ArgumentException( $"GdDB name {name} is incorrect, assets file {assetsPath} not found " );

            _db = LoadGdDb( structureJson.text, objectsJson.text, referencedAssets );
        }
        
        public GdJsonLoader( String structureJson, String objectsJson, GdAssetReference referencedAssets = null )
        {
            _db = LoadGdDb( structureJson, objectsJson, referencedAssets );
        }

        private GdDb LoadGdDb( String structureJson, String objectsJson, GdAssetReference referencedAssets )
        {
            var folderSerializer = new FoldersSerializer();
            var rootFolder       = folderSerializer.Deserialize( structureJson );
            var gdJsonSerializer = new GDJson();
            var objects          = gdJsonSerializer.JsonToGD( objectsJson, referencedAssets );

            foreach ( var folder in rootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdo in folder.Objects )
                {
                    gdo.Asset = objects.FirstOrDefault( o => o.Guid == gdo.AssetGuid );
                } 
            }

            return new GdDb( rootFolder, objects );
        }
    }
}
