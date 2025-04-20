using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gddb.Serialization
{
    /// <summary>
    /// Simple asset resolver - just serializable list of assets
    /// </summary>
    public class DirectAssetReferences : ScriptableObject, IGdAssetResolver
    {
        public List<AssetReference> Assets = new (); 

        public Int32 Count => Assets.Count;

        public void AddAsset( UnityEngine.Object asset, String guid, long localId )
        {
            if ( asset == null ) throw new ArgumentNullException( nameof(asset) );

            var assetItem = Assets.Find( a => a.Asset == asset );
            if ( assetItem.Guid == null )
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

        public Boolean TryGetAsset(  String id, Int64 localId, out Object asset )
        {
            foreach ( var a in Assets )
            {
                if( a.Guid == id && a.LocalId == localId )
                {
                    asset = a.Asset;
                    return true;
                }
            }

            asset = null;
            return false;
        }

        [Serializable]
        public struct AssetReference
        {
            public string             Guid;
            public long               LocalId;
            public UnityEngine.Object Asset;
        }
    }

    
}