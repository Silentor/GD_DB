using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using FluentAssertions;
using GDDB.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using BinaryReader = GDDB.Serialization.BinaryReader;
using BinaryWriter = GDDB.Serialization.BinaryWriter;
using Object = System.Object;

namespace GDDB.Tests
{
    public class LowLevelSerializerTests : BaseSerializationTests
    {
        [Test]
        public void TestBasicSerializer( [Values]EBackend backend )
        {
            // Write
            var buffer = GetBuffer( backend );
            var serializer   = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "testName" );
            serializer.WriteValue( "testValue" );
            serializer.WritePropertyName( "testInt" );
            serializer.WriteValue( 42 );
            serializer.WritePropertyName( "testIntArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 1 );
                serializer.WriteValue( 2 );
                serializer.WriteValue( 3 );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "testMiscArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 42 );
                serializer.WriteValue( "testValue" );
                serializer.WriteStartObject();
                    serializer.WritePropertyName( "embeddedObject" );
                    serializer.WriteValue( "embeddedValue" );
                serializer.WriteEndObject();
            serializer.WriteEndArray();

            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );

            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName(  ),  "testName" );      //Read propertyName
            Assert.AreEqual( deserializer.ReadStringValue(  ),   "testValue" );     //Read property value
            Assert.AreEqual( deserializer.ReadPropertyInt32( "testInt" ), 42 );     //Read property name and value

            Assert.AreEqual( deserializer.ReadPropertyName(  ),             "testIntArray" );

            deserializer.ReadStartArray();
            var counter = 0;
            while( deserializer.ReadNextToken() != EToken.EndArray )
            {
                var value = deserializer.GetInt32Value(  );
                Assert.AreEqual( value, ++counter );
            }
            deserializer.EnsureEndArray();

            Assert.AreEqual( deserializer.ReadPropertyName(  ), "testMiscArray" );
            deserializer.ReadStartArray();
            while( deserializer.ReadNextToken() != EToken.EndArray )
            {
                switch ( deserializer.CurrentToken )
                {
                    case EToken.Number:
                        var intValue = deserializer.GetInt32Value(  );
                        Assert.AreEqual( intValue, 42 );
                        break;                    
                    case EToken.String:
                        var stringValue = deserializer.GetStringValue(  );
                        Assert.AreEqual( stringValue, "testValue" );
                        break;
                    case EToken.StartObject:
                        deserializer.EnsureStartObject();
                        Assert.AreEqual( deserializer.ReadPropertyName(  ), "embeddedObject" );
                        Assert.AreEqual( deserializer.ReadStringValue(  ),  "embeddedValue" );
                        deserializer.ReadEndObject();
                        break;
                }
            }
            deserializer.EnsureEndArray();

