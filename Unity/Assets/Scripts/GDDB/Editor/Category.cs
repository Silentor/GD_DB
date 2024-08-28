using System;
using System.Globalization;
using System.Linq;

namespace GDDB.Editor
{
    public partial class Category
    {
        /// <summary>
        /// Extract value of current category from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Int32 GetValue( GdType type )
        {
            var shift = GetShift( Index, Type );
            var mask  = GetMask();
            return (Int32)( ( type.Data >> shift ) & mask );
        }

        /// <summary>
        /// Extract item for current category from type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public CategoryItem GetItem( GdType type )
        {
            var value = GetValue( type );
            if( Type == CategoryType.Enum )
                return Items.FirstOrDefault( i => i.Value == value );
            return new CategoryItem( value.ToString( CultureInfo.InvariantCulture ), value, this );     //For Int values
        }

        public GdType SetValue( GdType type, Int32 value )
        {
            var shift = GetShift( Index, Type );
            var mask  = GetMask();
            var data = type.Data;
            data &= ~( mask << shift );
            data |= (UInt32)value << shift;
            type.Data = data;
            return type;
        }

    }
}