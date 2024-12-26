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
    public abstract class TypeCustomSerializer<TValue> : TypeCustomSerializer
    {
        public Type SerializedType => typeof( TValue );

        public abstract void Serialize(  WriterBase writer, TValue obj );

        public abstract TValue Deserialize( ReaderBase reader );

        public override void Serialize( WriterBase writer, Object obj )
        {
            Serialize( writer, (TValue) obj );
        }

        public override Object DeserializeBase(ReaderBase reader )
        {
            return Deserialize( reader );
        }
    }

#if UNITY_2021_2_OR_NEWER
    [RequireDerived]
#endif
    public abstract class TypeCustomSerializer
    {
        public abstract void Serialize( WriterBase writer, Object obj );

        public abstract Object DeserializeBase( ReaderBase reader );
    }
}

