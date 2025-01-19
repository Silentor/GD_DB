using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Assertions;

namespace GDDB.Serialization
{
    public class BinaryWriter : WriterBase
    {
        public override String Path => "Not implemented";

        private readonly System.IO.BinaryWriter          _writer;
        private readonly Dictionary<String, UInt32> _propertyNameAliases = new ();
        private readonly Dictionary<String, UInt32> _stringValueAliases = new ();
        //public readonly Dictionary<Type, Int32> DebugTypesCounter = new ();
        //public readonly Dictionary<String, Int32> DebugNamespaceCounter = new ();

        public BinaryWriter( Stream writer )
        {
            _writer = new System.IO.BinaryWriter( writer );
        }

        public void SetAlias( UInt32 id, EToken token, String stringValue )
        {
            Assert.IsTrue( id <= 127 );
            switch ( token )
            {
                case EToken.PropertyName:
                    _propertyNameAliases[stringValue] = id;
                    _stringValueAliases.Remove( stringValue );
                    break;
                case EToken.String:
                    _stringValueAliases[stringValue] = id;
                    _propertyNameAliases.Remove( stringValue );
                    break;
                default:
                    throw new Exception( $"Unsupported token {token}" );                
            }
        }

        public override void WriteStartObject( )
        {
            _writer.Write( (byte)EToken.StartObject );
        }

        public override void WriteEndObject( )
        {
            _writer.Write( (byte)EToken.EndObject );
        }

        public override void WriteStartArray( EToken elementType )
        {
            _writer.Write( (byte)EToken.StartBuffer );
        }

        public override void WriteStartArray( )
        {
            _writer.Write( (byte)EToken.StartArray );
        }

        public override void WriteEndArray( )
        {
            _writer.Write( (byte)EToken.EndArray );
        }

        public override WriterBase WritePropertyName(String propertyName )
        {
            if( _propertyNameAliases.TryGetValue( propertyName, out var id ) )
            {
                _writer.Write( (byte)((UInt32)EToken.Alias | id) );
                return this;
            }
                 
            _writer.Write( (byte)EToken.PropertyName );
            _writer.Write( propertyName );
            return this;
        }

        public override void WriteNullValue( )
        {
            _writer.Write( (byte)EToken.Null );
        }

