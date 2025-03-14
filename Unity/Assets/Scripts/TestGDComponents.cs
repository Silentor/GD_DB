using System;
using GDDB;
using UnityEngine;

namespace GDDB_User
{
    public class Primitives : GDComponent
    {
        public Int32   IntValue    = 1;
        public Int64   BigIntValue = Int64.MaxValue;
        public Single  FloatValue  = -1.0f;
        public Double  DoubleValue = Double.MaxValue;
        public Single  NANValue    = Single.NaN;
        public String  StrValue    = "Test \"string\"";
        public Boolean BoolValue   = true;
        public Char CharValue      = 'A';
        public Decimal DecimalValue = Decimal.MaxValue;
    }

    [Serializable]
    public class Classes : GDComponent
    {
        public TestEmbeddedClassParent ParentClassPolymord;
        public TestEmbeddedClassChild  ChildClass;

        public TestStruct                Struct1;
        public NonSerializableTestStruct NonSerializableStruct;
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

    [Serializable]
    public struct TestStruct
    {
        public Int32 IntValue;

        public TestEmbeddedClassParent EmbeddedClass;
        public TestEmbeddedClassParent EmbeddedClassPolymorf;
    }

    public struct NonSerializableTestStruct
    {
        public Int32 IntValue;
    }

    public class RequiredGDOComponent : GDComponent
    {

    }

    public class RequiredGDCComponent : GDComponent
    {

    }

    public class DeniedGDCComponent : GDComponent
    {

    }

    public class TestMobComponent : GDComponent
    {
        public String Name;
    }

}
