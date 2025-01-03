using System;
using System.Collections.Generic;
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

        protected IEnumerable<(EToken token, Object value)> EnumerateTokens( ReaderBase reader )
        {
            while ( reader.ReadNextToken() != EToken.EoF )
            {
                if( reader.CurrentToken == EToken.PropertyName )
                {
                    yield return (reader.CurrentToken, reader.GetPropertyName());
                }
                else if( reader.CurrentToken == EToken.String )
                {
                    yield return (reader.CurrentToken, reader.GetStringValue());
                }
                else if ( reader.CurrentToken == EToken.UInt64 )
                {
                    yield return (reader.CurrentToken, reader.GetUInt64Value());
                }
                else if ( reader.CurrentToken == EToken.Guid )
                {
                    yield return (reader.CurrentToken, reader.GetGuidValue());
                }
                else if( reader.CurrentToken.IsIntegerToken() )
                {
                    yield return (reader.CurrentToken, reader.GetIntegerValue());
                }
                else if( reader.CurrentToken == EToken.False || reader.CurrentToken == EToken.True )
                {
                    yield return (reader.CurrentToken, reader.GetBoolValue());
                }
                else if( reader.CurrentToken == EToken.Single || reader.CurrentToken == EToken.Double )
                {
                    yield return (reader.CurrentToken, reader.GetFloatValue());
                }
                else if( reader.CurrentToken == EToken.Null )
                {
                    yield return (reader.CurrentToken, null);
                }
                else 
                {
                    yield return (reader.CurrentToken, null);
                }
            }
        }

        protected Object GetBuffer( EBackend storageType )
        {
            if ( storageType == EBackend.JsonNet  )
            {
                return new StringBuilder();
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
                return GetJsonNetWriter( (StringBuilder)buffer );
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

        protected ReaderBase GetReader( EBackend storageType, Object buffer, Boolean supportMultipleContent  = false )
        {
            if ( storageType == EBackend.JsonNet )
            {
                var bufferStr = ((StringBuilder)buffer).ToString();
                return GetJsonNetReader( bufferStr, supportMultipleContent );
            }
            else if ( storageType == EBackend.Binary )
            {
                var ms = (MemoryStream)buffer;
                var bufferBytes = ms.ToArray();
                return GetBinaryReader( bufferBytes );
            }
            // if ( storageType == EBackend.SimpleText )
            // {
            //     var bufferStr = ((StringWriter)buffer).ToString();
            //     return GetSimpleTextReader( bufferStr );
            // }

            throw new NotImplementedException();
        }

        private WriterBase GetJsonNetWriter( StringBuilder buffer )
        {
            return new JsonNetWriter( buffer, true );
        }

        private WriterBase GetBinaryWriter( MemoryStream buffer )
        {
            return new BinaryWriter( buffer );
        }

        private ReaderBase GetJsonNetReader( String buffer, Boolean supportMultipleContent )
        {
            return new JsonNetReader( buffer, supportMultipleContent );
        }

        private ReaderBase GetBinaryReader( Byte[] buffer )
        {
            return new BinaryReader( buffer );
        }

        protected void SaveToFile( EBackend backend, String fileName, Object buffer )
        {
            if( backend == EBackend.JsonNet )
            {
                var sw = (StringBuilder)buffer;
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
            if ( buffer is StringBuilder sb )
            {
                var json    = sb.ToString();
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

        protected long GetBufferLength( Object buffer )
        {
            if ( buffer is StringBuilder sb )
            {
                var json    = sb.ToString();
                return json.Length;
            }
            else if ( buffer is MemoryStream ms )
            {
                return ms.Length;
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