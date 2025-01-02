using System;
using System.Collections;
using System.Collections.Generic;
using GDDB;
using UnityEngine;

namespace GDDB.Tests
{
   
    public class NotSerializableContentComponent : GDComponent
    {
        public readonly Int32     ReadOnlyField = 42;
        public static   Int32     StaticField   = 42;
        private         Int32     PrivateField  = -42;
        public const    Int32     ConstField    = -42;
        [NonSerialized]
        public          Int32       NotSerializedPublicField   = -42;
        [SerializeField]
        public          SomeClass   ClassWithoutSerializableAttributeField;
        public          Decimal     DecimalField = Decimal.MaxValue;
        public          IntPtr      IntPtrField = new IntPtr(42);
        public          DateTime    DateTimeField = DateTime.Now;

        public class SomeClass
        {
            public Int32 Field = 42;
        }
    }

    public class PrimitivesComponent : GDComponent
    { 
        public  SByte     SByteField   = SByte.MinValue; 
         public Int32     IntField     = Int32.MaxValue;
         public Int64     BigIntField  = Int64.MinValue;
         public UInt64    BigUIntField = UInt64.MaxValue;
         public Single    FloatField   = Single.PositiveInfinity;
         public Double    DoubleField  = Double.NaN;
         public Boolean   BoolField;
         public String    StringField   = "some text";
         public EByteFlagEnum ByteEnumField = EByteFlagEnum.Zero | EByteFlagEnum.One;
         public EIntEnum  IntEnumField  = EIntEnum.Last;
         public Char      CharField     = 'a';

         [Flags]
         public enum EByteFlagEnum : Byte
         {
             Zero = 1 << 0,
             One  = 1 << 1,
             Last = 1 << 7
         }

         public enum EIntEnum : UInt32
         {
             Zero,
             One,
             Last = UInt32.MaxValue, 
         }
    }

    public class NullReferencesAsEmptyComponent : GDComponent
    {
        public String StringMustBeEmpty = null;
        public NestedClass NestedClassMustBeEmpty = null;
        public NestedClass2 NestedClassMustBeEmptyWithoutConstructor = null;
        public NonSerializableClass NonSerializableClassMustStillBeNull = null;

        [Serializable]
        public class NestedClass
        {
            public Int32  IntParam          = 42;
            public String StringMustBeEmpty = null;
        }

        [Serializable]
        public class NestedClass2
        {
            public String StringMustBeEmpty = null;
            public Int32 IntParam = 42;
            public NestedClass NestedClassMustBeEmpty = null;

            private NestedClass2( )
            {
                IntParam = 99;
            }
        }

        public class NonSerializableClass
        {
            public String NullString = null;
        }

    }

    public  class CollectionTestComponent : GDComponent
    {
        public Array                            OldIntArray = new Int32[] { 1, 2, 3 };
        public Int32[]                          IntArray    = new Int32[] { 1, 2, 3 };
        public String[]                          StrArray    = new String[] { null, "", "3" };
        public List<NestedSerializableClass>    ClassList1  = new()  { new (){IntField  = 66}, new (){IntField = 99}, null };
        public List<NestedSerializableClass>    ClassListPolymorf2  = new()  { new (){IntField  = 66}, new (){IntField = 99}, new NestedSerializableChildClass(){IntField2 = 100} };
        public List<NestedNonSerializableClass> ClassListNonSerializable  = new() { new (){IntField = 66}, new (){IntField = 99}, null };

        [Serializable]
        public class NestedSerializableClass
        {
            public Int32 IntField = 42;
        }

        [Serializable]
        public class NestedSerializableChildClass : NestedSerializableClass
        {
            public Int32 IntField2 = 42;
        }

        public class NestedNonSerializableClass
        {
            public Int32 IntField = 24;
        }

    }

    public class GDObjectReferenceComponent : GDComponent
    {
        public GDObject ReferencedObject;
    }

    public class SerializationCallbackComponent : GDComponent, ISerializationCallbackReceiver
    {
        public String NonSerialized { get; set; }
        public String Serialized;

        public void OnBeforeSerialize( )
        {
            Serialized = NonSerialized;
        }

        public void OnAfterDeserialize( )
        {
            NonSerialized = Serialized;
        }
    }

    public class UnitySimpleTypesComponent : GDComponent
    {
        public Vector3        Vector3    = Vector3.one * 666;
        public Vector2        Vector2    = Vector2.one * 66;
        public Vector3Int     Vector3Int = Vector3Int.one * 66;
        public Vector2Int     Vector2Int = Vector2Int.one * 666;
        public Bounds         Bounds     = new Bounds( UnityEngine.Vector3.forward, Vector3.one );
        public Rect           Rect       = new Rect( UnityEngine.Vector2.up, Vector2.one );
        public Quaternion     Quaternion = Quaternion.Euler( 1, 2, 3 );
        public Color          Color      = Color.red;
        public Color32        Color32    = new ( 100, 100, 100, 100 );
        public AnimationCurve AnimCurve  = new (new Keyframe( 0, 0 ), new Keyframe( 0.5f, 1 ), new Keyframe( 1, 0 ));
    }

    public class UnityAssetReferenceComponent : GDComponent
    {
        public Texture2D  Texture2D;
        public Material   Material;
        public GameObject GameObject;
        public Component  Component;
    }
}

public class ComponentWithoutNamespace : GDComponent
{
    public Int32 Field = 42;
}

