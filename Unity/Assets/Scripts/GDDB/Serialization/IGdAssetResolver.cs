using System;
using Object = UnityEngine.Object;

namespace GDDB.Serialization
{
    /// <summary>
    /// Responsible for resolving Unity project assets referenced from GDObjects
    /// </summary>
    public interface IGdAssetResolver
    {
        /// <summary>
        /// Assets count that this resolver supports. May be 0 if not applicable
        /// </summary>
        public Int32 Count { get; }

        /// <summary>
        /// Add asset during GDDB serialization
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="guid"></param>
        /// <param name="localId"></param>
        void AddAsset(  Object asset, String guid, long localId );

        /// <summary>
        /// Get asset during GDDB deserialization todo consider string name support for Addressables
        /// </summary>
        /// <param name="id"></param>
        /// <param name="localId"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        Boolean TryGetAsset(   String id, Int64 localId, out Object asset );
    }

    public class NullGdAssetResolver : IGdAssetResolver
    {
        public Int32 Count => 0;

        public void AddAsset(Object asset, String guid, long localId )
        {
        }

        public Boolean TryGetAsset(   String id, Int64 localId, out Object asset )
        {
            asset = null;
            return true;
        }

        public static readonly NullGdAssetResolver Instance = new ();
    }
}