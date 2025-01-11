using System;
using System.Reflection;

namespace GDDB.Serialization
{
    public abstract class ReaderBase
    {
        public EToken CurrentToken { get; protected set; }

        public abstract String Path { get; }

        public abstract void SetAlias( UInt32 id, EToken token, String stringValue );

        public abstract EToken ReadNextToken( );

        public abstract void ReadStartObject( );
        public abstract void ReadEndObject( );

        public abstract void ReadStartArray( );
        public abstract void ReadEndArray( );
        
        public abstract String GetPropertyName( );
        public abstract String GetStringValue( );


        /// <summary>
        /// Get any integer value up to int64
        /// </summary>
        /// <returns></returns>
        public abstract Int64 GetIntegerValue( );
        public abstract Byte   GetUInt8Value( );
        public abstract Int32  GetInt32Value( );
        public abstract UInt64 GetUInt64Value( );
        public abstract Guid   GetGuidValue( );
        public abstract Object GetEnumValue( Type enumType );

        //Its a complex type, so we read it as a whole instead of get from buffer
        public abstract Type ReadTypeValue( Assembly defaultAssembly );
          
        /// <summary>
        /// Get any float value (single or double)
        /// </summary>
        /// <returns></returns>
        public abstract Double GetFloatValue( );
        public abstract Single GetSingleValue( );
        public abstract Double GetDoubleValue( );
        

        public abstract Boolean GetBoolValue( );


        public abstract void SkipProperty( );

        public void EnsureToken( EToken token )
        {
            if ( CurrentToken == token )
            {
                //It's ok
            }
            else
                throw new ReaderTokenException( token.ToString(), this, $"[{nameof(ReaderBase)}]-[{nameof(EnsureToken)}] Expected token {token} but got {CurrentToken} at {Path}" );
        }

        public void EnsureStartObject( )
        {
            EnsureToken( EToken.StartObject );
        }
        public void SeekStartObject( )
        {
            while( CurrentToken != EToken.StartObject )
            {
                ReadNextToken();
                if ( CurrentToken == EToken.EoF )
                    return;
            }
        }
        public void EnsureEndObject( )
        {
            EnsureToken( EToken.EndObject );
        }
        public void EnsureStartArray( )
        {
            EnsureToken( EToken.StartArray );
        }
        public  void EnsureEndArray( )
        {
            EnsureToken( EToken.EndArray );
        }

        public void EnsurePropertyName( String propertyName )
        {
            EnsureToken( EToken.PropertyName );
            if( GetPropertyName() != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {GetPropertyName()} at {Path}" );
        }

        public String ReadPropertyName( )
        {
            var propertyToken = ReadNextToken();
            if( propertyToken == EToken.PropertyName )
                return GetPropertyName();
            else
                throw new Exception( $"Expected property name but got {propertyToken}" );
        }

        public void ReadPropertyName( String propertyName )
        {
            var actualPropertyName = ReadPropertyName();
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
        }

        public EToken SeekPropertyName( String propertyName )
        {
            while( CurrentToken != EToken.PropertyName || GetPropertyName() != propertyName )
            {
                ReadNextToken();
                if ( CurrentToken == EToken.EoF )
                    return EToken.EoF;
            }

            return EToken.PropertyName;
        }

        public Boolean TryReadPropertyName( out String propertyName )
        {
            var propertyToken = ReadNextToken();
            if( propertyToken == EToken.PropertyName )
            {
                propertyName = GetPropertyName();
                return true;
            }
            else
            {
                propertyName = null;
                return false;
            }
        }

        public Byte  ReadUInt8Value( )
        {
            ReadNextToken();
            return GetUInt8Value();
        }

        public Int32  ReadInt32Value( )
        {
            ReadNextToken();
            return GetInt32Value();
        }

        public UInt64  ReadUInt64Value( )
        {
            ReadNextToken();
            return GetUInt64Value();
        }

        public Int64  ReadIntegerValue( )
        {
            ReadNextToken();
            return GetIntegerValue();
        }

        public Guid  ReadGuidValue( )
        {
            ReadNextToken();
            return GetGuidValue();
        }

        public Object  ReadEnumValue( Type enumType )
        {
            ReadNextToken();
            return GetEnumValue( enumType );
        }

        public Single  ReadSingleValue( )
        {
            ReadNextToken();
            return GetSingleValue();
        }

        public Double  ReadDoubleValue( )
        {
            ReadNextToken();
            return GetDoubleValue();
        }

        public Boolean  ReadBoolValue( )
        {
            ReadNextToken();
            return GetBoolValue();
        }

        public String  ReadStringValue( )
        {
            ReadNextToken();
            return GetStringValue();
        }

        public Int32 ReadPropertyInt32(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName == propertyName ) return ReadInt32Value(  );
            throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
        }

        public Int64 ReadPropertyInteger(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName == propertyName ) return ReadIntegerValue(  );
            throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
        }


        public Single ReadPropertySingle(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadSingleValue(  );
        }

        public String ReadPropertyString(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadStringValue(  );
        }

        public Double ReadPropertyDouble(string propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadDoubleValue();
        }

        public Boolean ReadPropertyBool(string propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadBoolValue();
        }

        public Guid ReadPropertyGuid(  String propertyName )
        {
            var actualPropertyName = ReadPropertyName(  );
            if ( actualPropertyName != propertyName )
                throw new Exception( $"Expected property {propertyName} but got {actualPropertyName}" );
            return ReadGuidValue(  );
        }

    }
}