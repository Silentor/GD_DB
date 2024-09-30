using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// Store project assets for GdJson format
    /// </summary>
    public class GdAssetReference : ScriptableObject
    {
        public List<AssetReference> Assets = new (); 

        public void AddAsset( [NotNull] UnityEngine.Object asset, String guid, long localId )
        {
            if ( asset == null ) throw new ArgumentNullException( nameof(asset) );

            var assetItem = Assets.Find( a => a.Asset == asset );
            if ( assetItem == null )
            {
                var newItem = new AssetReference()
                              {
                                      Guid    = guid,
                                      LocalId = localId,
                                      Asset   = asset,
                              };
                Assets.Add( newItem );
            }
        }

        [Serializable]
        public class AssetReference
        {
            public string             Guid;
            public long               LocalId;
            public UnityEngine.Object Asset;
        }
    }
}