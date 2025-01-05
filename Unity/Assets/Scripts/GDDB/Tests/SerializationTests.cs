using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using GDDB.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GDDB.Tests
{
    public class SerializationTests : BaseSerializationTests
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
            reader.SeekPropertyName( ".Components" );                   //Seek to components array           
            reader.SeekPropertyName( ".Type" );                         //Seek to component type                                 

            //Make sure there are no serialized properties in NotSerializableContentComponent
            while ( reader.ReadNextToken() != EToken.EndObject )
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
            var comp_copy    = deserializer.Deserialize( ).Components.First() as PrimitivesComponent;
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
            var copy         = deserializer.Deserialize( ).Components.First() as NullReferencesAsEmptyComponent;
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
        public void EnumsTest( [Values]EBackend backend )
        {
                //Arrange
                var enumComp = new EnumsComponent()
                               {
                                               DefaultEnum  = DefaultEnum.Third,
                                               BigEnum      = UInt64Enum.Last,
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
                var copy         = deserializer.Deserialize( ).Components.First() as EnumsComponent;
                copy.DefaultEnum.Should().Be( enumComp.DefaultEnum );
                copy.BigEnum.Should().Be( enumComp.BigEnum );
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
                var copy         = deserializer.Deserialize( ).Components.First() as NullReferencesAsEmptyComponent;
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
            var testObj = ScriptableObject.CreateInstance<GDObject>();
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
            var copy         = deserializer.Deserialize(  ).Components.First() as CollectionTestComponent;
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
             var copyObj      = deserializer.Deserialize(  );
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
                var copyObjects  = deserializer.Deserialize( );
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
                var copyObjects  = deserializer.Deserialize( testAssetResolver );
                var copyComp     = copyObjects.GetComponent<UnityAssetReferenceComponent>();
                copyComp.Texture2D.Should().BeSameAs( testTexture );
                copyComp.Material.Should().BeSameAs( testMat );
                copyComp.GameObject.Should().BeSameAs( testGO );
                copyComp.Component.Should().BeSameAs( testComponent );
        }

        [Test]
        public void FoldersSerializationTest( [Values]EBackend backend )
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
            serializer.Serialize( writer, root, root.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() , NullGdAssetResolver.Instance );

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
            elvesFolder.Objects.Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.SubFolders.Should().BeEmpty();
            elvesFolder.Objects.Select( gdo => gdo.Guid ).Should().BeEquivalentTo( new[] { elf1.Guid, elf2.Guid } );
            elvesFolder.Objects.Select( gdo => gdo.Name ).Should().BeEquivalentTo( new[] { "Elf1", "Elf2" } );
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
                var serializer = new DBDataSerializer();
                serializer.Serialize( writer, root, root.EnumerateFoldersDFS(  ).SelectMany( f => f.Objects ).ToArray() , NullGdAssetResolver.Instance );

                LogBuffer( buffer );

                var reader = GetReader( backend, buffer );
                var folderSerializer = new FolderSerializer();
                var rootFolder       = folderSerializer.Deserialize( reader, null, out _ ); //Because objectSerializer is null, we don't get objects

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
        public void ReaderPathTests( [Values]EBackend backend )
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
                reader.SeekPropertyName( ".Name" );
                reader.Path.Should().Contain( ".Name" );

                reader.SeekPropertyName( ".Components" );
                reader.Path.Should().Contain( ".Components" );

                reader.SeekPropertyName( ".Type" );
                reader.Path.Should().Contain( ".Components" );          //Property of GDObject
                reader.Path.Should().Contain( "[0]" );                  //Index of GDComponent
                reader.Path.Should().Contain( ".Type" );                //Property of GDComponent

                reader.SeekPropertyName( "IntArray" );
                reader.ReadStartArray();
                reader.ReadNextToken();                                         //Read first element
                reader.ReadNextToken();                                         //Read second element
                reader.Path.Should().Contain( ".Components" );          
                reader.Path.Should().Contain( "[0]" );                  
                reader.Path.Should().Contain( "IntArray" );                  
                reader.Path.Should().Contain( "[1]" );            
                
                Debug.Log( reader.Path );
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
    }
}