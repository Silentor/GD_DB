using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Gddb.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestRunner;
using Object = System.Object;

//[assembly:TestRunCallback(typeof(GDDB.Tests.SerializationTests))]

namespace Gddb.Tests
{

    public class SerializationTests : BaseSerializationTests, ITestRunCallback                               
    {
        [Test]
        public void NotSerializableComponentTest( [Values]EBackend backend )
        {
            var buffer = GetBuffer( backend );
            var writer = GetWriter( backend, buffer );

            //Arrange
            var nonSerComp = new NotSerializableContentComponent();
            var testObj    = ScriptableObject.CreateInstance<GDObject>();
            testObj.Components.Add( nonSerComp );

            //Act
            var serializer = new GDObjectSerializer( writer );
            serializer.Serialize( testObj );
            LogBuffer( buffer );

            //Assert
            var reader = GetReader( backend, buffer );
            reader.SeekPropertyName( ".components" );                   //Seek to components array           
            reader.SeekPropertyName( ".type" );                         //Seek to component type                                 

            //Make sure there are no serialized user properties in NotSerializableContentComponent
            while ( reader.ReadNextToken() != EToken.EoF )
            {
                 reader.CurrentToken.Should().NotBe( EToken.PropertyName, "No properties should be serialized" );   
            }
        }

        [TestCaseSource(nameof(DataSourceForPrimitiveComponentTest))]
        public void PrimitivesComponentTest( EBackend backend, 
                                             Boolean   boolParam,  Double doubleParam, Single floatParam,
                                             SByte   sbyteParam, Int32  intParam,    Int64  bigIntParam, UInt64 bigUIntParam,
                                             String   strParam,   Char   charParam,
                                             PrimitivesComponent.EByteFlagEnum byteEnumParam,
                                             PrimitivesComponent.EIntEnum  intEnumParam  )
        {
            //Arrange
            var comp = new PrimitivesComponent()
                       {
                               SByteField = sbyteParam,
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
            var testObj = ScriptableObject.CreateInstance<GDObject>();
            testObj.Components.Add( comp );

            //Act
            var buffer     = GetBuffer( backend );
            var writer     = GetWriter( backend, buffer );
            var serializer = new GDObjectSerializer(writer);
            serializer.Serialize( testObj );

            LogBuffer( buffer );
            SaveToFile( backend, "test", buffer );

            //Assert
            var reader       = GetReader( backend, buffer );
            var deserializer = new GDObjectDeserializer( reader );
            var comp_copy    = ((GDObject)deserializer.Deserialize( )).Components.First() as PrimitivesComponent;
            comp_copy.Should().BeEquivalentTo( comp );
        }

        [Test]
        public void NullIsEmptyTest( [Values]EBackend backend )
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
            var buffer     = GetBuffer( backend );
            var writer     = GetWriter( backend, buffer );

            var serializer = new GDObjectSerializer( writer );
            serializer.Serialize( testObj );

            LogBuffer( buffer );
            SaveToFile( backend, "test", buffer );

            //Assert
            var reader       = GetReader( backend, buffer );
            var deserializer = new GDObjectDeserializer( reader );
            var copy         = ( (GDObject)deserializer.Deserialize() ).Components.First() as NullReferencesAsEmptyComponent;
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

        // [Test]                         //Its not exposed to user for now
        // public void TestPropertyNameAlias( EBackend backend)
        // {
        //      //Arrange
        //      var gdo = GDObject.CreateInstance<GDObject>();
        //      var comp = new PrimitivesComponent()
        //                 {
        //                                 IntField = 66,
        //                                 FloatField = 42.42f,
        //                 };
        //      gdo.Components.Add( comp );
        //
        //      //Act
        //      var buffer = GetBuffer( backend );
        //      var writer = GetWriter( backend, buffer );
        //      var serializer = new GDObjectSerializer( writer );
        //
        //
        // }

        [Test]
        public void EnumsTest( [Values]EBackend backend )
        {
                //Arrange
                var enumComp = new EnumsComponent()
                               {
                                               DefaultEnum  = DefaultEnum.Third,
                                               //BigEnum      = UInt64Enum.Last,
                                               SignedEnum   = SignedEnum.Second,
                                               Flags                 = Flags.All,
                                               FlagsArray        = new[]{ Flags.First, Flags.Second | Flags.Fourth },
                                              
                               };
                var testObj = ScriptableObject.CreateInstance<GDObject>();
                testObj.Components.Add( enumComp );

                //Act
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );

                var serializer = new GDObjectSerializer( writer );
                serializer.Serialize( testObj );

                LogBuffer( buffer );
                SaveToFile( backend, "test", buffer );

                //Assert
                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copy         = ( (GDObject)deserializer.Deserialize() ).Components.First() as EnumsComponent;
                copy.DefaultEnum.Should().Be( enumComp.DefaultEnum );
                //copy.BigEnum.Should().Be( enumComp.BigEnum );
                copy.SignedEnum.Should().Be( enumComp.SignedEnum );
                copy.Flags.Should().Be( enumComp.Flags );
                copy.FlagsArray.Should().BeEquivalentTo( enumComp.FlagsArray );
        }