            deserializer.ReadEndObject();

            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
        }

        [Test]
        public void TestEnumerateTokens( [Values]EBackend backend )
        {
            // Write
            var buffer = GetBuffer( backend );
            var serializer   = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "testName" );
            serializer.WriteValue( "testValue" );
            serializer.WritePropertyName( "testInt" );
            serializer.WriteValue( 42 );
            serializer.WritePropertyName( "testIntArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 1 );
                serializer.WriteValue( 2 );
                serializer.WriteValue( 3 );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "testMiscArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 42 );
                serializer.WriteValue( "testValue" );
                serializer.WriteStartObject();
                    serializer.WritePropertyName( "embeddedObject" );
                    serializer.WriteValue( "embeddedValue" );
                serializer.WriteEndObject();
            serializer.WriteEndArray();

            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var resultTokens = new[]
            {
                EToken.StartObject,
                EToken.PropertyName,
                EToken.String,
                EToken.PropertyName,
                EToken.Int32,
                EToken.PropertyName,
                EToken.StartArray,
                EToken.Int32,
                EToken.Int32,
                EToken.Int32,
                EToken.EndArray,
                EToken.PropertyName,
                EToken.StartArray,
                EToken.Int32,
                EToken.String,
                EToken.StartObject,
                EToken.PropertyName,
                EToken.String,
                EToken.EndObject,
                EToken.EndArray,
                EToken.EndObject,
                //EToken.EoF
            };

            var deserializer = GetReader( backend, buffer );
            var i = 0;
            while ( deserializer.ReadNextToken() != EToken.EoF )
            {
                var expectedToken = resultTokens[ i++ ];
                var actualToken   = deserializer.CurrentToken;
                Assert.AreEqual( ReduceIntegerToken( actualToken ), ReduceIntegerToken( expectedToken), $"Error at index {i - 1}" );
            }
        }

        [Test]
        public void TestSkipProperties( [Values]EBackend backend )
        {
            // Write
            var buffer = GetBuffer( backend );
            var serializer   = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "testName" );
            serializer.WriteValue( "testValue" );
            serializer.WritePropertyName( "testInt" );
            serializer.WriteValue( 42 );
            serializer.WritePropertyName( "testIntArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 1 );
                serializer.WriteValue( 2 );
                serializer.WriteValue( 3 );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "testMiscArray" );
            serializer.WriteStartArray();
                serializer.WriteValue( 42 );
                serializer.WriteValue( "testValue" );
                serializer.WriteStartObject();
                    serializer.WritePropertyName( "embeddedObject" );
                    serializer.WriteValue( "embeddedValue" );
                serializer.WriteEndObject();
            serializer.WriteEndArray();

            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            var deserializer = GetReader( backend, buffer );
            Assert.AreEqual( EToken.StartObject, deserializer.ReadNextToken() );
            Assert.AreEqual( EToken.PropertyName, deserializer.ReadNextToken() );
            deserializer.SkipProperty();                                                                //Skip "testName" primitive property staying on property name token
            Assert.AreEqual( EToken.String, deserializer.CurrentToken );                        //After skip we should stay on last token of skipped property
            Assert.AreEqual( "testInt", deserializer.ReadPropertyName() );
            deserializer.SkipProperty();                                                                //Skip "testInt" primitive property staying on property name value
            Assert.IsTrue( deserializer.CurrentToken.IsIntegerToken() );                        //After skip we should stay on last token of skipped property
            Assert.AreEqual( "testIntArray", deserializer.ReadPropertyName() );
            deserializer.SkipProperty();                                                                //Skip "testIntArray" array property
            Assert.AreEqual( EToken.EndArray, deserializer.CurrentToken );                        //After skip we should stay on last token of skipped property
            Assert.AreEqual( "testMiscArray",     deserializer.ReadPropertyName() );
            Assert.AreEqual( EToken.StartArray,   deserializer.ReadNextToken() );
            Assert.IsTrue( deserializer.ReadNextToken().IsIntegerToken() );
            Assert.AreEqual( EToken.String,       deserializer.ReadNextToken() );
            Assert.AreEqual( EToken.StartObject,  deserializer.ReadNextToken() );
            Assert.AreEqual( EToken.PropertyName, deserializer.ReadNextToken() );
            deserializer.SkipProperty();                                                                //Skip "embeddedObject" property
            Assert.AreEqual( EToken.String,    deserializer.CurrentToken );                     //After skip we should stay on last token of skipped property
            Assert.AreEqual( EToken.EndObject, deserializer.ReadNextToken() );
            Assert.AreEqual( EToken.EndArray,  deserializer.ReadNextToken() );
            Assert.AreEqual( EToken.EndObject, deserializer.ReadNextToken() );
        }

        [Test]
        public void TestOptionalProperties( [Values]EBackend backend, [Values]bool includeProp1, [Values]bool includeProp2 )
        {
            // Write
            var buffer = GetBuffer( backend );
            var serializer   = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            if( includeProp1 )
            {
                serializer.WritePropertyName( "optionalProp1" );
                serializer.WriteValue( "testValue" );
            }
            if( includeProp2 )
            {
                serializer.WritePropertyName( "optionalProp2" );
                serializer.WriteValue( 42 );
            }
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );

            deserializer.ReadStartObject();
            while( deserializer.TryReadPropertyName( out var propName ) )
            {
                if( propName == "optionalProp1" )
                {
                    Assert.AreEqual( deserializer.ReadStringValue(  ), "testValue" );
                }
                else if( propName == "optionalProp2" )
                {
                    Assert.AreEqual( deserializer.ReadInt32Value(  ), 42 );
                }
            }

            deserializer.EnsureEndObject();
        }

        [Test]
        public void TestInt8( [Values]EBackend backend, [Values(0, 1, -1, SByte.MaxValue, SByte.MinValue)]SByte value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestInt" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( "TestInt", deserializer.ReadPropertyName());
            var actualValue = deserializer.ReadIntegerValue( );
            actualValue.Should().Be( value );
        }

        [Test]
        public void TestInt32( [Values]EBackend backend, [Values(0, 1, -1, Int32.MaxValue, Int32.MinValue)]Int32 value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestInt32" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert via typed read
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            var actualValue = deserializer.ReadPropertyInt32( "TestInt32" );
            actualValue.Should().Be( value );

            // Read and assert via generic read
            deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            deserializer.ReadPropertyName();
            var actualValue2 = deserializer.ReadIntegerValue( );
            actualValue2.Should().Be( value );
        }

        [Test]
        public void TestUInt64( [Values]EBackend backend, [Values(1UL, UInt64.MaxValue, UInt64.MinValue)]UInt64 value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestUInt64" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( "TestUInt64", deserializer.ReadPropertyName(  ) );
            //Assert.AreEqual( EToken.UInt64, deserializer.ReadNextToken() );    JSON.NET fail that assert, its guess type from value, so its know that value is Integer but cannot know that its UInt64 (for 0 value for example) 
            var actualValue = deserializer.ReadUInt64Value( );
            Assert.AreEqual( value, actualValue );
        }

        [Test]
        public void TestFloats( [Values]EBackend backend )
        {
            var singleValues = new Single[] { 0, 1, -1, Single.MinValue, Single.MaxValue, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon };
            var doubleValues = new Double[] { 0, 1, -1, Double.MinValue, Double.MaxValue, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon };

            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestDoubles" );
            serializer.WriteStartArray();
            foreach ( var dbl in doubleValues )                
                serializer.WriteValue( dbl );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "TestSingles" );
            serializer.WriteStartArray();
            foreach ( var sng in singleValues )                
                serializer.WriteValue( sng );
            serializer.WriteEndArray();
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName(  ), "TestDoubles" );
            deserializer.ReadStartArray();
            var counter = 0;
            while ( deserializer.ReadNextToken() != EToken.EndArray )
            {
                var actualValue = deserializer.GetFloatValue(  );
                Assert.AreEqual( doubleValues[ counter++ ], actualValue );
            }
            deserializer.EnsureEndArray();
            Assert.AreEqual( deserializer.ReadPropertyName(  ), "TestSingles" );
            deserializer.ReadStartArray();
            counter = 0;
            while ( deserializer.ReadNextToken() != EToken.EndArray )
            {
                var actualValue = deserializer.GetFloatValue(  );
                Assert.AreEqual( singleValues[ counter++ ], (Single)actualValue );
            }
            deserializer.EnsureEndArray();
        }

        [Test]
        public void TestSingles( [Values]EBackend backend, [Values(0, 1, -1, Single.MinValue, Single.MaxValue, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon)]Single value )
        {
            // Write
            var buffer = GetBuffer( backend );
            var serializer   = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestFloat" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            var actualValue = deserializer.ReadPropertySingle( "TestFloat" );
            Assert.AreEqual( value, actualValue );
        }

        [Test]
        public void TestDoubles( [Values]EBackend backend, [Values(0, 1, -1, Double.MinValue, Double.MaxValue, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon)]Double value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestDouble" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName(  ), "TestDouble" );
            deserializer.ReadNextToken();
            var actualValue = deserializer.GetDoubleValue(  );
            Assert.AreEqual( value, actualValue );
        }

        [Test]
        public void TestAliases( [Values(EBackend.Binary)]EBackend backend )
        {
            // Write
            backend = EBackend.Binary;
            var buffer     = GetBuffer( backend );
            var serializer = (BinaryWriter)GetWriter( backend, buffer );
            serializer.SetAlias( 0, EToken.PropertyName, "TestAliasPropName" );
            serializer.SetAlias( 1, EToken.String, "TestAliasValue" );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestAliasPropName" );
            serializer.WriteValue( 0 );
            serializer.WritePropertyName( "NormalPropertyName" );
            serializer.WriteValue( "TestAliasValue" );
            serializer.WritePropertyName( "TestAliasPropName" );
            serializer.WriteValue( "TestAliasValue" );

            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            {
                // Read without setting alias, should read alias token
                var deserializer = GetReader( backend, buffer );
                var aliasesCount = 0;
                while(deserializer.ReadNextToken() != EToken.EoF)
                {
                    if ( deserializer.CurrentToken.IsAliasToken() )
                        aliasesCount++;
                }
                aliasesCount.Should().Be( 4 );
            }

            { 
                //Read with alias processing, should read aliased property name
                var deserializer = (BinaryReader)GetReader( backend, buffer );
                deserializer.SetAlias( 0, EToken.PropertyName, "TestAliasPropName" );
                deserializer.SetAlias( 1, EToken.String,       "TestAliasValue" );
                deserializer.ReadStartObject();
                deserializer.ReadNextToken().Should().Be( EToken.PropertyName );
                deserializer.GetPropertyName().Should().Be( "TestAliasPropName" );
                deserializer.ReadIntegerValue().Should().Be( 0 );
                deserializer.ReadPropertyName().Should().Be( "NormalPropertyName" );
                deserializer.ReadStringValue().Should().Be( "TestAliasValue" );
                deserializer.ReadPropertyName().Should().Be( "TestAliasPropName" );
                deserializer.ReadStringValue().Should().Be( "TestAliasValue" );
            }
        }

        [Test]
        public void TestGuids( [Values]EBackend backend )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestGuids" );
            serializer.WriteStartArray();
            serializer.WriteValue( Guid.Empty );
            serializer.WriteValue( Guid.Parse( "6CBA8F3D-BF44-44B0-9B76-0A85E836C29A" ) );
            serializer.WriteNullValue();
            serializer.WriteEndArray();
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName(  ), "TestGuids" );
            deserializer.ReadStartArray();
            Assert.AreEqual( Guid.Empty,                                           deserializer.ReadGuidValue() );
            Assert.AreEqual( Guid.Parse( "6CBA8F3D-BF44-44B0-9B76-0A85E836C29A" ), deserializer.ReadGuidValue() );
            Assert.AreEqual( Guid.Empty,                                           deserializer.ReadGuidValue() );
            deserializer.ReadEndArray();
            deserializer.ReadEndObject();
        }

        [Test]
        public void TestEnums( [Values]EBackend backend )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "DefaultEnum" );
            serializer.WriteStartArray();
            serializer.WriteValue( DefaultEnum.Zero );
            serializer.WriteValue( DefaultEnum.First );
            serializer.WriteValue( DefaultEnum.Third );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "UInt64Enum" );
            serializer.WriteStartArray();
            serializer.WriteValue( UInt64Enum.Zero );
            serializer.WriteValue( UInt64Enum.Second );
            serializer.WriteValue( UInt64Enum.Third );
            serializer.WriteValue( UInt64Enum.Last );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "SignedEnum" );
            serializer.WriteStartArray();
            serializer.WriteValue( SignedEnum.Zero );
            serializer.WriteValue( SignedEnum.First );
            serializer.WriteValue( SignedEnum.Last );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "Flags" );
            serializer.WriteStartArray();
            serializer.WriteValue( Flags.None );
            serializer.WriteValue( Flags.First );
            serializer.WriteValue( Flags.First | Flags.Third );
            serializer.WriteValue( Flags.All );
            serializer.WriteEndArray();
            serializer.WritePropertyName( "NullEnum" );
            serializer.WriteNullValue();
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName(  ), "DefaultEnum" );
            deserializer.ReadStartArray();
            Assert.AreEqual( DefaultEnum.Zero,  deserializer.ReadEnumValue( typeof(DefaultEnum) ) );
            Assert.AreEqual( DefaultEnum.First, deserializer.ReadEnumValue( typeof(DefaultEnum) ) );
            Assert.AreEqual( DefaultEnum.Third, deserializer.ReadEnumValue( typeof(DefaultEnum) ) );
            deserializer.ReadEndArray();

            Assert.AreEqual(deserializer.ReadPropertyName(), "UInt64Enum");
            deserializer.ReadStartArray();
            Assert.AreEqual(UInt64Enum.Zero,   deserializer.ReadEnumValue(typeof(UInt64Enum)));
            Assert.AreEqual(UInt64Enum.Second, deserializer.ReadEnumValue(typeof(UInt64Enum)));
            Assert.AreEqual(UInt64Enum.Third,  deserializer.ReadEnumValue(typeof(UInt64Enum)));
            Assert.AreEqual(UInt64Enum.Last,   deserializer.ReadEnumValue(typeof(UInt64Enum)));
            deserializer.ReadEndArray();

            Assert.AreEqual(deserializer.ReadPropertyName(), "SignedEnum");
            deserializer.ReadStartArray();
            Assert.AreEqual(SignedEnum.Zero,  deserializer.ReadEnumValue(typeof(SignedEnum)));
            Assert.AreEqual(SignedEnum.First, deserializer.ReadEnumValue(typeof(SignedEnum)));
            Assert.AreEqual(SignedEnum.Last,  deserializer.ReadEnumValue(typeof(SignedEnum)));
            deserializer.ReadEndArray();

            Assert.AreEqual(deserializer.ReadPropertyName(), "Flags");
            deserializer.ReadStartArray();
            Assert.AreEqual(Flags.None,                deserializer.ReadEnumValue(typeof(Flags)));
            Assert.AreEqual(Flags.First,               deserializer.ReadEnumValue(typeof(Flags)));
            Assert.AreEqual(Flags.First | Flags.Third, deserializer.ReadEnumValue(typeof(Flags)));
            Assert.AreEqual(Flags.All,                 deserializer.ReadEnumValue(typeof(Flags)));
            deserializer.ReadEndArray();

            Assert.AreEqual(deserializer.ReadPropertyName(), "NullEnum");
            Assert.AreEqual(DefaultEnum.Zero, deserializer.ReadEnumValue(typeof(DefaultEnum)));

            deserializer.ReadEndObject();
        }

        

        [Test]
        public void TestStrings( [Values]EBackend backend, [Values("ASCII test", "\r\n\t", @"\", "\"", "", " ", "кірилиця", "知道", "Europäische", null )]String value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestString" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            var actualValue = deserializer.ReadPropertyString( "TestString" );
            Assert.AreEqual( value, actualValue );
        }

        [Test]
        public void TestBooleans( [Values]EBackend backend, [Values]Boolean value )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "TestBool" );
            serializer.WriteValue( value );
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            deserializer.ReadPropertyName();
            deserializer.ReadNextToken();                var actualValue = deserializer.GetBoolValue(  );
            Assert.AreEqual( value, actualValue );
        }

        [Test]
        public void TestSerializingTypes( [Values] EBackend backend, [Values(
                                                  typeof(UnityEngine.Animator),                                         //Just some plain usual class
                                                  typeof(NoNameSpaceTestClass),                                          //No namespace
                                                  typeof(NestingGenericTestType<Int32>.NestedGenericTestType2<UnityEngine.Animator>), //Closed generic + nested
                                                  typeof(Dictionary<,>),            //Open generic
                                                  typeof(Int32[])                   //Array class
                                                  )]Type testType )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartArray();
            serializer.WriteValue( testType );
            serializer.WriteEndArray();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartArray();
            deserializer.ReadTypeValue(  ).Should().Be( testType );                 //Assembly included in serialized type name
        }

        [Test]
        public void TestTokenExtension( )
        {
            Assert.IsTrue( EToken.DataToken.IsDataToken() );
            Assert.IsTrue( EToken.True.IsDataToken() );
            Assert.IsTrue( EToken.False.IsDataToken() );
            Assert.IsTrue( EToken.Null.IsDataToken() );

            Assert.IsFalse( EToken.EoF.IsDataToken() );
            Assert.IsFalse( EToken.BoF.IsDataToken() );
            Assert.IsFalse( EToken.Number.IsDataToken() );
            Assert.IsFalse( EToken.StartObject.IsDataToken() );

            Assert.IsTrue( EToken.String.IsStringToken() );
            Assert.IsTrue( EToken.PropertyName.IsStringToken() );

            Assert.IsFalse( EToken.EoF.IsStringToken() );
            Assert.IsFalse( EToken.BoF.IsStringToken() );
            Assert.IsFalse( EToken.Number.IsStringToken() );
            Assert.IsFalse( EToken.StartObject.IsStringToken() );

            Assert.IsTrue( EToken.StartArray.IsContainerToken() );
            Assert.IsTrue( EToken.Container.IsContainerToken() );

            Assert.IsFalse( EToken.EoF.IsContainerToken() );
            Assert.IsFalse( EToken.BoF.IsContainerToken() );
            Assert.IsFalse( EToken.True.IsContainerToken() );
            Assert.IsFalse( EToken.Number.IsContainerToken() );
            Assert.IsFalse( EToken.Alias.IsContainerToken() );

            Assert.IsTrue( EToken.Int16.IsIntegerToken() );
            Assert.IsTrue( EToken.UInt64.IsIntegerToken() );

            Assert.IsFalse( EToken.EoF.IsIntegerToken() );
            Assert.IsFalse( EToken.BoF.IsIntegerToken() );
            Assert.IsFalse( EToken.True.IsIntegerToken() );
            Assert.IsFalse( EToken.Double.IsIntegerToken() );
            Assert.IsFalse( EToken.Alias.IsIntegerToken() );
        }

        [Test]
        public void TestNullSupport( [Values]EBackend backend )
        {
            // Write
            var buffer     = GetBuffer( backend );
            var serializer = GetWriter( backend, buffer );
            serializer.WriteStartObject();
            serializer.WritePropertyName( "NullStringImplicit" );
            serializer.WriteValue( (String)null );
            serializer.WritePropertyName( "NullStringExplicit" );
            serializer.WriteNullValue();
            serializer.WritePropertyName( "NullInt32" );
            serializer.WriteNullValue();
            serializer.WritePropertyName( "NullDouble" );
            serializer.WriteNullValue();
            serializer.WritePropertyName( "NullBool" );
            serializer.WriteNullValue();
            serializer.WritePropertyName( "Array" );
            serializer.WriteStartArray();
            serializer.WriteNullValue();
            serializer.WriteNullValue();
            serializer.WriteNullValue();
            serializer.WriteEndArray();
            serializer.WriteEndObject();

            // Save to file
            SaveToFile( backend, "test", buffer );

            // Log
            LogBuffer( buffer );

            // Read and assert
            var deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            Assert.AreEqual( null, deserializer.ReadPropertyString( "NullStringImplicit" ) );
            Assert.AreEqual( null, deserializer.ReadPropertyString( "NullStringExplicit" ) );
            Assert.AreEqual( 0, deserializer.ReadPropertyInt32( "NullInt32" ) );
            Assert.AreEqual( 0d, deserializer.ReadPropertyDouble( "NullDouble" ) );
            Assert.AreEqual( false, deserializer.ReadPropertyBool( "NullBool" ) );

            deserializer.ReadPropertyName(  );
            deserializer.ReadStartArray();
            while ( deserializer.ReadNextToken() != EToken.EndArray )
            {
                Assert.AreEqual( 0, deserializer.GetInt32Value(  ) );
            }
            deserializer.EnsureEndArray();

            deserializer.ReadEndObject();
        }

        

       
    }
}

public class NoNameSpaceTestClass
{

}
