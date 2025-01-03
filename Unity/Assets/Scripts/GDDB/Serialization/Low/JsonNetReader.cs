using System;
using System.Numerics;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public class JsonNetReader : ReaderBase
    {
        private readonly JsonReader _reader;

        public JsonNetReader( String buffer, Boolean supportMultipleContent )
        {
            _reader = new JsonTextReader( new System.IO.StringReader( buffer ) );
            _reader.SupportMultipleContent = supportMultipleContent;
        }

        public JsonNetReader( System.IO.TextReader stream )
        {
            _reader = new JsonTextReader( stream );
        }

        public override String Path => _reader.Path;

        public override EToken ReadNextToken( )
        {
            if ( CurrentToken == EToken.EoF )
            {
                return EToken.EoF;
            }

            var result = _reader.Read();
            if ( !result )
                CurrentToken = EToken.EoF;
            else
                CurrentToken = ConvertToken( _reader.TokenType );

            return CurrentToken;
        }

        public override void ReadStartObject( )
        {
            ReadNextToken();
            EnsureToken( JsonToken.StartObject );
        }

        public override void ReadEndObject( )
        {
            ReadNextToken();
            EnsureToken( JsonToken.EndObject );
        }

        public override void ReadStartArray( )
        {
            ReadNextToken();
            EnsureToken( JsonToken.StartArray );
        }

        public override void ReadEndArray( )
        {
            ReadNextToken();
            EnsureToken( JsonToken.EndArray );
        }

        public override String GetPropertyName( )
        {
            EnsureToken( JsonToken.PropertyName );
            return (String)_reader.Value;
        }

        public override String GetStringValue( )
        {
            return (String)_reader.Value;
        }

        public override Int64 GetIntegerValue( )
        {
            return Convert.ToInt64( _reader.Value );
        }

        public override Byte GetUInt8Value( )
        {
            return Convert.ToByte( _reader.Value );
        }

        public override Int32 GetInt32Value( )
        {
            return Convert.ToInt32( _reader.Value );
        }

        public override UInt64 GetUInt64Value( )
        {
            if( _reader.Value is BigInteger bi )
                return (UInt64)bi;

            return Convert.ToUInt64( _reader.Value );
        }

        public override Guid GetGuidValue( )
        {
            if( _reader.TokenType == JsonToken.String )
                return Guid.ParseExact( _reader.Value.ToString(), "D" );
            else if( _reader.TokenType == JsonToken.Null )
                return Guid.Empty;
            else
                throw new Exception( $"Unexpected token {_reader.TokenType}" );
        }

        public override Double GetFloatValue( )
        {
            return Convert.ToDouble( _reader.Value );
        }

        public override Single GetSingleValue( )
        {
            return Convert.ToSingle( _reader.Value );
        }

        public override Double GetDoubleValue( )
        {
            return Convert.ToDouble( _reader.Value );
        }

        public override Boolean GetBoolValue( )
        {
            return Convert.ToBoolean( _reader.Value );
        }

        public override void SkipProperty( )
        {
            _reader.Skip();
            CurrentToken  = ConvertToken( _reader.TokenType );
        }

        private void EnsureToken( JsonToken token )
        {
            if ( _reader.TokenType == token )
            {
                //It's ok
            }
            else
                throw new ReaderTokenException( token.ToString(), this, $"Expected token {token} but got {_reader.TokenType} = {_reader.Value}, path {_reader.Path}" );
        }

        private static EToken ConvertToken( JsonToken token )
        {
            switch ( token )
            {
                case JsonToken.StartObject:
                    return EToken.StartObject;
                case JsonToken.EndObject:
                    return EToken.EndObject;
                case JsonToken.PropertyName:
                    return EToken.PropertyName;
                case JsonToken.String:
                    return EToken.String;
                case JsonToken.Integer:
                    return EToken.Integer;
                case JsonToken.Float:
                    return EToken.Double;
                case JsonToken.StartArray:
                    return EToken.StartArray;
                case JsonToken.EndArray:
                    return EToken.EndArray;
                case JsonToken.Null:
                    return EToken.Null;
                case JsonToken.Boolean:
                    return Convert.ToBoolean( token ) ? EToken.True : EToken.False;

                default:
                    throw new Exception( $"Unexpected token {token}" );
            }
        }
    }
}