using System;
using UnityEngine;

namespace GDDB.Tests
{
    public class TestObjectWithReference : GDObject
    {
        public GDObject ObjReference;
    }

    public class TestObjectSerializationCallback : GDObject, ISerializationCallbackReceiver
    {
        public String NonSerialized { get; set; }
        public String Serialized;

        public void     OnBeforeSerialize( )
        {
            Serialized = NonSerialized;
        }

        public void     OnAfterDeserialize( )
        {
            NonSerialized = Serialized;
        }
    }

}