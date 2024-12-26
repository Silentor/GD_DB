using System;

namespace GDDB.Serialization
{
    public class BinaryReader : ReaderBase
    {
        private readonly System.IO.BinaryReader _reader;
        private          Int32                  _depth;

        private Int64  _intBuffer;
        private Single _floatBuffer;
        private Double _doubleBuffer;
        private String _stringBuffer;

        public BinaryReader( System.IO.Stream binaryStream )
        {
            _reader = new System.IO.BinaryReader( binaryStream );
        }

        public BinaryReader( System.IO.BinaryReader reader )
        {
            _reader = reader;
        }

        public override String Path { get; }

        public override EToken ReadNextToken( )
        {
            if( CurrentToken == EToken.EoF )
            {
                return EToken.EoF;
            }

            if ( _reader.BaseStream.Position < _reader.BaseStream.Length )
            {
                CurrentToken  = (EToken)_reader.ReadByte();
                StoreValue();

                if( CurrentToken.IsStartContainer() )
                    _depth++;
                else if( CurrentToken.IsEndContainer() )
                    _depth--;
                if( _depth < 0 )
                    throw new Exception( $"Unexpected end of container" );
                return CurrentToken;
            }
            else
            {
                CurrentToken = EToken.EoF;
                return EToken.EoF;
            }
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

        public override String GetPropertyName( )
        {
            EnsureToken( EToken.PropertyName );
            return _stringBuffer;
        }

        public override String GetStringValue( )
        {
            if( CurrentToken == EToken.String )
                return _stringBuffer;
            else if( CurrentToken == EToken.Null )
                return null;
            else
                throw new Exception( $"Expected token {EToken.String} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Int64 GetIntegerValue( )
        {
            if( CurrentToken >= EToken.Int8 && CurrentToken <= EToken.Int64 )
                return _intBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected tokens from {EToken.Int8} to {EToken.Int64} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Byte GetUInt8Value( )
        {
            if( CurrentToken == EToken.UInt8 )
                return (Byte)_intBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.UInt8} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Int32 GetInt32Value( )
        {
            if( CurrentToken == EToken.Int32 || CurrentToken == EToken.Int16 || CurrentToken == EToken.UInt16 || CurrentToken == EToken.Int8 || CurrentToken == EToken.UInt8 )
                return (Int32)_intBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected tokens {EToken.Int32}, {EToken.Int16}, {EToken.UInt16}, {EToken.Int8}, {EToken.UInt8} or {EToken.Null} but got {CurrentToken}" );
        }

        public override UInt64 GetUInt64Value( )
        {
            if( CurrentToken == EToken.UInt64 || CurrentToken == EToken.UInt32 || CurrentToken == EToken.UInt16 || CurrentToken == EToken.UInt8 )
                return unchecked((UInt64)_intBuffer);
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.UInt64} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Double GetFloatValue( )
        {
            if( CurrentToken == EToken.Single )
                return _floatBuffer;
            else if( CurrentToken == EToken.Double )
                return _doubleBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Single} or {EToken.Double} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Single GetSingleValue( )
        {
            if( CurrentToken == EToken.Single )
                return _floatBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Single} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Double GetDoubleValue( )
        {
            if( CurrentToken == EToken.Double )
                return _doubleBuffer;
            if( CurrentToken == EToken.Single )
                return _floatBuffer;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Double} or {EToken.Single} or {EToken.Null} but got {CurrentToken}" );
        }

        public override Boolean GetBoolValue( )
        {
            if( CurrentToken == EToken.True )
                return true;
            else if( CurrentToken == EToken.False || CurrentToken == EToken.Null )
                return false;
            else
                throw new Exception( $"Expected token {EToken.True} or {EToken.False} or {EToken.Null} but got {CurrentToken}" );
        }

        public override void SkipProperty( )
        {
            if ( CurrentToken == EToken.PropertyName )
            {
                if ( ReadNextToken().IsStartContainer() )
                {
                    var parentDepth = _depth - 1;
                    while ( ReadNextToken() != EToken.EoF && _depth > parentDepth )
                    {
                    }
                }
            }
        }

        private void StoreValue( )
        {
            if ( CurrentToken.HasPayload() )
            {
                switch ( CurrentToken )
                {
                    case EToken.Int8:
                        _intBuffer = _reader.ReadSByte();
                        break;

                    case EToken.UInt8:
                        _intBuffer = _reader.ReadByte();
                        break;

                    case EToken.Int16:
                        _intBuffer = _reader.ReadInt16();
                        break;

                    case EToken.UInt16:
                        _intBuffer = _reader.ReadUInt16();
                        break;

                    case EToken.Int32:
                        _intBuffer = _reader.ReadInt32();
                        break;

                    case EToken.UInt32:
                        _intBuffer = _reader.ReadUInt32();
                        break;

                    case EToken.Int64:
                        _intBuffer = _reader.ReadInt64();
                        break;

                    case EToken.UInt64:
                        _intBuffer = unchecked((Int64)_reader.ReadUInt64());
                        break;

                    case EToken.Single:
                        _floatBuffer = _reader.ReadSingle();
                        break;

                    case EToken.Double:
                        _doubleBuffer = _reader.ReadDouble();
                        break;

                    case EToken.String:
                    case EToken.PropertyName:
                        _stringBuffer = _reader.ReadString();
                        break;
                }
            }
        }

        // private void SkipValue( )
        // {
        //     switch ( CurrentToken )
        //     {
        //         case EToken.Single:
        //         case EToken.Int32:
        //         case EToken.UInt32:
        //             _reader.ReadInt32();
        //             break;
        //
        //         case EToken.Double:
        //         case EToken.Int64:
        //         case EToken.UInt64:
        //             _reader.ReadInt64();
        //             break;
        //
        //         case EToken.PropertyName:
        //         case EToken.String:
        //             _reader.ReadString();         //todo not optimal, unnecessary allocation
        //             break;
        //         
        //         case EToken.Byte:
        //         case EToken.SByte:
        //             _reader.ReadByte();
        //             break;
        //
        //         case EToken.Int16:
        //         case EToken.UInt16:
        //             _reader.ReadInt16();
        //             break;
        //
        //         default:
        //             return;
        //     }
        //
        //     _tokenWasRead = false;
        // }

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