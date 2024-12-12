using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Object = System.Object;

#if UNITY_2021_2_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace GDDB.Serialization
{
#if UNITY_2021_2_OR_NEWER
    [RequireDerived]
#endif
    public abstract class TypeCustomSerializer
    {
        public abstract Type SerializedType { get; }

        public abstract JToken Serialize(  Object obj );

        public abstract Object Deserialize( JsonReader json );
    }
}

