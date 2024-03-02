using System;
using UnityEngine;

namespace GDDB
{
    public class GDComponentChild1 : GDComponent
    {
        public Int32                   IntValue;
        public TestEmbeddedClassParent ClassValue1;
        public TestEmbeddedClassChild  ClassValue2;
    }

    public class GDComponentChild2 : GDComponent
    {
        public String  StrValue;
        public Vector3 Vector3Value = new Vector3( 1, 2, 3.5f );
    }

    public class GDComponentChild3 : GDComponent
    {
        public Texture TexValue;
    }

    [Serializable]
    public class TestEmbeddedClassParent
    {
        public Int32 IntValue2;
    }

    [Serializable]
    public class TestEmbeddedClassChild  : TestEmbeddedClassParent
    {
        public Int32 IntValue3;
    }

}
