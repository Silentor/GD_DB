using System;
using System.IO;
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
        [TestCaseSource(nameof(GetWriterAndReader))]
        public void TestSerializer( Type writerType, Type readerType, Object abstractBuffer )
        {
            // Write
            var serializer   = GetWriter( writerType, abstractBuffer );
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
            SaveToFile( "test", abstractBuffer );

            // Log
            LogBuffer( abstractBuffer );

            // Read and assert
            var deserializer = GetReader( readerType, abstractBuffer );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );

            deserializer.ReadStartObject();
            Assert.AreEqual( deserializer.ReadPropertyName( false ),    "testName" );
            Assert.AreEqual( deserializer.ReadStringValue( false ),     "testValue" );
            Assert.AreEqual( deserializer.ReadInt32Property( "testInt", false ), 42 );

            Assert.AreEqual( deserializer.ReadPropertyName( false ),             "testIntArray" );

            deserializer.ReadStartArray();
            var counter = 0;
            while( deserializer.ReadNextToken() != EToken.EndArray )
            {
                var value = deserializer.ReadInt32Value( true );
                Assert.AreEqual( value, ++counter );
            }
            deserializer.EnsureEndArray();

            Assert.AreEqual( deserializer.ReadPropertyName( false ), "testMiscArray" );
            deserializer.ReadStartArray();
            while( deserializer.ReadNextToken() != EToken.EndArray )
            {
                switch ( deserializer.CurrentToken )
                {
                    case EToken.Integer:
                        var intValue = deserializer.ReadInt32Value( true );
                        Assert.AreEqual( intValue, 42 );
                        break;                    
                    case EToken.String:
                        var stringValue = deserializer.ReadStringValue( true );
                        Assert.AreEqual( stringValue, "testValue" );
                        break;
                    case EToken.StartObject:
                        deserializer.EnsureStartObject();
                        Assert.AreEqual( deserializer.ReadPropertyName( false ), "embeddedObject" );
                        Assert.AreEqual( deserializer.ReadStringValue( false ), "embeddedValue" );
                        deserializer.ReadEndObject();
                        break;
                }
            }
            deserializer.EnsureEndArray();

            deserializer.ReadEndObject();

            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
        }

        [TestCaseSource(nameof(GetWriterAndReaderAndBooleans))]
        public void TestOptionalProperties( Type writerType, Type readerType, Object abstractBuffer, bool includeProp1, bool includeProp2 )
        {
            // Write
            var serializer   = GetWriter( writerType, abstractBuffer );
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
            SaveToFile( "test", abstractBuffer );

            // Log
            LogBuffer( abstractBuffer );

            // Read and assert
            var deserializer = GetReader( readerType, abstractBuffer );
            Assert.AreEqual( deserializer.CurrentToken, EToken.BoF );

            deserializer.ReadStartObject();
            while( deserializer.ReadNextToken() == EToken.PropertyName )
            {
                var propName = deserializer.ReadPropertyName( true  );
                if( propName == "optionalProp1" )
                {
                    Assert.AreEqual( deserializer.ReadStringValue( false ), "testValue" );
                }
                else if( propName == "optionalProp2" )
                {
                    Assert.AreEqual( deserializer.ReadInt32Value( false ), 42 );
                }
            }

            deserializer.EnsureEndObject();

            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
            Assert.AreEqual( deserializer.ReadNextToken(), EToken.EoF );
        }

        private static Object[] GetWriterAndReader( )
        {
            return new []
                   {
                           new Object[] { typeof(JsonNetWriter), typeof(JsonNetReader), new StringWriter() },
                           new Object[] { typeof(BinaryWriter), typeof(BinaryReader), new MemoryStream() },
                   };
        }

        private static Object[] GetWriterAndReaderAndBooleans( )
        {
            return new []
                   {
                           new Object[] { typeof(JsonNetWriter), typeof(JsonNetReader), new StringWriter(), false, false },
                           new Object[] { typeof(BinaryWriter), typeof(BinaryReader), new MemoryStream(), false, false },
                           new Object[] { typeof(JsonNetWriter), typeof(JsonNetReader), new StringWriter(), true, false },
                           new Object[] { typeof(BinaryWriter), typeof(BinaryReader), new MemoryStream(), true, false },
                           new Object[] { typeof(JsonNetWriter), typeof(JsonNetReader), new StringWriter(), false, true },
                           new Object[] { typeof(BinaryWriter), typeof(BinaryReader), new MemoryStream(), false, true },
                           new Object[] { typeof(JsonNetWriter), typeof(JsonNetReader), new StringWriter(), true, true },
                           new Object[] { typeof(BinaryWriter), typeof(BinaryReader), new MemoryStream(), true, true },
                   };
        }

        private WriterBase GetWriter( Type storageType, Object buffer )
        {
            if ( storageType == typeof( JsonNetWriter ) )
            {
                return GetJsonNetWriter( (StringWriter)buffer );
            }

            else if ( storageType == typeof( BinaryWriter ) )
            {
                return GetBinaryWriter( (MemoryStream)buffer );
            }

            throw new NotImplementedException();
        }

        private Reader GetReader( Type storageType, Object buffer )
        {
            if ( storageType == typeof( JsonNetReader ) )
            {
                var bufferStr = ((StringWriter)buffer).ToString();
                return GetJsonNetReader( bufferStr );
            }

            else if ( storageType == typeof( BinaryReader ) )
            {
                var bufferBytes = ((MemoryStream)buffer).ToArray();
                return GetBinaryReader( bufferBytes );
            }

            throw new NotImplementedException();
        }

        private JsonNetWriter GetJsonNetWriter( StringWriter buffer )
        {
            var  jsonWriter     = new JsonTextWriter( buffer );
            jsonWriter.Formatting = Formatting.Indented;
            return new JsonNetWriter( jsonWriter );
        }

        private BinaryWriter GetBinaryWriter( MemoryStream buffer )
        {
            var writer = new System.IO.BinaryWriter( buffer );
            return new BinaryWriter( writer );
        }

        private JsonNetReader GetJsonNetReader( String buffer )
        {
            var reader   = new StringReader( buffer );
            var deserializer = new JsonTextReader( reader );
            return new JsonNetReader( deserializer );
        }

        private BinaryReader GetBinaryReader( Byte[] buffer )
        {
            var reader       = new MemoryStream( buffer );
            var deserializer = new System.IO.BinaryReader( reader );
            return new BinaryReader( deserializer );
        }

        private void SaveToFile( String fileName, Object buffer )
        {
            if ( buffer is StringWriter sw )
            {
                File.WriteAllText( Path.ChangeExtension( fileName, "json" ), sw.ToString() );
            }
            else if ( buffer is MemoryStream ms )
            {
                File.WriteAllBytes( Path.ChangeExtension( fileName, "bin" ), ms.ToArray() );
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void LogBuffer( Object buffer )
        {
            if ( buffer is StringWriter sw )
            {
                Debug.Log( sw.ToString() );
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
    }



}