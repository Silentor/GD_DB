﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;

namespace GDDB.Editor
{
    /// <summary>
    /// Provides a tree of Categories for GdType
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

            Root = _categories.First( c => c.Parent == null );
        }

        public String GetTypeString( GdType type )
        {
            if ( type == default )
                return "None";

            var result = String.Empty;
            result = GetTypeString( type, 0, Root, result );
            return result;
        }

        /// <summary>
        /// For given type check if every component is in proper category enum range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="incorrectIndex"></param>
        /// <returns></returns>
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
                if ( category == null )
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
                if ( category != null )
                {
                    result[ i ] = category;
                    category = category.FindItem( type[ i ] ).Subcategory;
                }
            }

            return result;
        }

        [MenuItem( "GDDB/Print hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
             var instance = new GDTypeHierarchy();
             UnityEngine.Debug.Log( instance.PrintHierarchy() );
        }

        public String PrintHierarchy( )
        {
            var result = new StringBuilder();
            PrintCategoryRecursive( Root, result, 0 );
            return result.ToString();
        }

        private void PrintCategoryRecursive( Category category, StringBuilder result, Int32 depth )
        {
            if ( category == null )
                return;

            foreach ( var item in category.Items )
            {
                result.Append( ' ', depth * 4 );
                result.Append( category.UnderlyingType.Name );
                result.Append( "." );
                result.AppendLine(  item.Name );
                PrintCategoryRecursive( item.Subcategory, result, depth + 1 );
            }
        }

        private String GetTypeString( GdType type, Int32 categoryIndex, Category category, String result )
        {
            var value = type[categoryIndex];
            var cat   = category?.FindItem( value ) ?? new CategoryItem( $"{value}", value );
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
                //Find and modify parent category to reference this child
                var parentCategoryType = attr.ParentCategory;
                var parentValue        = attr.ParentValue;
                var parentCategory     = GetOrBuildCategory( parentCategoryType );
                var parentItem         = parentCategory.FindItem( parentValue );
                var result             = new Category( CategoryType.Enum, categoryEnum, items, parentCategory );
                parentItem.Subcategory = result;

                _categories.Add( result );
                return result;
            }
            else
            {
                var result = new Category( CategoryType.Enum, categoryEnum, items, null ); 
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
        public class Category
        {
            public readonly Type               UnderlyingType;
            public readonly CategoryType       Type;
            public          List<CategoryItem> Items;  //Null - default values range
            public readonly Category           Parent;

            public Category(   CategoryType type, Type underlyingType, IEnumerable<CategoryItem> items, Category parent )
            {
                UnderlyingType = underlyingType;
                Type           = type;
                if ( items != null )
                {
                    Items = new List<CategoryItem>( items );
                }
                Parent         = parent;
            }

            public Boolean IsCorrectValue( Int32 value )
            {
                if ( Items.Count > 0 || Type == CategoryType.Enum )
                    return Items.Any( i => i.Value == value ); 

                return Type switch
                       {
                               CategoryType.Int8  => value is >= 0 and <= 255,
                               CategoryType.Int16 => value is >= 0 and <= 65535,
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

        }

        [DebuggerDisplay( "{Name} = {Value} => {Subcategory?.UnderlyingType.Name}" )]
        public class CategoryItem : IEquatable<CategoryItem>
        {
            public readonly String   Name;
            public readonly Int32    Value;
            public          Category Subcategory;

            public CategoryItem(String name, Int32 value, Category subcategory = default )
            {
                Name        = name;
                Value       = value;
                Subcategory = subcategory;
            }
            
            public bool Equals(CategoryItem other)
            {
                return Value == other?.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is CategoryItem other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return Value;
            }

            public static bool operator ==(CategoryItem left, CategoryItem right)
            {
                return left.Equals( right );
            }

            public static bool operator !=(CategoryItem left, CategoryItem right)
            {
                return !left.Equals( right );
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