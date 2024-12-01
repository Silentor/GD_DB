using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using GDDB.Serialization;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
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
            var serializer = new ObjectsJsonSerializer();
            var jsonString = serializer.Serialize( testObj ).ToString();

            Debug.Log( jsonString );

            //Assert
            var jObject     = (JSONObject)JSONNode.Parse( jsonString );
            var jComponents = (JSONArray)jObject[ ".Components" ];
            var jsonSerComp = (JSONObject)jComponents[ 0 ][ ".Value" ];

            Assert.That( jsonSerComp.Children, Is.Empty );
        }

        [TestCase( false, 0D, 0F,  0, 0, 0L, 0UL, "", 'a',
                PrimitivesComponent.EByteFlagEnum.Zero, PrimitivesComponent.EIntEnum.Zero )]
        [TestCase( true,  Double.MinValue, Single.MinValue, SByte.MinValue, Int32.MinValue, Int64.MinValue, UInt64.MinValue, 
                        "", 
                'Я',
                PrimitivesComponent.EByteFlagEnum.One,
                PrimitivesComponent.EIntEnum.One )]
        [TestCase(                true,     Double.MaxValue, Single.MaxValue, SByte.MaxValue, Int32.MaxValue, Int64.MaxValue, UInt64.MaxValue,
                "some ansi text", '\uD846', PrimitivesComponent.EByteFlagEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NegativeInfinity, Single.NegativeInfinity, 0, 0, 0, UInt64.MaxValue - 1, "кирилиця", Char.MinValue,
                PrimitivesComponent.EByteFlagEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.PositiveInfinity, Single.PositiveInfinity, 0, 0, Int64.MinValue + 1, 0UL,
                "some chinese \uD846",
                Char.MaxValue, PrimitivesComponent.EByteFlagEnum.Zero | PrimitivesComponent.EByteFlagEnum.One,
                PrimitivesComponent.EIntEnum.Last )]
        [TestCase( true, Double.NaN, Single.NaN, 0, 0, 0, UInt64.MaxValue - 2, "some smile \uD83D", '!',
                PrimitivesComponent.EByteFlagEnum.Last,
                PrimitivesComponent.EIntEnum.Last )]
        public void PrimitivesComponentTest( Boolean                       boolParam,  Double doubleParam, Single floatParam,
                                             SByte                         sbyteParam, Int32  intParam,    Int64  bigIntParam, UInt64 bigUIntParam,
                                             String                        strParam,   Char   charParam,
                                             PrimitivesComponent.EByteFlagEnum byteEnumParam,
                                             PrimitivesComponent.EIntEnum  intEnumParam  )
        {
            //Arrange
            var comp = new PrimitivesComponent()
                       {
                               BoolField     = boolParam,
                               DoubleField   = doubleParam,
                               FloatField    = floatParam,
                               IntField      = intParam,
                               BigIntField   = bigIntParam,
                               BigUIntField   = bigUIntParam,
                               StringField   = strParam,
                               ByteEnumField = byteEnumParam,
                               IntEnumField  = intEnumParam,
                               CharField     = charParam
                       };
            var testObj = ScriptableObject.CreateInstance<GDRoot>();
            testObj.Components.Add( comp );

            //Act
            var serializer = new ObjectsJsonSerializer();
            var jsonString = serializer.Serialize( testObj ).ToString( 2 );

            Debug.Log( jsonString );

            //Assert
            var comp_copy = serializer.Deserialize( jsonString ).Components.First() as PrimitivesComponent;
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
            var serializer = new ObjectsJsonSerializer();
            var jsonString = serializer.Serialize( testObj );

            Debug.Log( jsonString );

            //Assert
            var copy = serializer.Deserialize( jsonString ).Components.First() as NullReferencesAsEmptyComponent;
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
            var serializer = new ObjectsJsonSerializer();
            var json = serializer.Serialize( testObj );
            var jsonString = json.ToString();

            Debug.Log( jsonString );

            //Assert json
            var jObject     = (JSONObject)JSONNode.Parse( jsonString );
            var jComponents = (JSONArray)jObject[ ".Components" ];
            var jsonSerComp = (JSONObject)jComponents[ 0 ][ ".Value" ];

            //Should not be serialized
            jsonSerComp.HasKey( nameof(CollectionTestComponent.OldIntArray) ).Should().BeFalse(  );
            jsonSerComp.HasKey( nameof(CollectionTestComponent.ClassListNonSerializable) ).Should().BeFalse(  );

            //Asset deserialized data
            var copy = serializer.Deserialize( json ).Components.First() as CollectionTestComponent;
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
             var serializer = new ObjectsJsonSerializer();
             var jsonString = serializer.Serialize( obj );
             Debug.Log( jsonString );
             var copyObj = serializer.Deserialize( jsonString );

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
                var serializer  = new ObjectsJsonSerializer();
                var jsonString1 = serializer.Serialize( obj1 ).ToString(2);
                var jsonString2 = serializer.Serialize( obj2 ).ToString(2);
                var jsonString3 = serializer.Serialize( obj3 ).ToString(2);
                Debug.Log( jsonString1 );
                Debug.Log( jsonString2 );
                Debug.Log( jsonString3 );
                var copyObject1 = serializer.Deserialize( jsonString1 );
                var copyObject2 = serializer.Deserialize( jsonString2 );
                var copyObject3 = serializer.Deserialize( jsonString3 );
                serializer.ResolveGDObjectReferences();

                //Assert
                var obj1_copy   = (TestObjectWithReference)copyObject1;
                var obj2_copy   = (TestObjectWithReference)copyObject2;
                var obj3_copy   = copyObject3;
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
                var serializer = new ObjectsJsonSerializer();
                var jsonString = serializer.Serialize( obj1 ).ToString(2);
                Debug.Log( jsonString );
                var copyObjects = serializer.Deserialize( jsonString );

                //Assert
                var obj1_copy = (TestObjectSerializationCallback)copyObjects;
                obj1_copy.NonSerialized.Should().Be( obj1.NonSerialized );    //Should restore value from serialized field
                obj1_copy.GetComponent<SerializationCallbackComponent>().NonSerialized.Should().Be( comp.NonSerialized );
        }

        [Test]
        public void UnityTypesSupportTest( )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<GDObject>();
                var comp = new  UnitySimpleTypesComponent
                           {
                                           Vector3    = -Vector3.one * 0.5f,
                                           Quaternion = Quaternion.Euler( 100, 200, 300 ),
                                           Color      = Color.green,
                                           Color32    = new Color32( 200, 200, 200, 200 ),
                                           AnimCurve  = new (new Keyframe( 0, 1 ), new Keyframe( 0.5f, 0 ), new Keyframe( 1, 1 )),
                                           Bounds = new Bounds( Vector3.back, Vector3.down ),
                                           Rect = new Rect( 4.0f, 3, 2.1f, 1 ),
                                           Vector2 = -Vector2.one,
                                           Vector2Int = Vector2Int.up,
                                           Vector3Int = Vector3Int.up,
                           };
                obj1.Components.Add( comp );

                //Act
                var serializer = new ObjectsJsonSerializer();
                var jsonString = serializer.Serialize( obj1 ).ToString( 2 );
                Debug.Log( jsonString );
                var copyObjects = serializer.Deserialize( jsonString );

                //Assert
                var obj1_copy = (GDObject)copyObjects;
                obj1_copy.GetComponent<UnitySimpleTypesComponent>().Should().BeEquivalentTo( comp );
        }

        [Test]
        public void DisabledGDObjectTest( )
        {
                //Arrange
                var disabledObj = GDObject.CreateInstance<GDObject>();
                disabledObj.EnabledObject = false;

                //Act
                var serializer = new ObjectsJsonSerializer();
                var jsonString = serializer.Serialize( disabledObj );

                //Assert
                jsonString.Should().BeNull(  );
        }

        [Test]
        public void AwakeEnableAfterJsonTest( )
        {
                //Arrange
                var obj = GDObject.CreateInstance<TestObjectAwakeEnable>();

                //Act
                var serializer = new ObjectsJsonSerializer();
                var jsonString = serializer.Serialize( obj );
                Debug.Log( jsonString );
                var copyObjects = serializer.Deserialize( jsonString );
                var copy = (TestObjectAwakeEnable)copyObjects;

                //Assert
                copy.IsAwaked.Should().BeTrue();
                copy.IsEnabled.Should().BeTrue();
        }

        [Test]
        public void UnityAssetsSerializationTest_SOAssetResolver( )
        {
                //Arrange
                var obj           = GDObject.CreateInstance<GDObject>();
                var testTexture   = Resources.FindObjectsOfTypeAll<Texture2D>().First( AssetDatabase.Contains );
                var testMat       = Resources.FindObjectsOfTypeAll<Material>().First( AssetDatabase.Contains );
                var testGO        = Resources.Load<GameObject>( "TestPrefab" );
                var testComponent = testGO.GetComponent<MeshFilter>();

                var goComp = new UnityAssetReferenceComponent
                           {
                                   Texture2D  = testTexture,
                                   Material   = testMat,
                                   GameObject = testGO,
                                   Component = testComponent,
                           };
                obj.Components.Add( goComp );
                var testAssetResolver = ScriptableObject.CreateInstance<DirectAssetReferences>();

                //Act
                var serializer = new ObjectsJsonSerializer();
                var jsonString = serializer.Serialize( obj , testAssetResolver );
                Debug.Log( jsonString );
                var copyObjects = serializer.Deserialize( jsonString, testAssetResolver );

                //Assert
                var copyComp = copyObjects.GetComponent<UnityAssetReferenceComponent>();
                copyComp.Texture2D.Should().BeSameAs( testTexture );
                copyComp.Material.Should().BeSameAs( testMat );
                copyComp.GameObject.Should().BeSameAs( testGO );
                copyComp.Component.Should().BeSameAs( testComponent );
        }

        [Test]
        public void FoldersSerializationTest( )
        {
            //Arrange
            var root      = GetFolder( "Root", null  ) ;
            var mobs      = GetFolder( "Mobs", root); 
            var elves     = GetFolder( "Elves", mobs); 
            var locations = GetFolder( "Locations", root);

            var gdRoot         = CreateGDObject<GDRoot>( "Root" ); root.Objects.Add( gdRoot );
            var elf1           = CreateGDObject( "Elf1" ); elves.Objects.Add( elf1 );
            var elf2           = CreateGDObject( "Elf2" ); elves.Objects.Add( elf2 );
            var mobsSettings   = CreateGDObject( "MobsSettings" ); mobs.Objects.Add( mobsSettings );
            var forestLocation = CreateGDObject( "Forest" ); locations.Objects.Add( forestLocation );

            //Act
            var serializer = new DBJsonSerializer();
            var gddbJson = serializer.Serialize( root, root.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() , NullGdAssetResolver.Instance ).ToString();
            var db       = new GdJsonLoader( gddbJson ).GetGameDataBase();

            //Assert
            db.RootFolder.SubFolders.Count.Should().Be( 2 );
            db.RootFolder.Name.Should().Be( "Root" );
            var mobsFolder = db.RootFolder.SubFolders.Single( f => f.Name == "Mobs" );
            mobsFolder.Objects.Count.Should().Be( 1 );
            var elvesFolder = mobsFolder.SubFolders.Single( f => f.Name == "Elves" );
            elvesFolder.Objects.Count.Should().Be( 2 );
            elvesFolder.Objects.Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.SubFolders.Should().BeEmpty();
            elvesFolder.Objects.Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.Objects.Select( gdo => gdo.Name ).Should().BeEquivalentTo( new[] { "Elf1", "Elf2" } );

            
        }

        GDObject CreateGDObject( String name )
        {
                var gdo = GDObject.CreateInstance<GDObject>();
                gdo.name = name;
                return gdo;
        }

        GDObject CreateGDObject<T>( String name ) where T : GDObject
        {
                var gdo = GDObject.CreateInstance<T>();
                gdo.name = name;
                return gdo;
        }

        private Folder GetFolder( String name, Folder parent )
        { 
                if ( parent != null )
                {
                        var result = new Folder( name, Guid.NewGuid(), parent );
                        return result;
                }
                else
                {
                        return new Folder( name, Guid.NewGuid() );
                }
        }
    }
}