        public override void WriteValue(String value )
        {
            if( value != null )
            {
                if( _stringValueAliases.TryGetValue( value, out var id ) )
                {
                    _writer.Write( (byte)((UInt32)EToken.Alias | id) );
                    return;
                }

                _writer.Write( (byte)EToken.String );
                _writer.Write( value );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Byte value )
        {
            WriteMinimalInteger( value );
        }

        public override void WriteValue(SByte value )
        {
            WriteMinimalInteger( value );
        }

        public override void WriteValue(Int32 value )
        {
            WriteMinimalInteger( value );
        }

        public override void WriteValue(Int64 value )
        {
            WriteMinimalInteger( value );
        }

        public override void WriteValue(UInt64 value )
        {
            WriteMinimalInteger( value );
        }

        public override void WriteValue(Single value )
        {
            if( value != 0f )
            {
                _writer.Write( (byte)EToken.Single );
                _writer.Write( value );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Double value )
        {
            if( value != 0d )
            {
                _writer.Write( (byte)EToken.Double );
                _writer.Write( value );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Boolean value )
        {
            if( value )
                _writer.Write( (byte)EToken.True );
            else
                _writer.Write( (byte)EToken.False );
        }

        public override void WriteValue(Guid value )
        {
            if ( value != Guid.Empty )
            {
                _writer.Write( (byte)EToken.Guid );
                Span<Byte> bytes = stackalloc Byte[16];
                if( value.TryWriteBytes( bytes ) )
                    _writer.Write( bytes );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Enum value )
        {
            var underlyingType = Enum.GetUnderlyingType( value.GetType() );
            if ( underlyingType == typeof(Byte) )
            {
                WriteValue( Convert.ToByte( value ) );
            }
            else if( underlyingType == typeof(SByte) )
            {
                WriteValue( Convert.ToSByte( value ) );
            }
            else if( underlyingType == typeof(Int16) )
            {
                WriteValue( Convert.ToInt16( value ) );
            }
            else if( underlyingType == typeof(UInt16) )
            {
                WriteValue( Convert.ToUInt16( value ) );
            }
            else if( underlyingType == typeof(Int32) )
            {
                WriteValue( Convert.ToInt32( value ) );
            }
            else if( underlyingType == typeof(UInt32) )
            {
                WriteValue( Convert.ToUInt32( value ) );
            }
            else if( underlyingType == typeof(Int64) )
            {
                WriteValue( Convert.ToInt64( value ) );
            }
            else if( underlyingType == typeof(UInt64) )
            {
                WriteValue( Convert.ToUInt64( value ) );
            }
            else
                throw new Exception( $"Unsupported enum type {underlyingType}" );
        }

        public override void WriteValue( Type value )
        {
            WriteStartArray();
            WriteValue( value.Assembly.GetName().Name );
            if( value.Namespace != null )
                WriteValue( value.Namespace );
            else 
                WriteNullValue();
            WriteValue( GetTypeName( value) );
            WriteEndArray();

            //DEBUG
            // if( DebugTypesCounter.TryGetValue( value, out var count ) )
            //     DebugTypesCounter[value] = count + 1;
            // else
            //     DebugTypesCounter[value] = 1;
            //
            // var namespaceName = value.Namespace;
            // if( namespaceName != null )
            //     if( DebugNamespaceCounter.TryGetValue( namespaceName, out count ) )
            //         DebugNamespaceCounter[namespaceName] = count + 1;
            //     else
            //         DebugNamespaceCounter[namespaceName] = 1;

        }

        protected override void WriteRaw(Byte value )
        {
            _writer.Write( value );
        }

        private void WriteMinimalInteger( Int64 value )
        {
            if ( value == 0 )
            {
                _writer.Write( (byte)EToken.Null );
                return;
            }

            if ( value > 0 )
            {
                if ( value <= Byte.MaxValue )
                {
                    _writer.Write( (byte)EToken.UInt8 );
                    _writer.Write( (byte)value );
                }
                else if ( value <= UInt16.MaxValue )
                {
                    _writer.Write( (byte)EToken.UInt16 );
                    _writer.Write( (UInt16)value );
                }
                else if ( value <= UInt32.MaxValue )
                {
                    _writer.Write( (byte)EToken.UInt32 );
                    _writer.Write( (UInt32)value );
                }
                else 
                {
                    _writer.Write( (byte)EToken.Int64 );
                    _writer.Write( value );
                }
            }
            else
            {
                if( value >= SByte.MinValue )
                {
                    _writer.Write( (byte)EToken.Int8 );
                    _writer.Write( (SByte)value );
                }
                else if( value >= Int16.MinValue )
                {
                    _writer.Write( (byte)EToken.Int16 );
                    _writer.Write( (Int16)value );
                }
                else if( value >= Int32.MinValue )
                {
                    _writer.Write( (byte)EToken.Int32 );
                    _writer.Write( (Int32)value );
                }
                else
                {
                    _writer.Write( (byte)EToken.Int64 );
                    _writer.Write( value );
                }
            }
        }

        private void WriteMinimalInteger( UInt64 value )
        {
            if ( value == 0 )
            {
                _writer.Write( (byte)EToken.Null );
                return;
            }

            if ( value <= Byte.MaxValue )
            {
                _writer.Write( (byte)EToken.UInt8 );
                _writer.Write( (byte)value );
            }
            else if ( value <= UInt16.MaxValue )
            {
                _writer.Write( (byte)EToken.UInt16 );
                _writer.Write( (UInt16)value );
            }
            else if ( value <= UInt32.MaxValue )
            {
                _writer.Write( (byte)EToken.UInt32 );
                _writer.Write( (UInt32)value );
            }
            else
            {
                _writer.Write( (byte)EToken.UInt64 );
                _writer.Write( value );
            }
        }

        private static String GetTypeName( Type type )
        {
            String result = String.Empty;
            if ( type.FullName != null )
            {
                if ( type.Namespace != null )
                    result = type.FullName.Remove( 0, type.Namespace.Length + 1 );
                else 
                    result = type.FullName;
            }
            else
                result = type.Name;

            if( type.IsGenericType )
                result = result.Replace( ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "" );   //Remove unneded assembly description

            return result;
        }
    }
}