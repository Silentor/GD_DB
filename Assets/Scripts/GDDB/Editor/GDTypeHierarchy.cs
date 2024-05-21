using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace GDDB.Editor
{
    /// <summary>
    /// Provides an tree of Categories for GdType
    /// </summary>
    public class GDTypeHierarchy
    {
        private readonly List<Category> _categories    = new ();
        public readonly  Category       Root;

        public GDTypeHierarchy( )
        {
            //Get all category types
            var categoryTypes = TypeCache.GetTypesWithAttribute<CategoryAttribute>();

            //Make categories tree
            foreach ( var categoryType in categoryTypes )
            {
                if( _categories.Exists( c => c.UnderlyingType == categoryType ) )
                    continue;

                BuildCategoryFromType( categoryType );
            }

            Root = _categories.First( c => c.IsRoot );
        }

        public String GetTypeString( GdType type )
        {
            if ( type == default )
                return "None";

            var result = String.Empty;
            result = GetTypeString( type, 0, Root, result );
            return result;
        }

        public Boolean IsTypeCorrect( GdType type, out Int32 incorrectIndex )
        {
            if ( type == default )
            {
                incorrectIndex = -1;
                return true;
            }

            var category = Root;
            for ( int i = 0; i < 4; i++ )
            {
                if ( category.IsNone )
                {
                    incorrectIndex = -1;
                    return true;
                }

                if ( !category.IsCorrectValue( type[ i ] ) )
                {
                    incorrectIndex = i;
                    return false;
                }
                else
                {
                    category = category.FindItem( type[ i ] ).Subcategory;
                }
            }

            incorrectIndex = -1;
            return true;
        }

        public IReadOnlyList<Category> GetCategories( GdType type )
        {
            if ( type == default )
                return Array.Empty<Category>();

            var result = new Category[4];
            var category = Root;
            for ( int i = 0; i < 4; i++ )
            {
                if ( !category.IsNone )
                {
                    result[ i ] = category;
                    category = category.FindItem( type[ i ] ).Subcategory;
                }
            }

            return result;
        }

        private String GetTypeString( GdType type, Int32 categoryIndex, Category category, String result )
        {
            var value = type[categoryIndex];
            var cat   = category.FindItem( value );
            result += categoryIndex == 0 ? $"{cat.Name}" : $".{cat.Name}";

            if( categoryIndex < 3 )
                result = GetTypeString( type, categoryIndex + 1, cat.Subcategory, result );

            return result;
        }

        private Category BuildCategoryFromType( Type categoryEnum )
        {
            var items = new List<CategoryItem>();
            foreach ( var enumCategoryField in categoryEnum.GetFields( BindingFlags.Public | BindingFlags.Static ) )
            {
                var name     = enumCategoryField.Name;
                var intValue = (Int32)enumCategoryField.GetValue( null );
                var item     = new CategoryItem( name, intValue );
                items.Add( item );
            }

            var attr   = categoryEnum.GetCustomAttribute<CategoryAttribute>();
            if ( attr.ParentCategory != null )            //Link this Category with parent Category
            {
                var result             = new Category( CategoryType.Enum, categoryEnum, items, false ); 

                var parentCategoryType = attr.ParentCategory;
                var parentValue        = attr.ParentValue;
                var parentCategory     = GetOrBuildCategory( parentCategoryType );
                var parentItem         = parentCategory.FindItem( parentValue );
                _categories.Remove( parentCategory );
                parentCategory         = parentCategory.WithCategoryItem( parentItem.WithSubcategory( result ) );
                _categories.Add( parentCategory );
                return result;
            }
            else
            {
                var result = new Category( CategoryType.Enum, categoryEnum, items, true ); 
                _categories.Add( result );
                return result;
            }
        }

        Category GetOrBuildCategory( Type categoryType )
        {
            if ( !_categories.TryFirst( c => c.UnderlyingType == categoryType, out var result ) )
            {
                result = BuildCategoryFromType( categoryType );
            }

            return result;
        }

        [DebuggerDisplay("{UnderlyingType.Name} ({Type}): {Items.Count}")]
        public readonly struct Category
        {
            public readonly Type                        UnderlyingType;
            public readonly CategoryType                Type;
            public readonly IReadOnlyList<CategoryItem> Items;
            public readonly Boolean                     IsRoot;

            public Boolean  IsNone                      => UnderlyingType == null;

            public Category(   CategoryType type, Type underlyingType, IReadOnlyList<CategoryItem> items, Boolean isRoot )
            {
                UnderlyingType = underlyingType;
                Type           = type;
                Items          = items ?? Array.Empty<CategoryItem>();
                IsRoot         = isRoot;
            }

            public Boolean IsCorrectValue( Int32 value )
            {
                return Type switch
                       {
                               CategoryType.Int8  => value is >= 0 and <= 255,
                               CategoryType.Int16 => value is >= 0 and <= 65535,
                               CategoryType.Enum  => Items.Any( i => i.Value == value ),
                               _                  => throw new ArgumentOutOfRangeException()
                       };
            }

            public Int32 ClampValue( Int32 value )
            {
                return Type switch
                       {
                               CategoryType.Int8  => Math.Clamp( value, 0, 255 ),
                               CategoryType.Int16 => Math.Clamp( value, 0, 65535 ),
                               CategoryType.Enum  => value,
                               _                  => throw new ArgumentOutOfRangeException()
                       };
            }

            public CategoryItem FindItem( Int32 value )
            {
                if ( Items.TryFirst( i => i.Value == value, out var categoryItem ) )
                {
                    return categoryItem;
                }

                return new CategoryItem( $"{value}", value );
            }

            public Category WithCategoryItem( CategoryItem categoryItem )
            {
                var items     = Items.ToList();
                var itemIndex = items.FindIndex( ci => ci.Value == categoryItem.Value );
                if ( itemIndex >= 0 )
                {
                    items.RemoveAt( itemIndex );
                    items.Insert( itemIndex, categoryItem );
                    return new Category( Type, UnderlyingType, items, IsRoot );
                }

                throw new ArgumentOutOfRangeException( nameof(categoryItem) );
            } 
        }

        [DebuggerDisplay( "{Name} = {Value} => {Subcategory?.UnderlyingType.Name}" )]
        public readonly struct CategoryItem
        {
            public readonly String   Name;
            public readonly Int32    Value;
            public readonly Category Subcategory;

            public CategoryItem(String name, Int32 value, Category subcategory = default )
            {
                Name        = name;
                Value       = value;
                Subcategory = subcategory;
            }

            internal CategoryItem WithSubcategory( Category childCategory )
            {
                return new CategoryItem( Name, Value, childCategory );
            }

        }

        public enum CategoryType
        {
            Int8,
            Int16,
            Enum
        }
    }
}