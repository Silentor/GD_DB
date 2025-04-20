using System;
using UnityEngine;

namespace Gddb.Tests
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

    public class TestObjectAwakeEnable : GDObject
    {
        public Boolean IsAwaked  { get; private set; }
        public Boolean IsEnabled { get; private set; }

        private void Awake( )
        {
            IsAwaked = true;
        }

        private void OnEnable( )
        {
            IsEnabled = true;
        }
    }

    public class TestSO : ScriptableObject
    {
        public Int32            Value;

        public GDObject         GDObjectReference;
        public TestSO2          SOObjectReference;
        public TestSO           SelfReference;
    }

    public class TestSO2 : ScriptableObject
    {
        public String Value;
        public TestSO CircularReference;

    }

}