        [Test]
        public void GuidsTest( [Values]EBackend backend )
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
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );

                var serializer = new GDObjectSerializer( writer );
                serializer.Serialize( testObj );

                LogBuffer( buffer );
                SaveToFile( backend, "test", buffer );

                //Assert
                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copy         = ( (GDObject)deserializer.Deserialize() ).Components.First() as NullReferencesAsEmptyComponent;
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
        public void CollectionsTest( [Values]EBackend backend )
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
            var testObj = GDObject.CreateInstance<GDObject>();
            testObj.Components.Add( collComp );

            //Act
            var buffer     = GetBuffer( backend );
            var writer     = GetWriter( backend, buffer );
            
            var serializer = new GDObjectSerializer( writer );
            serializer.Serialize( testObj );
            LogBuffer( buffer );
            SaveToFile( backend, "test", buffer );

            //Assert

            //Some properties should not be serialized
            var reader1 = GetReader( backend, buffer );         //Because reader is forward-only we should create new instances
            EnumerateTokens( reader1 ).Should().NotContainEquivalentOf( (EToken.PropertyName, nameof(CollectionTestComponent.OldIntArray) ) );
            var reader2 = GetReader( backend, buffer );
            EnumerateTokens( reader2 ).Should().NotContainEquivalentOf( (EToken.PropertyName, nameof(CollectionTestComponent.ClassListNonSerializable) ) );

