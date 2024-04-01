using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GDDB.Tests
{
    public class SerializationTests
    {
        [Test]
        public void NotSerializableComponentTest( )
        {
            //Arrange
            var nonSerComp = new NotSerializableContentComponent();
            var testObj    = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( nonSerComp );

            //Act
            var serializer = new GDJson();
            var jsonString = serializer.GDToJson( new GDObject[] { testObj } );

            Debug.Log( jsonString );

            //Assert
            var jObjects    = (JArray)JToken.Parse( jsonString );
            var jRoot       = (JObject)jObjects[ 0 ];
            var jComponents = (JArray)jRoot[ "Components" ];
            var jsonSerComp = (JObject)jComponents[ 0 ][ ".Value" ];

            Assert.That( jsonSerComp.Children(), Is.Empty );
        }

        [TestCase( false, 0D,              0F,              0, 0,              0, "", 'a',
                PrimitivesComponent.EByteEnum.Zero, PrimitivesComponent.EIntEnum.Zero )]
        [TestCase( true,  Double.MinValue, Single.MinValue, SByte.MinValue, Int32.MinValue, Int64.MinValue, "",
                'Я',
                PrimitivesComponent.EByteEnum.One,
                PrimitivesComponent.EIntEnum.One )]
        [TestCase( true, Double.MaxValue, Single.MaxValue, SByte.MaxValue, Int32.MaxValue, Int64.MaxValue,
                "some ansi text", '\uD846', PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NegativeInfinity, Single.NegativeInfinity, 0, 0, 0, "кирилиця", Char.MinValue,
                PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.PositiveInfinity, Single.PositiveInfinity, 0, 0, 0,
                "some chinese \uD846",
                Char.MaxValue, PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NaN, Single.NaN, 0, 0, 0, "some smile \uD83D", '!',
                PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        public void PrimitivesComponentTest( Boolean boolParam, Double  doubleParam, Single floatParam,
                                             SByte sbyteParam, Int32 intParam, Int64 bigIntParam,
                                             String  strParam,  Char charParam,
                                             PrimitivesComponent.EByteEnum byteEnumParam,
                                             PrimitivesComponent.EIntEnum intEnumParam  )
        {
            //Arrange
            var comp = new PrimitivesComponent()
                       {
                               BoolField     = boolParam,
                               DoubleField   = doubleParam,
                               FloatField    = floatParam,
                               IntField      = intParam,
                               BigIntField   = bigIntParam,
                               StringField   = strParam,
                               ByteEnumField = byteEnumParam,
                               IntEnumField  = intEnumParam,
                               CharField     = charParam
                       };
            var testObj = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( comp );

            //Act
            var serializer = new GDJson();
            var jsonString = serializer.GDToJson( new GDObject[] { testObj } );

            Debug.Log( jsonString );

            //Assert
            var comp_copy = serializer.JsonToGD( jsonString ).First().Components.First() as PrimitivesComponent;
            comp_copy.Should().BeEquivalentTo( comp );
        }

        [Test]
        public void NullIsEmptyTest( )
        {
            //Arrange
            var nullComp = new NullReferencesAsEmptyComponent()
                           {
                                   StringMustBeEmpty      = null,
                                   NestedClassMustBeEmpty = null
                           };
            var testObj = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( nullComp );

            //Act
            var serializer = new GDJson();
            var jsonString = serializer.GDToJson( new GDObject[] { testObj } );

            Debug.Log( jsonString );

            //Assert
            var copy =
                    serializer.JsonToGD( jsonString ).First().Components.First() as
                            NullReferencesAsEmptyComponent;
            copy.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmpty.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmpty.IntParam.Should()
                .Be( 42, "default value from field init" );
            copy.NestedClassMustBeEmptyWithoutConstructor.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmptyWithoutConstructor.IntParam.Should()
                .Be( 99, "value from private constructor" );
            copy.NestedClassMustBeEmptyWithoutConstructor.NestedClassMustBeEmpty.Should()
                .BeEquivalentTo( copy.NestedClassMustBeEmpty );
            copy.NonSerializableClassMustStillBeNull.Should().BeNull();
        }

        [Test]
        public void CollectionsTest( )
        {
            //Arrange
            var collComp = new CollectionTestComponent()
                           {
                                   OldIntArray = new Single[] { 1, 2, 3 },     //Mutate
                                   IntArray    = new Int32[] { 3, 2, 1 }         //Mutate
                           };
            collComp.ClassListPolymorf2[ 2 ] =
                    new CollectionTestComponent.NestedSerializableChildClass()   //Mutate
                    {
                            IntField  = 1,
                            IntField2 = 2
                    };
            var testObj = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( collComp );

            //Act
            var serializer = new GDJson();
            var jsonString = serializer.GDToJson( new GDObject[] { testObj } );

            Debug.Log( jsonString );

            //Assert json
            var jObjects    = (JArray)JToken.Parse( jsonString );
            var jRoot       = (JObject)jObjects[ 0 ];
            var jComponents = (JArray)jRoot[ "Components" ];
            var jsonSerComp = (JObject)jComponents[ 0 ][ ".Value" ];

            Assert.IsNull( jsonSerComp[ nameof(CollectionTestComponent.OldIntArray) ] );
            Assert.IsNull( jsonSerComp[ nameof(CollectionTestComponent.ClassListNonSerializable) ] );

            //Asset deserialized data
            var copy =
                    serializer.JsonToGD( jsonString ).First().Components.First() as
                            CollectionTestComponent;
            copy.OldIntArray.Should().BeEquivalentTo( new CollectionTestComponent().OldIntArray );
            copy.IntArray.Should().BeEquivalentTo( collComp.IntArray );
            copy.StrArray.Should().BeEquivalentTo( new String[] { String.Empty, String.Empty, "3" } );
            copy.ClassList1.Should().BeEquivalentTo(
                    new System.Collections.Generic.List<
                            CollectionTestComponent.NestedSerializableClass>()
                    {
                            collComp.ClassList1[ 0 ],
                            collComp.ClassList1[ 1 ],
                            new ()
                    }, "null elements deserialized as empty objects" );
            copy.ClassListPolymorf2.Should().BeEquivalentTo(
                    new System.Collections.Generic.List<
                            CollectionTestComponent.NestedSerializableClass>()
                    {
                            collComp.ClassListPolymorf2[ 0 ],
                            collComp.ClassListPolymorf2[ 1 ],
                            new () { IntField = 1 }
                    } , "polymorphism is lost" );
            copy.ClassListNonSerializable.Should().BeEquivalentTo( collComp.ClassListNonSerializable );
        }

        [Test]
        public void RuntimeGDO( )
        {
             //Arrange
             var obj = GDObject.CreateInstance<GDObject>();

             //Act
             var serializer = new GDJson();
             var jsonString = serializer.GDToJson( new GDObject[] { obj } );
             Debug.Log( jsonString );
             var copyObj = serializer.JsonToGD( jsonString )[0];

             //Assert
             obj.Guid.Should().NotBe( default(Guid) );
             copyObj.Guid.Should().Be( obj.Guid );
        }

        [Test]
        public void GDObjectReferenceTest( )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<TestObjectWithReference>();
                var obj2 = GDObject.CreateInstance<TestObjectWithReference>();
                var obj3 = GDObject.CreateInstance<GDObject>();

                obj1.ObjReference = obj3;
                var refComp = new GDObjectReferenceComponent         { ReferencedObject = obj3 };
                obj1.Components.Add( refComp );
                obj2.ObjReference = obj3;

                //Act
                var serializer = new GDJson();
                var jsonString = serializer.GDToJson( new GDObject[] { obj1, obj2, obj3 } );
                Debug.Log( jsonString );
                var copyObjects = serializer.JsonToGD( jsonString );

                //Assert
                var obj1_copy   = (TestObjectWithReference)copyObjects[ 0 ];
                var obj2_copy   = (TestObjectWithReference)copyObjects[ 1 ];
                var obj3_copy   = copyObjects[ 2 ];
                obj1_copy.ObjReference.Should().NotBeNull(  );
                obj1_copy.ObjReference.Should().BeSameAs( obj2_copy.ObjReference );
                obj1_copy.ObjReference.Should().BeSameAs( obj3_copy );
                obj1_copy.GetComponent<GDObjectReferenceComponent>().ReferencedObject.Should().BeSameAs( obj3_copy );
        }

        [Test]
        public void ISerializationCallbackReceiverTest( )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<TestObjectSerializationCallback>();
                obj1.NonSerialized = "some text";
                var comp = new  SerializationCallbackComponent        { NonSerialized = "some other text"};
                obj1.Components.Add( comp );

                //Act
                var serializer = new GDJson();
                var jsonString = serializer.GDToJson( new GDObject[] { obj1 } );
                Debug.Log( jsonString );
                var copyObjects = serializer.JsonToGD( jsonString );

                //Assert
                var obj1_copy = (TestObjectSerializationCallback)copyObjects[ 0 ];
                obj1_copy.NonSerialized.Should().Be( obj1.NonSerialized );
                obj1_copy.GetComponent<SerializationCallbackComponent>().NonSerialized.Should().Be( comp.NonSerialized );
        }

        [Test]
        public void AssetReferenceTest( )
        {
                throw new NotImplementedException();
        }

        [Test]
        public void UnityTypesSupportTest( )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<GDObject>();
                var comp = new  UnitySimpleTypesComponent
                           {
                                           Vector3    = -Vector3.one,
                                           Quaternion = Quaternion.Euler( 100, 200, 300 ),
                                           Color      = Color.green,
                                           Color32    = new Color32( 200, 200, 200, 200 ),
                                           AnimCurve  = new (new Keyframe( 0, 1 ), new Keyframe( 0.5f, 0 ), new Keyframe( 1, 1 )),
                                           Bounds = new Bounds( Vector3.back, Vector3.down ),
                                           Rect = new Rect( 4, 3,2,1 ),
                                           Vector2 = -Vector2.one,
                                           Vector2Int = Vector2Int.up,
                                           Vector3Int = Vector3Int.up,
                           };
                obj1.Components.Add( comp );

                //Act
                var serializer = new GDJson();
                var jsonString = serializer.GDToJson( new GDObject[] { obj1 } );
                Debug.Log( jsonString );
                var copyObjects = serializer.JsonToGD( jsonString );

                //Assert
                var obj1_copy = (GDObject)copyObjects[ 0 ];
                obj1_copy.GetComponent<UnitySimpleTypesComponent>().Should().BeEquivalentTo( comp );
        }

        [Test]
        public void DisabledGDObjectTest( )
        {
                //Arrange
                var root = GDObject.CreateInstance<GDRoot>();
                root.Id = "TestRoot";
                var enabledObj = GDObject.CreateInstance<GDObject>();
                var disabledObj = GDObject.CreateInstance<GDObject>();
                disabledObj.Enabled = false;

                //Act
                var serializer = new GDJson();
                var jsonString = serializer.GDToJson( new GDObject[] { root, enabledObj, disabledObj } );
                Debug.Log( jsonString );
                var gddb = new GdJsonLoader( new StringReader( jsonString ) );

                //Assert
                gddb.AllObjects.Count( gdo => !gdo.Enabled ).Should().Be( 1 );
        }
    }
}