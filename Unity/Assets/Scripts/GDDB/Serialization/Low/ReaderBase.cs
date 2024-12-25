using System;
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

        public abstract String GetPropertyName( );
        public abstract String GetStringValue( );


        /// <summary>
        /// Get any integer value up to int64
        /// </summary>
        /// <returns></returns>
        public abstract Int64 GetIntegerValue( );
        public abstract Int32 GetInt32Value( );
        public abstract UInt64 GetUInt64Value( );

        /// <summary>
        /// Get any float value (single or double)
        /// </summary>
        /// <returns></returns>
        public abstract Double GetFloatValue( );
        public abstract Single GetSingleValue( );
        public abstract Double GetDoubleValue( );
        

        public abstract Boolean GetBoolValue( );


        public abstract void SkipProperty( );

        public String ReadPropertyName( )
        {
            var propertyToken = ReadNextToken();
            if( propertyToken == EToken.PropertyName )
                return GetPropertyName();
            else
                throw new Exception( $"Expected property name but got {propertyToken}" );
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

        public Int32  ReadInt32Value( )
        {
            ReadNextToken();
            return GetInt32Value();
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
    }
}