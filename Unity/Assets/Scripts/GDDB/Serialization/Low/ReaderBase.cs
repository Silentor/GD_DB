using System;
using Newtonsoft.Json;
using UnityEditor.Profiling;

namespace GDDB.Serialization
{
    public abstract class ReaderBase
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


        public abstract String ReadPropertyName( );
        public abstract String ReadStringValue( );
        public abstract Int32  ReadInt32Value( );
        public abstract Single ReadSingleValue( );

        public abstract void Skip( );

        public Int32 ReadInt32Property(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadInt32Value(  );
        }
    }

    public class JsonNetReader : ReaderBase
    {
        private readonly JsonReader _reader;
        private Boolean _tokenWasRead;

        public JsonNetReader( JsonReader reader )
        {
            _reader = reader;
        }

        public override EToken ReadNextToken( )
        {
            if ( CurrentToken == EToken.EoF )
            {
                _tokenWasRead = true;
                return EToken.EoF;
            }

            var result = _reader.Read();
            if ( !result )
                CurrentToken = EToken.EoF;
            else
                CurrentToken = ConvertToken( _reader.TokenType );

            _tokenWasRead = true;
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

        public override String ReadPropertyName( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.PropertyName )
                    ReadNextToken();
            }

            _reader.EnsureToken( JsonToken.PropertyName );
            var result = (String)_reader.Value;
            _tokenWasRead = false;
            return result;
        }

        public override String ReadStringValue( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.String )
                    ReadNextToken();
            }

            _reader.EnsureToken( JsonToken.String );
            var result = (String)_reader.Value;
            _tokenWasRead = false;
            return result;
        }

        public override Int32 ReadInt32Value( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.Integer )
                    ReadNextToken();
            }

            _reader.EnsureToken( JsonToken.Integer );
            var result = Convert.ToInt32( _reader.Value );
            _tokenWasRead = false;
            return result;
        }

        public override Single ReadSingleValue( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.Float )
                    ReadNextToken();
            }

            _reader.EnsureToken( JsonToken.Float );
            var result = Convert.ToSingle( _reader.Value );
            _tokenWasRead = false;
            return result;
        }

        public override void Skip( )
        {
            _reader.Skip();
            CurrentToken  = ConvertToken( _reader.TokenType );
            _tokenWasRead = false;
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

    public class BinaryReader : ReaderBase
    {
        private readonly System.IO.BinaryReader _reader;
        private          Boolean                _tokenWasRead;
        private          Int32                  _depth;

        public BinaryReader( System.IO.BinaryReader reader )
        {
            _reader = reader;
        }

        public override EToken ReadNextToken( )
        {
            if( CurrentToken == EToken.EoF )
            {
                _tokenWasRead = true;
                return EToken.EoF;
            }

            if ( _tokenWasRead )                    
                SkipValue();

            if( _reader.BaseStream.Position < _reader.BaseStream.Length )
                CurrentToken  = (EToken)_reader.ReadByte();
            else
                CurrentToken = EToken.EoF;

            _tokenWasRead = true;

            if( CurrentToken.IsStartContainer() )
                _depth++;
            else if( CurrentToken.IsEndContainer() )
                _depth--;
            if( _depth < 0 )
                throw new Exception( $"Unexpected end of container" );
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

        public override String ReadPropertyName( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.PropertyName )
                    ReadNextToken();
            }
            EnsureToken( EToken.PropertyName );
            var result = _reader.ReadString();
            _tokenWasRead = false;
            return result;
        }

        public override String ReadStringValue( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.String )
                    ReadNextToken();
            }

            EnsureToken( EToken.String );
            var result = _reader.ReadString();
            _tokenWasRead = false;
            return result;
        }

        public override Int32 ReadInt32Value( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.Int32 )
                    ReadNextToken();
            }

            EnsureToken( EToken.Int32 );
            var result = _reader.ReadInt32();
            _tokenWasRead = false;
            return result;
        }

        public override Single ReadSingleValue( )
        {
            if( !_tokenWasRead )
                ReadNextToken();
            else
            {
                if ( CurrentToken != EToken.Single )
                    ReadNextToken();
            }

            EnsureToken( EToken.Single );
            var result = _reader.ReadSingle();      test float NAN etc
            _tokenWasRead = false;
            return result;
        }

        public override void Skip( )
        {
            if ( CurrentToken == EToken.PropertyName && _tokenWasRead )
                ReadPropertyName();

            if ( ReadNextToken().IsStartContainer() )
            {
                var parentDepth = _depth - 1;
                while ( ReadNextToken() != EToken.EoF && _depth > parentDepth )
                {
                }
            }
            else
            {
                SkipValue();
            }
        }

        private void SkipValue( )
        {
            switch ( CurrentToken )
            {
                case EToken.Single:
                case EToken.Int32:
                case EToken.UInt32:
                    _reader.ReadInt32();
                    break;

                case EToken.Double:
                case EToken.Int64:
                case EToken.UInt64:
                    _reader.ReadInt64();
                    break;

                case EToken.PropertyName:
                case EToken.String:
                    _reader.ReadString();         //todo not optimal, unnecessary allocation
                    break;
                
                case EToken.Byte:
                case EToken.SByte:
                    _reader.ReadByte();
                    break;

                case EToken.Int16:
                case EToken.UInt16:
                    _reader.ReadInt16();
                    break;

                default:
                    return;
            }

            _tokenWasRead = false;
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