            //Asset deserialized data
            var reader       = GetReader( backend, buffer );
            var deserializer = new GDObjectDeserializer( reader );
            var copy         = ( (GDObject)deserializer.Deserialize() ).Components.First() as CollectionTestComponent;
            copy.OldIntArray.Should().BeEquivalentTo( new CollectionTestComponent().OldIntArray );
            copy.IntArray.Should().BeEquivalentTo( collComp.IntArray );
            copy.StrArray.Should().BeEquivalentTo( new String[] { String.Empty, String.Empty, "3" } );
            copy.ClassList1.Should().BeEquivalentTo(
                    new List<CollectionTestComponent.NestedSerializableClass>
                    {
                            collComp.ClassList1[ 0 ],
                            collComp.ClassList1[ 1 ],
                            new ()
                    }, "null elements deserialized as empty objects" );
            copy.ClassListPolymorf2.Should().BeEquivalentTo(
                    new List<CollectionTestComponent.NestedSerializableClass>
                    {
                            collComp.ClassListPolymorf2[ 0 ],
                            collComp.ClassListPolymorf2[ 1 ],
                            new () { IntField = 1 }
                    } , "polymorphism is lost" );
            copy.ClassListNonSerializable.Should().BeEquivalentTo( collComp.ClassListNonSerializable );
        }

        [Test]
        public void RuntimeGDOCreationAndSerialization( [Values]EBackend backend )
        {
             //Arrange
             var obj = GDObject.CreateInstance<GDObject>();

             //Act
             var buffer = GetBuffer( backend );
             var writer = GetWriter( backend, buffer );
             var serializer = new GDObjectSerializer( writer );
             serializer.Serialize( obj );

             LogBuffer( buffer );
             SaveToFile( backend, "test", buffer );

             //Assert
             var reader       = GetReader( backend, buffer );
             var deserializer = new GDObjectDeserializer( reader );
             var copyObj      = (GDObject)deserializer.Deserialize();
             obj.Guid.Should().NotBe( Guid.Empty );
             copyObj.Guid.Should().Be( obj.Guid );
        }

        [Test]
        public void GDObjectReferenceTest( [Values]EBackend backend )
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
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer  = new GDObjectSerializer( writer );
                serializer.Serialize( obj1 );
                serializer.Serialize( obj2 );
                serializer.Serialize( obj3 );

                LogBuffer( buffer );

                var reader       = GetReader( backend, buffer, true );
                var deserializer = new GDObjectDeserializer( reader );
                var copyObject1 = deserializer.Deserialize(  );
                var copyObject2 = deserializer.Deserialize(  );
                var copyObject3 = deserializer.Deserialize(  );
                deserializer.ResolveGDObjectReferences();

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
        public void ISerializationCallbackReceiverTest( [Values]EBackend backend )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<TestObjectSerializationCallback>();
                obj1.NonSerialized = "some text";
                var comp = new  SerializationCallbackComponent        { NonSerialized = "some other text"};
                obj1.Components.Add( comp );

                //Act
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new GDObjectSerializer( writer );
                serializer.Serialize( obj1 );

                LogBuffer( buffer );

                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copyObjects = deserializer.Deserialize( );

                //Assert
                var obj1_copy = (TestObjectSerializationCallback)copyObjects;
                obj1_copy.NonSerialized.Should().Be( obj1.NonSerialized );    //Should restore value from serialized field
                obj1_copy.GetComponent<SerializationCallbackComponent>().NonSerialized.Should().Be( comp.NonSerialized );
        }

        [Test]
        public void UnityCustomTypesSupportTest( [Values]EBackend backend )
        {
                //Arrange
                var obj1 = GDObject.CreateInstance<GDObject>();
                var comp = new  UnitySimpleTypesComponent
                           {
                                           Vector3    = -Vector3.one * 0.5f,
                                           Quaternion = Quaternion.Euler( 100, 200, 300 ),
                                           Quaternions = new []{ Quaternion.Euler( 3,2,1 ), Quaternion.Euler( 7,6,5 ),  },
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
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new GDObjectSerializer( writer );
                serializer.Serialize( obj1 );

                LogBuffer( buffer );

                //Assert
                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copyObjects  = (GDObject)deserializer.Deserialize();
                copyObjects.GetComponent<UnitySimpleTypesComponent>().Should().BeEquivalentTo( comp );
        }

        [Test]
        public void DisabledGDObjectTest( [Values]EBackend backend )
        {
                //Arrange
                var disabledObj = GDObject.CreateInstance<GDObject>();
                disabledObj.EnabledObject = false;

                //Act
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new GDObjectSerializer( writer );
                serializer.Serialize( disabledObj );
                LogBuffer( buffer );

                //Assert
                GetBufferLength( buffer ).Should().Be( 0 );
        }

        [Test]
        public void AwakeEnableShouldBeCalledAfterDeserializeTest( [Values]EBackend backend  )
        {
                //Arrange
                var obj = GDObject.CreateInstance<TestObjectAwakeEnable>();

                //Act
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer   = new GDObjectSerializer( writer );
                serializer.Serialize( obj );

                LogBuffer( buffer );

                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copyObjects  = deserializer.Deserialize( );
                var copy         = (TestObjectAwakeEnable)copyObjects;

                //Assert
                copy.IsAwaked.Should().BeTrue();
                copy.IsEnabled.Should().BeTrue();
        }

        [Test]
        public void UnityAssetsSerializationTest_SOAssetResolver( [Values]EBackend backend  )
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
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer   = new GDObjectSerializer( writer );
                serializer.Serialize( obj , testAssetResolver );

                LogBuffer( buffer );

                //Assert
                var reader       = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );
                var copyObjects  = (GDObject)deserializer.Deserialize( null, testAssetResolver );
                var copyComp     = copyObjects.GetComponent<UnityAssetReferenceComponent>();
                copyComp.Texture2D.Should().BeSameAs( testTexture );
                copyComp.Material.Should().BeSameAs( testMat );
                copyComp.GameObject.Should().BeSameAs( testGO );
                copyComp.Component.Should().BeSameAs( testComponent );
        }

        [Test]
        public void FullDBSerializationTest( [Values]EBackend backend )
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
            var buffer = GetBuffer( backend );
            var writer = GetWriter( backend, buffer );
            var serializer = new DBDataSerializer();
            serializer.Serialize( writer, root , NullGdAssetResolver.Instance );

            LogBuffer( buffer );

            var reader       = GetReader( backend, buffer );
            var deserializer = new DBDataSerializer();
            var rootFolder = deserializer.Deserialize( reader, NullGdAssetResolver.Instance, out _ ).rootFolder;

            //Assert
            rootFolder.SubFolders.Count.Should().Be( 2 );
            rootFolder.Name.Should().Be( "Root" );
            var mobsFolder = rootFolder.SubFolders.Single( f => f.Name == "Mobs" );
            mobsFolder.Objects.Count.Should().Be( 1 );
            var elvesFolder = mobsFolder.SubFolders.Single( f => f.Name == "Elves" );
            elvesFolder.Objects.Count.Should().Be( 2 );
            elvesFolder.Objects.OfType<GDObject>().Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.SubFolders.Should().BeEmpty();
            elvesFolder.Objects.OfType<GDObject>().Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.Objects.OfType<GDObject>().Select( gdo => gdo.Name ).Should().BeEquivalentTo( new[] { "Elf1", "Elf2" } );
        }

        [Test]
        public void FoldersWithoutObjectsSerializationTest( [Values]EBackend backend )
        {
                //Arrange
                var root      = GetFolder( "Root",      null  ) ;
                var mobs      = GetFolder( "Mobs",      root); 
                var elves     = GetFolder( "Elves",     mobs); 
                var locations = GetFolder( "Locations", root);

                var gdRoot         = CreateGDObject<GDRoot>( "Root" ); root.Objects.Add( gdRoot );
                var elf1           = CreateGDObject( "Elf1" ); elves.Objects.Add( elf1 );
                var elf2           = CreateGDObject( "Elf2" ); elves.Objects.Add( elf2 );
                var mobsSettings   = CreateGDObject( "MobsSettings" ); mobs.Objects.Add( mobsSettings );
                var forestLocation = CreateGDObject( "Forest" ); locations.Objects.Add( forestLocation );

                //Act
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new FolderSerializer();
                var objectSerializer = new GDObjectSerializer( writer );                        //Serialized with Objects
                serializer.Serialize( root, objectSerializer, writer );

                LogBuffer( buffer );

                var reader = GetReader( backend, buffer );
                var folderSerializer = new FolderSerializer();
                var rootFolder       = folderSerializer.Deserialize( reader, null ); //Because objectSerializer is null, we don't deserialized objects

                //Assert
                rootFolder.SubFolders.Count.Should().Be( 2 );
                rootFolder.Name.Should().Be( "Root" );
                var mobsFolder = rootFolder.SubFolders.Single( f => f.Name == "Mobs" );
                mobsFolder.Objects.Count.Should().Be( 0 );
                var elvesFolder = mobsFolder.SubFolders.Single( f => f.Name == "Elves" );
                elvesFolder.Objects.Count.Should().Be( 0 );
                elvesFolder.SubFolders.Should().BeEmpty();
        }


        [Test]
        public void ReaderPathTests( [Values(EBackend.JsonNet)]EBackend backend )
        {
                //Arrange
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new GDObjectSerializer( writer );         

                var gdo = GDObject.CreateInstance<GDObject>();
                var comp = new CollectionTestComponent();
                gdo.Components.Add( comp );
                serializer.Serialize( gdo );

                LogBuffer( buffer );

                //Act and Assert
                var reader = GetReader( backend, buffer );
                var deserializer = new GDObjectDeserializer( reader );                                  //Create unused deserializer to set-up property name aliases
                reader.SeekPropertyName( GDObjectSerializationCommon.NameTag );
                reader.Path.Should().Contain( GDObjectSerializationCommon.NameTag );

                reader.SeekPropertyName( GDObjectSerializationCommon.ComponentsTag );
                reader.Path.Should().Contain( GDObjectSerializationCommon.ComponentsTag );

                reader.SeekPropertyName( GDObjectSerializationCommon.TypeTag );
                reader.Path.Should().Contain( GDObjectSerializationCommon.ComponentsTag );          //Property of GDObject
                reader.Path.Should().Contain( "[0]" );                  //Index of GDComponent
                reader.Path.Should().Contain( GDObjectSerializationCommon.TypeTag );                //Property of GDComponent

                reader.SeekPropertyName( "IntArray" );
                reader.ReadStartArray();
                reader.ReadNextToken();                                         //Read first element
                reader.ReadNextToken();                                         //Read second element
                reader.Path.Should().Contain( GDObjectSerializationCommon.ComponentsTag );          
                reader.Path.Should().Contain( "[0]" );                  
                reader.Path.Should().Contain( "IntArray" );                  
                reader.Path.Should().Contain( "[1]" );            
                
                Debug.Log( reader.Path );
        }
        
        [Test]
        public void TestDBSerializer( [Values]EBackend backend )
        {
                //Arrange
                var buffer = GetBuffer( backend );
                var writer = GetWriter( backend, buffer );
                var serializer = new DBDataSerializer(  );         

                var rootFolder = GetFolder( "Root", null );
                var gdo = GDObject.CreateInstance<GDRoot>();
                var comp = new CollectionTestComponent();
                gdo.Components.Add( comp );
                rootFolder.Objects.Add( gdo );

                serializer.Serialize( writer, rootFolder, NullGdAssetResolver.Instance );

                LogBuffer( buffer );

                //Act and Assert
                var reader = GetReader( backend, buffer );
                var deserializer = new DBDataSerializer(  );
                var (readRoot, objects) = deserializer.Deserialize( reader, NullGdAssetResolver.Instance, out var storedHash );
                readRoot.Objects.Count.Should().Be( rootFolder.Objects.Count );
                storedHash.Should().Be( rootFolder.GetFoldersChecksum() );
                storedHash.Should().Be( readRoot.GetFoldersChecksum() );
                
        }
        
        [Test]
        public void TestBinaryReaderWriterCopy(  )
        {
                var backend = EBackend.Binary;

                //Arrange
                var buffer     = GetBuffer( backend );
                var writer     = GetWriter( backend, buffer );
                var serializer = new DBDataSerializer(  );         

                var rootFolder = GetFolder( "Root", null );
                var gdo        = GDObject.CreateInstance<GDRoot>();
                var comp       = new CollectionTestComponent();
                gdo.Components.Add( comp );
                rootFolder.Objects.Add( gdo );

                serializer.Serialize( writer, rootFolder, NullGdAssetResolver.Instance );

                LogBuffer( buffer );

                //Act and Assert
                var buffer2 = GetBuffer( backend );
                var writer2 = GetWriter( backend, buffer2 );
                var reader       = GetReader( backend, buffer );

                writer2.Copy( reader );

                var memory1 = ((MemoryStream)buffer).ToArray();
                var memory2 = ((MemoryStream)buffer2).ToArray();
                memory2.Should().BeEquivalentTo( memory1 );
        }
        
        [Test]
        public void TestBinaryCompression(  )
        {
                var backend = EBackend.Binary;

                //Arrange
                var buffer     = GetBuffer( backend );
                var writer     = GetWriter( backend, buffer );
                var serializer = new DBDataSerializer(  );                      //Compression/decompression incapsulated in DB Data serializer

                var rootFolder = GetFolder( "Root", null );
                var gdo        = GDObject.CreateInstance<GDRoot>();
                var comp       = new CollectionTestComponent();
                gdo.Components.Add( comp );
                gdo.Components.Add( comp );
                gdo.Components.Add( comp );
                rootFolder.Objects.Add( gdo );

                serializer.Serialize( writer, rootFolder, NullGdAssetResolver.Instance );

                LogBuffer( buffer );

                //Act and Assert
                var reader       = GetReader( backend, buffer );
                var deserializer = new DBDataSerializer(  );
                var result = deserializer.Deserialize( reader, NullGdAssetResolver.Instance, out _ );
                result.rootFolder.Objects.Count.Should().Be( 1 );
                ( (GDObject)result.objects[ 0 ].Object ).Components.Count.Should().Be( 3 );
        }

        [Test]
        public void TestScriptableObjectsInDB( [Values]EBackend backend)
        {
              //Arrange
              var so1 = ScriptableObject.CreateInstance<TestSO>();
              var so2 = ScriptableObject.CreateInstance<TestSO2>();
              var gdo = GDObject.CreateInstance<GDObject>();
              var rootFolder = GetFolder( "Root", null );

              rootFolder.Objects.Add( so1 );
              rootFolder.Objects.Add( so2 );
              rootFolder.Objects.Add( gdo );

              so1.name = "SO1";
              so1.Value = 42;
              so1.SOObjectReference = so2;
              so1.SelfReference = so1;
              so1.GDObjectReference = gdo;

              so2.name = "SO2";
              so2.CircularReference = so1;

              gdo.name = "GDO";

              //Act
              var buffer           = GetBuffer( backend );
              var writer           = GetWriter( backend, buffer );
              var serializer       = new FolderSerializer();
              var objectSerializer = new GDObjectSerializer( writer );
              serializer.Serialize( rootFolder, objectSerializer, writer );

              LogBuffer( buffer );

              var reader           = GetReader( backend, buffer );
              var folderSerializer = new FolderSerializer();
              var objectDeserializer = new GDObjectDeserializer( reader );
              var rootFolder2       = folderSerializer.Deserialize( reader, objectDeserializer );
              objectDeserializer.ResolveGDObjectReferences();

              //Assert
              objectDeserializer.LoadedObjects.Count.Should().Be( 3 );
              rootFolder2.Objects.Count.Should().Be( rootFolder.Objects.Count );
              rootFolder2.Name.Should().Be( "Root" );

              var so1_copy = (TestSO)rootFolder2.Objects.First( so => so.name   == "SO1" );
              var so2_copy = (TestSO2)rootFolder2.Objects.First( so => so.name  == "SO2" );
              var gdo_copy = (GDObject)rootFolder2.Objects.First( so => so.name == "GDO" );
              so1_copy.Value.Should().Be( so1.Value );
              so1_copy.SelfReference.Should().BeSameAs( so1_copy );
              so1_copy.SOObjectReference.Should().BeSameAs( so2_copy );
              so1_copy.GDObjectReference.Should().BeSameAs( gdo_copy );

              so2_copy.Value.Should().Be( string.Empty );                       //Unity deserialize null string as empty
              so2_copy.CircularReference.Should().BeSameAs( so1_copy );
        }

        [Test]
        public void TesNullGDObjectReferences( [Values]EBackend backend)
        {
              //Arrange
              var so1 = ScriptableObject.CreateInstance<TestSO>();
              var so2 = ScriptableObject.CreateInstance<TestSO2>();
              var gdo = GDObject.CreateInstance<GDObject>();
              var rootFolder = GetFolder( "Root", null );

              rootFolder.Objects.Add( so1 );
              rootFolder.Objects.Add( so2 );
              rootFolder.Objects.Add( gdo );

              so1.name = "SO1";
              so2.name = "SO2";
              gdo.name = "GDO";

              //Act
              var buffer           = GetBuffer( backend );
              var writer           = GetWriter( backend, buffer );
              var serializer       = new FolderSerializer();
              var objectSerializer = new GDObjectSerializer( writer );
              serializer.Serialize( rootFolder, objectSerializer, writer );

              LogBuffer( buffer );

              var reader           = GetReader( backend, buffer );
              var folderSerializer = new FolderSerializer();
              var objectDeserializer = new GDObjectDeserializer( reader );
              var rootFolder2       = folderSerializer.Deserialize( reader, objectDeserializer );
              objectDeserializer.ResolveGDObjectReferences();

              //Assert
              objectDeserializer.LoadedObjects.Count.Should().Be( 3 );
              rootFolder2.Objects.Count.Should().Be( rootFolder.Objects.Count );
              rootFolder2.Name.Should().Be( "Root" );

              var so1_copy = (TestSO)rootFolder2.Objects.First( so => so.name   == "SO1" );
              var so2_copy = (TestSO2)rootFolder2.Objects.First( so => so.name  == "SO2" );
              //var gdo_copy = (GDObject)rootFolder2.Objects.First( so => so.name == "GDO" );
              so1_copy.Value.Should().Be( so1.Value );
              so1_copy.SelfReference.Should().BeNull(  );
              so1_copy.SOObjectReference.Should().BeNull(  );
              so1_copy.GDObjectReference.Should().BeNull(  );

              so2_copy.Value.Should().Be( string.Empty );                       //Unity deserialize null string as empty
              so2_copy.CircularReference.Should().BeNull(  );
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

        private GdFolder GetFolder( String name, GdFolder parent )
        { 
                if ( parent != null )
                {
                        var result = new GdFolder( name, Guid.NewGuid(), parent );
                        return result;
                }
                else
                {
                        return new GdFolder( name, Guid.NewGuid() );
                }
        }

        private static IEnumerable<System.Object[]> DataSourceForPrimitiveComponentTest( )
        {
                var data = new System.Object[]
                {
                        new System.Object[] { false, 0D, 0F,  (SByte)0, 0, 0L, 0UL, "", 'a',
                                PrimitivesComponent.EByteFlagEnum.Zero, PrimitivesComponent.EIntEnum.Zero },
                        new System.Object[] { true,  Double.MinValue, Single.MinValue, SByte.MinValue, Int32.MinValue, Int64.MinValue, UInt64.MinValue, 
                                "", 
                                'Я',
                                PrimitivesComponent.EByteFlagEnum.One,
                                PrimitivesComponent.EIntEnum.One },
                        new System.Object[] { true,     Double.MaxValue, Single.MaxValue, SByte.MaxValue, Int32.MaxValue, Int64.MaxValue, UInt64.MaxValue,
                                "some ansi text", '\u005E', PrimitivesComponent.EByteFlagEnum.Last,
                                PrimitivesComponent.EIntEnum.Last },
                        new System.Object[] { true, Double.NegativeInfinity, Single.NegativeInfinity, (SByte)0, 0, 0, UInt64.MaxValue - 1, "кирилиця", Char.MinValue,
                                PrimitivesComponent.EByteFlagEnum.Last,
                                PrimitivesComponent.EIntEnum.Last },
                        new System.Object[] { true, Double.PositiveInfinity, Single.PositiveInfinity, (SByte)0, 0, Int64.MinValue + 1, 0UL,
                                "some chinese \u4E2D",
                                Char.MaxValue, PrimitivesComponent.EByteFlagEnum.Zero | PrimitivesComponent.EByteFlagEnum.One,
                                PrimitivesComponent.EIntEnum.Last },
                        new System.Object[] { true, Double.NaN, Single.NaN, (SByte)0, 0, 0, UInt64.MaxValue - 2, "some smile \u263A", '!',
                                PrimitivesComponent.EByteFlagEnum.Last,
                                PrimitivesComponent.EIntEnum.Last }
                };

                var backends = Enum.GetValues( typeof(EBackend) );

                foreach ( System.Object[] d in data )
                {
                        foreach ( var b in backends )
                        {
                                yield return  d.Prepend( b ).ToArray();
                        }
                }
        }

        public void RunStarted(ITest testsToRun )
        {
                Debug.Log( $"[{nameof(SerializationTests)}]-[{nameof(RunStarted)}] {testsToRun.Name}" );
        }

        public void RunFinished(ITestResult testResults )
        {
                Debug.Log( $"[{nameof(SerializationTests)}]-[{nameof(RunFinished)}] " );
        }

        public void TestStarted(ITest test )
        {
                Debug.Log( $"[{nameof(SerializationTests)}]-[{nameof(TestStarted)}] {test.Name}" );
                using var logFile = File.AppendText( "UniTest.log" );
                logFile.WriteLine( $"[{nameof(SerializationTests)}]-[{nameof(TestStarted)}] {test.Name}" );
        }

        public void TestFinished(ITestResult result )
        {
                Debug.Log( $"[{nameof(SerializationTests)}]-[{nameof(TestFinished)}] " );
                using var logFile = File.AppendText( "UniTest.log" );
                logFile.WriteLine( $"[{nameof(SerializationTests)}]-[{nameof(TestFinished)}] {result.Name}" );

        }
    }
}