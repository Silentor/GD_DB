using System;
using System.IO;
using System.Text;
using GDDB.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using BinaryReader = GDDB.Serialization.BinaryReader;
using BinaryWriter = GDDB.Serialization.BinaryWriter;
using Object = System.Object;

namespace GDDB.Tests
{
    public class BaseSerializationTests
    {
        private readonly Encoding _utf8WithoutBom = new UTF8Encoding( false );

        protected Object GetBuffer( EBackend storageType )
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

        protected WriterBase GetWriter( EBackend storageType, Object buffer )
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

        protected ReaderBase GetReader( EBackend storageType, Object buffer )
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

        private ReaderBase GetJsonNetReader( String buffer )
        {
            var reader       = new StringReader( buffer );
            var deserializer = new JsonTextReader( reader );
            return new JsonNetReader( deserializer );
        }

        private ReaderBase GetBinaryReader( Byte[] buffer )
        {
            var reader       = new MemoryStream( buffer );
            var deserializer = new System.IO.BinaryReader( reader );
            return new BinaryReader( deserializer );
        }

        protected void SaveToFile( EBackend backend, String fileName, Object buffer )
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

        protected void LogBuffer( Object buffer )
        {
            if ( buffer is StringWriter sw )
            {
                var json    = sw.ToString();
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

        protected EToken ReduceIntegerToken( EToken token )
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

        // private WriterBase GetSimpleTextWriter( StringWriter buffer )
        // {
        //     return new SimpleTextWriter( buffer );
        // }

        // private ReaderBase GetSimpleTextReader( String buffer )
        // {
        //     var reader       = new StringReader( buffer );
        //     return new SimpleTextReader( reader );
        // }
    }
}