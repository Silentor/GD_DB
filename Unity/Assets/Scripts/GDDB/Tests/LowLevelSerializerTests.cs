using System;
using System.IO;
using System.Security;
using System.Text;
using GDDB.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using BinaryReader = GDDB.Serialization.BinaryReader;
using BinaryWriter = GDDB.Serialization.BinaryWriter;
using Object = System.Object;

namespace GDDB.Tests
{
    public class LowLevelSerializerTests
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
                    case EToken.Integer:
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
            Assert.IsTrue( deserializer.ReadNextToken().IsIntegerToken() );
            var actualValue = deserializer.GetIntegerValue( );
            Assert.AreEqual( value, actualValue );
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
            Assert.AreEqual( value, actualValue );

            // Read and assert via generic read
            deserializer = GetReader( backend, buffer );
            deserializer.ReadStartObject();
            deserializer.ReadPropertyName();
            Assert.IsTrue( deserializer.ReadNextToken().IsIntegerToken() );
            var actualValue2 = deserializer.GetIntegerValue( );
            Assert.AreEqual( value, actualValue2 );
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
            Assert.IsTrue( deserializer.ReadNextToken().IsIntegerToken() );
            var actualValue = deserializer.GetUInt64Value( );
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
        public void TestTokenExtension( )
        {
            Assert.IsTrue( EToken.DataToken.IsDataToken() );
            Assert.IsTrue( EToken.True.IsDataToken() );
            Assert.IsTrue( EToken.False.IsDataToken() );
            Assert.IsTrue( EToken.Null.IsDataToken() );

            Assert.IsFalse( EToken.EoF.IsDataToken() );
            Assert.IsFalse( EToken.BoF.IsDataToken() );
            Assert.IsFalse( EToken.Integer.IsDataToken() );
            Assert.IsFalse( EToken.StartObject.IsDataToken() );

            Assert.IsTrue( EToken.String.IsStringToken() );
            Assert.IsTrue( EToken.PropertyName.IsStringToken() );

            Assert.IsFalse( EToken.EoF.IsStringToken() );
            Assert.IsFalse( EToken.BoF.IsStringToken() );
            Assert.IsFalse( EToken.Integer.IsStringToken() );
            Assert.IsFalse( EToken.StartObject.IsStringToken() );

            Assert.IsTrue( EToken.StartArray.IsContainerToken() );
            Assert.IsTrue( EToken.Container.IsContainerToken() );

            Assert.IsFalse( EToken.EoF.IsContainerToken() );
            Assert.IsFalse( EToken.BoF.IsContainerToken() );
            Assert.IsFalse( EToken.True.IsContainerToken() );
            Assert.IsFalse( EToken.Integer.IsContainerToken() );
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

        private Object GetBuffer( EBackend storageType )
        {
            if ( storageType == EBackend.JsonNet 
                 //|| storageType == EBackend.SimpleText 
                 )
            {
                return new StringWriter();
            }
            else if ( storageType == EBackend.Binary )
            {
                return new MemoryStream();
            }

            throw new NotImplementedException();
        }

        private WriterBase GetWriter( EBackend storageType, Object buffer )
        {
            if ( storageType == EBackend.JsonNet )
            {
                return GetJsonNetWriter( (StringWriter)buffer );
            }
            else if ( storageType == EBackend.Binary )
            {
                return GetBinaryWriter( (MemoryStream)buffer );
            }
            // else if ( storageType == EBackend.SimpleText )
            // {
            //     return GetSimpleTextWriter( (StringWriter)buffer );
            // }

            throw new NotImplementedException();
        }

        private ReaderBase GetReader( EBackend storageType, Object buffer )
        {
            if ( storageType == EBackend.JsonNet )
            {
                var bufferStr = ((StringWriter)buffer).ToString();
                return GetJsonNetReader( bufferStr );
            }
            else if ( storageType == EBackend.Binary )
            {
                var bufferBytes = ((MemoryStream)buffer).ToArray();
                return GetBinaryReader( bufferBytes );
            }
            // if ( storageType == EBackend.SimpleText )
            // {
            //     var bufferStr = ((StringWriter)buffer).ToString();
            //     return GetSimpleTextReader( bufferStr );
            // }

            throw new NotImplementedException();
        }

        private WriterBase GetJsonNetWriter( StringWriter buffer )
        {
            var  jsonWriter     = new JsonTextWriter( buffer );
            jsonWriter.Formatting = Formatting.Indented;
            return new JsonNetWriter( jsonWriter );
        }

        private WriterBase GetBinaryWriter( MemoryStream buffer )
        {
            var writer = new System.IO.BinaryWriter( buffer );
            return new BinaryWriter( writer );
        }

        // private WriterBase GetSimpleTextWriter( StringWriter buffer )
        // {
        //     return new SimpleTextWriter( buffer );
        // }

        private ReaderBase GetJsonNetReader( String buffer )
        {
            var reader   = new StringReader( buffer );
            var deserializer = new JsonTextReader( reader );
            return new JsonNetReader( deserializer );
        }

        private ReaderBase GetBinaryReader( Byte[] buffer )
        {
            var reader       = new MemoryStream( buffer );
            var deserializer = new System.IO.BinaryReader( reader );
            return new BinaryReader( deserializer );
        }

        // private ReaderBase GetSimpleTextReader( String buffer )
        // {
        //     var reader       = new StringReader( buffer );
        //     return new SimpleTextReader( reader );
        // }

        private void SaveToFile( EBackend backend, String fileName, Object buffer )
        {
            if( backend == EBackend.JsonNet )
            {
                var sw = (StringWriter)buffer;
                File.WriteAllText( Path.ChangeExtension( fileName, "json" ), sw.ToString() );
            }
            else if( backend == EBackend.Binary )
            {
                var ms = (MemoryStream)buffer;
                File.WriteAllBytes( Path.ChangeExtension( fileName, "bin" ), ms.ToArray() );
            }
            // else if( backend == EBackend.SimpleText )
            // {
            //     var sw = (StringWriter)buffer;
            //     File.WriteAllText( Path.ChangeExtension( fileName, "txt" ), sw.ToString() );
            // }
            else
            {
                throw new NotImplementedException();
            }
        }

        private readonly Encoding _utf8WithoutBom = new UTF8Encoding( false );

        private void LogBuffer( Object buffer )
        {
            if ( buffer is StringWriter sw )
            {
                var json = sw.ToString();
                var toBytes = _utf8WithoutBom.GetBytes( json ); 
                Debug.Log( $"Length {toBytes.Length}, value\n\r{json}" );
            }
            else if ( buffer is MemoryStream ms )
            {
                Debug.Log( $"Length {ms.Length}, value '{BytesToString( ms.ToArray() )}'" );
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private String BytesToString( byte[] bytes )
        {
            var sb = new StringBuilder( bytes.Length * 2 );
            foreach ( var b in bytes )
            {
                var c           = Convert.ToChar( b );
                var isPrintable = ! Char.IsControl(c) || Char.IsWhiteSpace(c);
                if( isPrintable )
                    sb.Append( c );
                else
                    sb.Append( '.' );
            }

            return sb.ToString();
        }

        private EToken ReduceIntegerToken( EToken token )
        {
            if ( token.IsIntegerToken() )
                return EToken.Integer;
            return token;
        }

        public enum EBackend
        {
            JsonNet,
            Binary,
            //SimpleText
        }
    }



}