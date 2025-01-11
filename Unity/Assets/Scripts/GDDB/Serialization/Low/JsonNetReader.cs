using System;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public sealed class JsonNetReader : ReaderBase
    {
        private readonly JsonReader    _reader;

        private readonly CustomSampler _readNextTokenSampler   = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(ReadNextToken)}" );
        private readonly CustomSampler _getGuidValueSampler    = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetGuidValue)}" );
        private readonly CustomSampler _getStringValueSampler  = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetStringValue)}" );
        private readonly CustomSampler _getIntegerValueSampler = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetIntegerValue)}" );
        private readonly CustomSampler _getSingleValueSampler = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetSingleValue)}" );
        private readonly CustomSampler _getBoolValueSampler = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetBoolValue)}" );
        private readonly CustomSampler _getEnumValueSampler = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetEnumValue)}" );


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

        public override void SetAlias( UInt32 id, EToken token, String stringValue )
        {
            //Not supported
        }

        public override EToken ReadNextToken( )
        {
            if ( CurrentToken == EToken.EoF )
            {
                return EToken.EoF;
            }

            _readNextTokenSampler.Begin();
            var result = _reader.Read();
            if ( !result )
                CurrentToken = EToken.EoF;
            else
                CurrentToken = ConvertToken( _reader.TokenType );

            _readNextTokenSampler.End();
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
            _getStringValueSampler.Begin();
            var value = (String)_reader.Value;
            _getStringValueSampler.End();
            return value;
        }

        public override Int64 GetIntegerValue( )
        {
            _getIntegerValueSampler.Begin();
            var value = Convert.ToInt64( _reader.Value );
            _getIntegerValueSampler.End();
            return value;
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
            _getGuidValueSampler.Begin();
            if ( _reader.TokenType == JsonToken.String )
            {
                _getGuidValueSampler.End();
                return Guid.ParseExact( _reader.Value.ToString(), "D" );
            }
            else if ( _reader.TokenType == JsonToken.Null )
            {
                _getGuidValueSampler.End();
                return Guid.Empty;
            }
            else
            {
                _getGuidValueSampler.End();
                throw new Exception( $"Unexpected token {_reader.TokenType}" );
            }
        }

        public override Object GetEnumValue(Type enumType )
        {
            _getEnumValueSampler.Begin();
            if ( _reader.Value is String str )
            {
                _getEnumValueSampler.End();
                return Enum.Parse( enumType, str );
            }
            else if ( _reader.Value is Int64 i64 )
            {
                _getEnumValueSampler.End();
                return Enum.ToObject( enumType, i64 );
            }
            else if( CurrentToken == EToken.Null )
            {
                _getEnumValueSampler.End();
                return Enum.ToObject( enumType, 0 );
            }
            else
            {
                _getEnumValueSampler.End();
                throw new Exception( $"Unexpected token {_reader.TokenType}" );
            }
        }

        public override Type ReadTypeValue(Assembly defaultAssembly )
        {
            var typeString = ReadStringValue();
            var result = Type.GetType( typeString );
            if( result != null )
                return result;

            if( defaultAssembly != null )
                return defaultAssembly.GetType( typeString );

            return null;
        }

        public override Double GetFloatValue( )
        {
            return Convert.ToDouble( _reader.Value );
        }

        public override Single GetSingleValue( )
        {
            _getSingleValueSampler.Begin();
            var value = Convert.ToSingle( _reader.Value );
            _getSingleValueSampler.End();
            return value;
        }

        public override Double GetDoubleValue( )
        {
            return Convert.ToDouble( _reader.Value );
        }

        public override Boolean GetBoolValue( )
        {
            _getBoolValueSampler.Begin();
            var value = Convert.ToBoolean( _reader.Value );
            _getBoolValueSampler.End();
            return value;
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
                    return EToken.Number;
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