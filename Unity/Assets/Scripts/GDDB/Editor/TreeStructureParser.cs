#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace GDDB.Editor
{
    public class TreeStructureParser
    {
        public Category ParseJson( string json, CancellationToken cancel )
        {
            if ( String.IsNullOrEmpty( json ) )
                throw new ArgumentNullException( nameof(json), "Json data is missing" );
            var root            = JObject.Parse( json );
            if( root.First is not JProperty rootCategory )
                throw new ( "Root category not found" );
            if( rootCategory.Value is not JObject rootValue )
                throw new ( "Root category value is not object" );
            return BuildCategory( rootCategory.Name, rootValue, 0, null, cancel );
        }

        public void ToFlatList( Category root, List<Category> result )
        {
            if( root == null )
                return;

            result.Add( root );
            foreach ( var item in root.Items )
            {
                if ( item.Subcategory != null )
                    ToFlatList( item.Subcategory, result ) ;
            }
        }

        private static Category BuildCategory(     String            name, JObject jCategory, Int32 index, CategoryItem? parentItem,
                                                   CancellationToken cancel )
        {
            var jItems            = jCategory["Items"] as JObject;
            if ( jItems == null )
                throw new ArgumentException( "Is not valid category" );

            cancel.ThrowIfCancellationRequested();

            var result = new Category( name, CategoryType.Enum, parentItem, index );
            var items  = new List<CategoryItem>();
            foreach ( var jItem in jItems.Children<JProperty>() )
            {
                var      itemName    = jItem.Name;
                var      itemValue   = jItem.Value["Value"].Value<Int32>();
                var      childItems  = jItem.Value["Items"];
                var      item        = new CategoryItem( itemName, itemValue, result );
                Category? subcategory = null;
                if ( childItems != null )            
                    subcategory = BuildCategory( itemName, jItem.Value as JObject, index + 1, item, cancel );
                item.Subcategory = subcategory;
                items.Add( item );
            }

            result.Items.AddRange( items );
            return result;
        }
    }

    [DebuggerDisplay("{Name} ({Type}): {Items.Count}")]
    public class Category
    {
        public readonly String              Name;
        public readonly CategoryType        Type;
        public readonly Int32               Index;
        public          Category?           Parent            => ParentItem?.Owner;
        public          CategoryItem?       ParentItem;
        public readonly  List<CategoryItem> Items = new();

        public Int32 MinValue
        {
            get
            {
                if ( Type == CategoryType.Enum )
                    return Items.Min( i => i.Value );
                return 0;
            }
        }

        public Int32 MaxValue
        {
            get
            {
                if ( Type == CategoryType.Enum )
                    return Items.Max( i => i.Value );
                return (Int32)GetMask();
            }
        }

        public Int32 Count
        {
            get
            {
                if( Type == CategoryType.Enum )
                    return Items.Count;

                return MaxValue - MinValue + 1;
            }
        }

        public Category( String name, CategoryType type, CategoryItem? parent, Int32 index )
        {
            Name       = name;
            Type       = type;
            ParentItem = parent;
            Index      = index;
        }

        public Boolean IsCorrectValue( Int32 value )
        {
            if ( Type == CategoryType.Enum )
            {
                if ( Items != null && Items.Any( i => i.Value == value ) )
                    return true;
            }
            else
            {
                if( Items != null )
                {
                    if ( Items.Any( i => i.Value == value ) ) 
                        return true;
                }
                else
                    return true;
            }

            return false;
        }

        public CategoryItem GetItem( GdType type )
        {
            var value = GetValue( type );
            if( Type == CategoryType.Enum )
                return Items.FirstOrDefault( i => i.Value == value );
            return new CategoryItem( value.ToString( CultureInfo.InvariantCulture ), value, this );     //For Int values
        }

        public CategoryItem GetItem( Int32 value )
        {
            if( Type == CategoryType.Enum )
                return Items.FirstOrDefault( i => i.Value == value );
            return new CategoryItem( value.ToString( CultureInfo.InvariantCulture ), value, this );     //For Int values
        }

        public Int32 GetValue( GdType type )
        {
            var shift = GetShift( Index, Type );
            var mask  = GetMask();
            return (Int32)( ( type.Data >> shift ) & mask );
        }

        public Int32 GetValue( UInt32 rawData )
        {
            var shift = GetShift( Index, Type );
            var mask  = GetMask();
            return (Int32)( ( rawData >> shift ) & mask );
        }

        public void SetValue( ref GdType type, Int32 value )
        {
            var shift = GetShift( Index, Type );
            var mask  = GetMask();
            var data  = type.Data;
            data      &= ~( mask << shift );
            data      |= (UInt32)value << shift;
            type.Data =  data;
        }

        public static Int32 GetShift( Int32 index, CategoryType type )
        {
            var indexShift = 3 - index;
            var typeShift  = type switch
                             {
                                     CategoryType.Enum  => 0,
                                     CategoryType.Int8  => 0,
                                     CategoryType.Int16 => 8,
                                     CategoryType.Int24 => 16,
                                     CategoryType.Int32 => 24,
                                     _                  => throw new ArgumentOutOfRangeException()
                             };
            return (indexShift - typeShift) * 8;
        }

        public static CategoryType GetValueTypeForIndex( Int32 index )
        {
            return index switch
                   {
                           0 => CategoryType.Int32,
                           1 => CategoryType.Int24,
                           2 => CategoryType.Int16,
                           3 => CategoryType.Int8,
                           _ => throw new ArgumentOutOfRangeException()
                   };
        }

        public UInt32 GetMask( )
        {
            return Type switch
                   {
                           CategoryType.Enum  => 0x000000FF,
                           CategoryType.Int8  => 0x000000FF,
                           CategoryType.Int16 => 0x0000FFFF,
                           CategoryType.Int24 => 0x00FFFFFF,
                           CategoryType.Int32 => 0xFFFFFFFF,
                           _                  => throw new ArgumentOutOfRangeException()
                   };
        }
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    public class CategoryItem
    {
        public readonly String    Name;
        public readonly Int32     Value;
        public readonly Category  Owner;
        public          Category? Subcategory;
        public          Boolean   IsValue      => Subcategory == null;

        public CategoryItem( String name, Int32 value, Category owner )
        {
            Name   = name;
            Value  = value;
            Owner  = owner;
        }

        private String DebuggerDisplay => $"{Owner.Name}.{Name} ({Value}): {(Subcategory != null ? $"=> {Subcategory.Name}" : String.Empty)}";
    }

    public enum CategoryType
    {
        Int8,           //Value 8 bits
        Int16,          //Value 16 bits
        Int24,          //Value 24 bits
        Int32,          //Value 32 bits
        Enum            //Namespace or enum value, always 8 bits
    }

}