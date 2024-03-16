using System;
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

        [TestCase( false, 0D,              0F,              0, 0,              0, "", 'a', PrimitivesComponent.EByteEnum.Zero, PrimitivesComponent.EIntEnum.Zero )]
        [TestCase( true,  Double.MinValue, Single.MinValue, SByte.MinValue, Int32.MinValue, Int64.MinValue, "", 'Я', PrimitivesComponent.EByteEnum.One,
                PrimitivesComponent.EIntEnum.One )]
        [TestCase( true, Double.MaxValue, Single.MaxValue, SByte.MaxValue, Int32.MaxValue, Int64.MaxValue, "some ansi text", '\uD846', PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NegativeInfinity, Single.NegativeInfinity, 0, 0, 0, "кирилиця", Char.MinValue, PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.PositiveInfinity, Single.PositiveInfinity, 0, 0, 0, "some chinese \uD846", Char.MaxValue, PrimitivesComponent.EByteEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NaN, Single.NaN, 0, 0, 0, "some smile \uD83D", '!', PrimitivesComponent.EByteEnum.Last, PrimitivesComponent.EIntEnum.Last )]
        public void PrimitivesComponentTest( Boolean boolParam, Double  doubleParam, Single floatParam, SByte sbyteParam, Int32 intParam, Int64 bigIntParam,
                                             String  strParam,  Char charParam, PrimitivesComponent.EByteEnum byteEnumParam, PrimitivesComponent.EIntEnum intEnumParam  )
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
                               CharField     = charParam,
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
                                   NestedClassMustBeEmpty = null,
                           };
            var testObj = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( nullComp );

            //Act
            var serializer = new GDJson();
            var jsonString = serializer.GDToJson( new GDObject[] { testObj } );

            Debug.Log( jsonString );

            //Assert
            var copy = serializer.JsonToGD( jsonString ).First().Components.First() as NullReferencesAsEmptyComponent;
            copy.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmpty.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmpty.IntParam.Should().Be( 42, because: "default value from field init" );
            copy.NestedClassMustBeEmptyWithoutConstructor.StringMustBeEmpty.Should().BeEmpty(  );
            copy.NestedClassMustBeEmptyWithoutConstructor.IntParam.Should().Be( 99, because: "value from private constructor" );
            copy.NestedClassMustBeEmptyWithoutConstructor.NestedClassMustBeEmpty.Should().BeEquivalentTo( copy.NestedClassMustBeEmpty );
            copy.NonSerializableClassMustStillBeNull.Should().BeNull();
        }

        [Test]
        public void CollectionsTest( )
        {
            //Arrange
            var collComp = new CollectionTestComponent()
                           {
                                   OldIntArray = new Single[] { 1, 2, 3 },     //Mutate
                                   IntArray = new Int32[] { 3, 2, 1 },         //Mutate
                          };
            collComp.ClassListPolymorf2[ 2 ] = new CollectionTestComponent.NestedSerializableChildClass()   //Mutate
                                               {
                                                       IntField  = 1,
                                                       IntField2 = 2,
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

            Assert.IsNull( jsonSerComp[nameof(CollectionTestComponent.OldIntArray)] );
            Assert.IsNull( jsonSerComp[nameof(CollectionTestComponent.ClassListNonSerializable)] );

            //Asset deserialized data
            var copy = serializer.JsonToGD( jsonString ).First().Components.First() as CollectionTestComponent;
            copy.OldIntArray.Should().BeEquivalentTo( new CollectionTestComponent().OldIntArray );
            copy.IntArray.Should().BeEquivalentTo( collComp.IntArray );
            copy.StrArray.Should().BeEquivalentTo( new String[]{String.Empty, String.Empty, "3"} ); 
            copy.ClassList1.Should().BeEquivalentTo( new System.Collections.Generic.List<CollectionTestComponent.NestedSerializableClass>()
                                                     {
                                                             collComp.ClassList1[0],    
                                                             collComp.ClassList1[1],
                                                             new ()
                                                     }, because: "null elements deserialized as empty objects" ); 
            copy.ClassListPolymorf2.Should().BeEquivalentTo( new System.Collections.Generic.List<CollectionTestComponent.NestedSerializableClass>()
                                                             {
                                                                    collComp.ClassListPolymorf2[0],
                                                                    collComp.ClassListPolymorf2[1],
                                                                    new (){ IntField = 1 },                    
                                                             } , because: "polymorphism is lost");
            copy.ClassListNonSerializable.Should().BeEquivalentTo( collComp.ClassListNonSerializable );
        }
    }
}
