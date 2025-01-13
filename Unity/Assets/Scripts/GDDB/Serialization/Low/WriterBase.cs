using System;

namespace GDDB.Serialization
{
       public abstract class WriterBase
    {
        public abstract String Path { get; }

        public abstract void SetAlias( UInt32 id, EToken token, String stringValue );

        public abstract void WriteStartObject( );

        public abstract void WriteEndObject( );
        public abstract void WriteStartArray( EToken elementType );
        public abstract void WriteStartArray( );
        public abstract void WriteEndArray( );

        public abstract WriterBase WritePropertyName( String propertyName );

        public abstract void WriteNullValue(  );
        public abstract void WriteValue( String value );
        public abstract void WriteValue( Byte value );
        public abstract void WriteValue( SByte value );
        public abstract void WriteValue( Int32 value );
        public abstract void WriteValue( Int64 value );
        public abstract void WriteValue( UInt64 value );
        public abstract void WriteValue( Single value );
        public abstract void WriteValue( Double value );
        public abstract void WriteValue( Boolean value );
        public abstract void WriteValue( Guid value );
        public abstract void WriteValue( Enum value );
        public abstract void WriteValue( Type value, Boolean includeAssembly );

        protected abstract void WriteRaw( Byte value );

        public void Copy( ReaderBase source)
        {
            var    target     = this;

            while ( source.ReadNextToken() != EToken.EoF )
            {
                switch ( source.CurrentToken )
                {
                   case EToken.StartObject:
                        target.WriteStartObject();
                        break;
                    case EToken.EndObject:
                        target.WriteEndObject();
                        break;
                    case EToken.StartArray:
                        target.WriteStartArray();
                        break;
                    case EToken.EndArray:
                        target.WriteEndArray();
                        break;
                    case EToken.PropertyName:
                        target.WritePropertyName( source.GetPropertyName() );
                        break;
                    case EToken.Null:
                        target.WriteNullValue();
                        break;
                    case EToken.String:
                        target.WriteValue( source.GetStringValue() );
                        break;
                    case EToken.UInt8:
                        target.WriteValue( source.GetUInt8Value() );
                        break;
                    case EToken.Int8:
                        target.WriteValue(source.GetInt8Value());
                        break;
                    case EToken.UInt16:
                        target.WriteValue( source.GetUInt16Value() );
                        break;
                    case EToken.Int16:
                        target.WriteValue(source.GetInt16Value());
                        break;
                    case EToken.Int32:
                        target.WriteValue(source.GetInt32Value());
                        break;
                    case EToken.UInt32:
                        target.WriteValue(source.GetUInt32Value());
                        break;
                    case EToken.Int64:
                        target.WriteValue(source.GetUInt64Value());
                        break;
                    case EToken.UInt64:
                        target.WriteValue(source.GetUInt64Value());
                        break;
                    case EToken.Single:
                        target.WriteValue(source.GetSingleValue());
                        break;
                    case EToken.Double:
                        target.WriteValue(source.GetDoubleValue());
                        break;
                    case EToken.True:
                        target.WriteValue(true);
                        break;
                    case EToken.False:
                        target.WriteValue(false);
                        break;
                    case EToken.Guid:
                        target.WriteValue( source.GetGuidValue() );
                        break;
                    default:
                        if ( source.CurrentToken.IsAliasToken() )
                        {
                            var alias = (Byte)source.CurrentToken;
                            target.WriteRaw( alias );
                            break;
                        }
                        throw new NotSupportedException($"Unsupported token: {source.CurrentToken}"); 
                }
            }
        }
    }
}
