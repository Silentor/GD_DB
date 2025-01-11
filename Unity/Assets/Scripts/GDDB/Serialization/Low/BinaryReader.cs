using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = System.Object;

namespace GDDB.Serialization
{
    public sealed class BinaryReader : ReaderBase
    {
        private readonly System.IO.BinaryReader _reader;
        private          Int32                  _depth;

        private readonly Stack<Container> _path = new ( 16 );

        private Int64  _intBuffer;
        private Guid  _guidBuffer;
        private Single _floatBuffer;
        private Double _doubleBuffer;
        private String _stringBuffer;
        private String  _assemblyNameBuffer;   //for Type token

        private readonly SortedDictionary<UInt32, (EToken token, String value)> _aliases = new ();

        private readonly CustomSampler _readNextTokenSampler   = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(ReadNextToken)}" );
        private readonly CustomSampler _getGuidValueSampler    = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetGuidValue)}" );
        private readonly CustomSampler _getStringValueSampler  = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetStringValue)}" );
        private readonly CustomSampler _getIntegerValueSampler = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetIntegerValue)}" );
        private readonly CustomSampler _getSingleValueSampler  = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetSingleValue)}" );
        private readonly CustomSampler _getBoolValueSampler    = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetBoolValue)}" );
        private readonly CustomSampler _getEnumValueSampler    = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetEnumValue)}" );
        private readonly CustomSampler _getTypeValueSampler    = CustomSampler.Create( $"{nameof(ReaderBase)}.{nameof(GetTypeValue)}" );

        private readonly CustomSampler _storeValueSampler    = CustomSampler.Create( $"{nameof(BinaryReader)}.{nameof(StoreValue)}" );
        private readonly CustomSampler _readTypeValueSampler = CustomSampler.Create( $"{nameof(BinaryReader)}.StoreTypePayload" );
        private readonly CustomSampler _processPathSampler   = CustomSampler.Create( $"{nameof(BinaryReader)}.ProcessPath" );

        public BinaryReader( System.IO.Stream binaryStream )
        {
            _reader = new System.IO.BinaryReader( binaryStream );
        }

        public BinaryReader( Byte[] buffer ) : this( new System.IO.MemoryStream( buffer ) )
        {
        }

        public override String Path
        {
            get
            {
                if( _path.Count == 0 )
                    return String.Empty;
                else
                {
                    var sb = new System.Text.StringBuilder();
                    foreach ( var container in _path.Reverse() )
                    {
                        if( container.Token == EToken.StartObject )
                        {
                            sb.Append( "." );
                            if ( container.PropertyName != null )                                
                                sb.Append( container.PropertyName );
                        }
                        else if( container.Token == EToken.StartArray )
                        {
                            sb.Append( "[" );
                            if( container.ElementIndex >= 0 )
                                sb.Append( container.ElementIndex );
                            sb.Append( "]" );
                        }
                    }
                    return sb.ToString();
                }
            }
        }

        public override void SetAlias( UInt32 id, EToken token, String stringValue )
        {
            Assert.IsTrue( id <= 127 );
            _aliases[id] = (token, stringValue);
        }

        public override EToken ReadNextToken( )
        {
            if( CurrentToken == EToken.EoF )
            {
                return EToken.EoF;
            }

            _readNextTokenSampler.Begin();

            if ( _reader.BaseStream.Position < _reader.BaseStream.Length )
            {
                var currentToken = (EToken)_reader.ReadByte();
                currentToken = StoreValue( currentToken );
                CurrentToken = currentToken;
                
                _processPathSampler.Begin();
                if ( _path.TryPeek( out var arr ) && arr.Token == EToken.StartArray )
                {
                    var lastObject = _path.Pop();
                    lastObject.ElementIndex += 1;                   //Count array elements for path
                    _path.Push( lastObject );
                }

                if ( currentToken == EToken.StartObject )
                {
                    _path.Push( new Container { Token = EToken.StartObject } );
                    _depth++;
                }
                else if ( currentToken == EToken.StartArray )
                {
                    _path.Push( new Container { Token = EToken.StartArray, ElementIndex = -1} );
                    _depth++;
                }
                else if ( currentToken == EToken.EndObject )
                {
                    _depth--;
                    var lastObject = _path.Pop();
                    Assert.IsTrue( lastObject.Token == EToken.StartObject );
                }
                else if( currentToken == EToken.EndArray )
                {
                    _depth--;
                    var lastArray = _path.Pop();
                    Assert.IsTrue( lastArray.Token == EToken.StartArray );
                }
                else if( currentToken == EToken.PropertyName )
                {
                    var lastObject = _path.Pop();
                    Assert.IsTrue( lastObject.Token == EToken.StartObject );
                    lastObject.PropertyName = _stringBuffer;
                    _path.Push( lastObject );
                }
                _processPathSampler.End();

                if( _depth < 0 )
                    throw new Exception( $"Unexpected end of container" );

                _readNextTokenSampler.End();
                return CurrentToken;
            }
            else
            {
                CurrentToken = EToken.EoF;
                _readNextTokenSampler.End();
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
            _getStringValueSampler.Begin();
            if ( CurrentToken == EToken.String )
            {
                _getStringValueSampler.End();
                return _stringBuffer;
            }
            else if ( CurrentToken == EToken.Null )
            {
                _getStringValueSampler.End();
                return null;
            }
            else
            {
                _getStringValueSampler.End();
                throw new Exception( $"Expected token {EToken.String} or {EToken.Null} but got {CurrentToken}" );
            }
        }

        public override Int64 GetIntegerValue( )
        {
            _getIntegerValueSampler.Begin();
            if ( CurrentToken >= EToken.Int8 && CurrentToken <= EToken.Int64 )
            {
                _getIntegerValueSampler.End();
                return _intBuffer;
            }
            else if ( CurrentToken == EToken.Null )
            {
                _getIntegerValueSampler.End();
                return 0;
            }
            else
            {
                _getIntegerValueSampler.End();
                throw new Exception( $"Expected tokens from {EToken.Int8} to {EToken.Int64} or {EToken.Null} but got {CurrentToken}" );
            }
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

        public override Guid GetGuidValue( )
        {
            _getGuidValueSampler.Begin();
            if ( CurrentToken == EToken.Guid )
            {
                _getGuidValueSampler.End();
                return _guidBuffer;
            }
            else if ( CurrentToken == EToken.Null )
            {
                _getGuidValueSampler.End();
                return Guid.Empty;
            }
            else
            {
                _getGuidValueSampler.End();
                throw new Exception( $"Expected token {EToken.Guid} or {EToken.Null} but got {CurrentToken}" );
            }
        }

        public override Object GetEnumValue(Type enumType )
        {
            _getEnumValueSampler.Begin();
            if ( CurrentToken.IsEnumToken() )
            {
                var underlyingType = Enum.GetUnderlyingType( enumType );
                if ( underlyingType == typeof(UInt64) )
                {
                    _getEnumValueSampler.End();
                    return Enum.ToObject( enumType, unchecked((UInt64)_intBuffer) );
                }
                else
                {
                    _getEnumValueSampler.End();
                    return  Enum.ToObject( enumType, _intBuffer);
                }
            }
            else if ( CurrentToken == EToken.Null )
            {
                _getEnumValueSampler.End();
                return Enum.ToObject( enumType, 0 );
            }
            else
            {   _getEnumValueSampler.End();
                throw new Exception( $"Expected Enum tokens or Null but got {CurrentToken}" );
            }
        }

        public override Type GetTypeValue(Assembly defaultAssembly )
        {
            _getTypeValueSampler.Begin();

            if ( CurrentToken == EToken.Type )
            {
                if ( _assemblyNameBuffer != null )
                    defaultAssembly = Assembly.Load( _assemblyNameBuffer );

                if ( defaultAssembly != null )
                {
                    var result = defaultAssembly.GetType( _stringBuffer );
                    _getTypeValueSampler.End();
                    return result;
                }
                else
                {
                    var result = Type.GetType( _stringBuffer );
                    _getTypeValueSampler.End();
                    return result;
                }
            }
            else if( CurrentToken == EToken.String )
            {
                var result = Type.GetType( _stringBuffer );
                _getTypeValueSampler.End();
                return result;
            }
            else if( CurrentToken == EToken.Null )
            {
                _getTypeValueSampler.End();
                return null;
            }
            else
            {
                _getTypeValueSampler.End();
                throw new Exception( $"Expected token {EToken.Type}, {EToken.String} or {EToken.Null} but got {CurrentToken}" );
            }

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
            _getSingleValueSampler.Begin();
            if ( CurrentToken == EToken.Single )
            {
                _getSingleValueSampler.End();
                return _floatBuffer;
            }
            else if ( CurrentToken == EToken.Null )
            {
                _getSingleValueSampler.End();
                return 0;
            }
            else
            {
                _getSingleValueSampler.End();
                throw new Exception( $"Expected token {EToken.Single} or {EToken.Null} but got {CurrentToken}" );
            }
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
            _getBoolValueSampler.Begin();
            if ( CurrentToken == EToken.True )
            {
                _getBoolValueSampler.End();
                return true;
            }
            else if ( CurrentToken == EToken.False || CurrentToken == EToken.Null )
            {
                _getBoolValueSampler.End();
                return false;
            }
            else
            {
                _getBoolValueSampler.End();
                throw new Exception( $"Expected token {EToken.True} or {EToken.False} or {EToken.Null} but got {CurrentToken}" );
            }
        }

        public override void SkipProperty( )
        {
            if ( CurrentToken == EToken.PropertyName || CurrentToken.IsAliasToken() )
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

        private EToken StoreValue( EToken token )
        {
            _storeValueSampler.Begin();

            if ( token.IsAliasToken() )
            {
                var id = (UInt32)token & 0x7F;
                if( _aliases.TryGetValue( id, out var alias ) )             //Replace alias with property name
                {
                    token = alias.token;
                    _stringBuffer = alias.value;
                }
                else
                {
                    Debug.LogWarning( $"[{nameof(BinaryReader)}]-[{nameof(StoreValue)}] Property name alias id {id} is not defined" );
                    //SkipProperty();
                }

                _storeValueSampler.End();
                return token;
            }

            switch ( token )                           //Read payload
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

                case EToken.Guid:
                    Span<Byte> buffer = stackalloc Byte[16];
                    Assert.IsTrue( _reader.Read( buffer ) == 16 );
                    _guidBuffer = new Guid( buffer );
                    break;

                case EToken.Enum1:
                    _intBuffer = _reader.ReadByte();
                    break;
                case EToken.Enum2:
                    _intBuffer = _reader.ReadUInt16();
                    break;
                case EToken.Enum4:
                    _intBuffer = _reader.ReadUInt32();
                    break;
                case EToken.Enum8:
                    _intBuffer = _reader.ReadInt64();
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

                case EToken.Type:
                    ReadTypePayload();
                    break;
            }

            _storeValueSampler.End();

            return token;

            void ReadTypePayload( )
            {
                _readTypeValueSampler.Begin();

                ReadStartArray();

                var assemblyName = ReadStringValue();
                var @namespace   = ReadStringValue();
                var typeName     = ReadStringValue();

                ReadEndArray();

                _stringBuffer = @namespace != null ? $"{@namespace}.{typeName}" : typeName;
                _assemblyNameBuffer = assemblyName;

                _readTypeValueSampler.End();
            }
        }

        private struct Container
        {
            public EToken Token;

            //For objects
            public String PropertyName;

            //For arrays
            public Int32 ElementIndex;
        }
    }
}