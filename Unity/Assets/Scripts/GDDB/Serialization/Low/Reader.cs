using System;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public abstract class Reader
    {
        public EToken CurrentToken { get; protected set; }

        public abstract EToken ReadNextToken( );

        public abstract void ReadStartObject( );
        public abstract void ReadEndObject( );

        public abstract void ReadStartArray( );
        public abstract void ReadEndArray( );

        public abstract void EnsureStartObject( );
        public abstract void EnsureEndObject( );

        public abstract void EnsureEndArray( );


        public abstract String ReadPropertyName(Boolean skipToken );
        public abstract String ReadStringValue(Boolean  skipToken );
        public abstract Int32  ReadInt32Value(Boolean   skipToken );

        public Int32 ReadInt32Property( String propertyName, Boolean skipToken )
        {
            var actualPropertyName = ReadPropertyName( skipToken );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadInt32Value( false );
        }
    }

    public class JsonNetReader : Reader
    {
        private readonly JsonReader _reader;

        public JsonNetReader( JsonReader reader )
        {
            _reader = reader;
        }

        public override EToken ReadNextToken( )
        {
            if ( CurrentToken == EToken.EoF )
                return EToken.EoF;

            var result = _reader.Read();
            if ( !result )
                CurrentToken = EToken.EoF;
            else
                CurrentToken = ConvertToken( _reader.TokenType );

            return CurrentToken;
        }

        public override void ReadStartObject( )
        {
            _reader.Read();
            EnsureToken( JsonToken.StartObject );
        }

        public override void ReadEndObject( )
        {
            _reader.Read();
            EnsureToken( JsonToken.EndObject );
        }

        public override void ReadStartArray( )
        {
            _reader.Read();
            EnsureToken( JsonToken.StartArray );
        }

        public override void ReadEndArray( )
        {
            _reader.Read();
            EnsureToken( JsonToken.EndArray );
        }

        public override void EnsureStartObject( )
        {
            EnsureToken( JsonToken.StartObject );
        }

        public override void EnsureEndObject( )
        {
            EnsureToken( JsonToken.EndObject );
        }

        public override void EnsureEndArray( )
        {
            EnsureToken( JsonToken.EndArray );
        }

        public override String ReadPropertyName(Boolean skipToken )
        {
            if(skipToken)
                _reader.EnsureToken( JsonToken.PropertyName );
            else
                _reader.EnsureNextToken( JsonToken.PropertyName );
            var result = (String)_reader.Value;
            return result;
        }

        public override String ReadStringValue(Boolean skipToken )
        {
            if( skipToken )
                _reader.EnsureToken( JsonToken.String );
            else
                _reader.EnsureNextToken( JsonToken.String );
            var result = (String)_reader.Value;
            return result;
        }

        public override Int32 ReadInt32Value(Boolean skipToken )
        {
            if( skipToken )
                _reader.EnsureToken( JsonToken.Integer );
            else
                _reader.EnsureNextToken( JsonToken.Integer );
            var result = Convert.ToInt32( _reader.Value );
            return result;
        }

        private void EnsureToken( JsonToken token )
        {
            if ( _reader.TokenType == token )
            {
                //It's ok
            }
            else
                throw new JsonTokenException( token.ToString(), _reader, $"Expected token {token} but got {_reader.TokenType} = {_reader.Value}" );
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
                case JsonToken.StartArray:
                    return EToken.StartArray;
                case JsonToken.EndArray:
                    return EToken.EndArray;

                default:
                    throw new Exception( $"Unexpected token {token}" );
            }
        }
    }

    public class BinaryReader : Reader
    {
        private readonly System.IO.BinaryReader _reader;

        public BinaryReader( System.IO.BinaryReader reader )
        {
            _reader = reader;
        }

        public override EToken ReadNextToken( )
        {
            if ( _reader.BaseStream.Position >= _reader.BaseStream.Length )
                CurrentToken = EToken.EoF;
            else
                CurrentToken = (EToken)_reader.ReadByte();
            return CurrentToken;
        }

        public override void ReadStartObject( )
        {
            ReadNextToken();
            EnsureToken( EToken.StartObject );
        }

        public override void ReadEndObject( )
        {
            ReadNextToken();
            EnsureToken( EToken.EndObject );
        }

        public override void ReadStartArray( )
        {
            ReadNextToken();
            if ( CurrentToken == EToken.StartArray || CurrentToken == EToken.StartBuffer )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected token {EToken.StartArray} or {EToken.StartBuffer} but got {CurrentToken}" );
        }

        public override void ReadEndArray( )
        {
            ReadNextToken();
            EnsureToken( EToken.EndArray );
        }

        public override void EnsureStartObject( )
        {
            EnsureToken( EToken.StartObject );
        }

        public override void EnsureEndObject( )
        {
            EnsureToken( EToken.EndObject );
        }

        public override void EnsureEndArray( )
        {
            EnsureToken( EToken.EndArray );
        }

        public override String ReadPropertyName(Boolean skipToken )
        {
            if( !skipToken )
                ReadNextToken();
            EnsureToken( EToken.PropertyName );
            var result = _reader.ReadString();
            return result;
        }

        public override String ReadStringValue(Boolean skipToken )
        {
            if( !skipToken )
                ReadNextToken();
            EnsureToken( EToken.String );
            var result = _reader.ReadString();
            return result;
        }

        public override Int32 ReadInt32Value(Boolean skipToken )
        {
            if ( !skipToken )                
                ReadNextToken();
            EnsureToken( EToken.Int32 );
            var result = _reader.ReadInt32();
            return result;
        }

        private void EnsureToken( EToken token )
        {
            if ( CurrentToken == token )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected token {token} but got {CurrentToken}" );
        }
    }
}