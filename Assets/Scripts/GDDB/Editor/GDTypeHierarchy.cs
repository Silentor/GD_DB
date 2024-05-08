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

        private String GetTypeString( GdType type, Int32 categoryIndex, Category category, String result )
        {
            var value = type[categoryIndex];
            var cat   = category?.FindItem( value );
            if( cat != null )
                result += categoryIndex == 0 ? $"{cat.Name}" : $".{cat.Name}";
            else
                result += categoryIndex == 0 ? $"{value}" : $".{value}";

            if( categoryIndex < 3 )
                result = GetTypeString( type, categoryIndex + 1, cat?.Subcategory, result );

            return result;
        }

        private Category BuildCategoryFromType( Type categoryEnum )
        {
            var result = new Category(){Type = CategoryType.Enum, UnderlyingType = categoryEnum, Items = new List<CategoryItem>()};

            foreach ( var enumCategoryField in categoryEnum.GetFields( BindingFlags.Public | BindingFlags.Static ) )
            {
                var name     = enumCategoryField.Name;
                var intValue = (Int32)enumCategoryField.GetValue( null );
                var item     = new CategoryItem() { Name = name, Value = intValue };
                result.Items.Add( item );
            }

            var attr = categoryEnum.GetCustomAttribute<CategoryAttribute>();
            if ( attr.ParentCategory != null )
            {
                var parentCategoryType = attr.ParentCategory;
                var parentValue        = attr.ParentValue;
                var parentCategory     = GetOrBuildCategory( parentCategoryType );
                var parentItem         = parentCategory.FindItem( parentValue );
                parentItem.Subcategory = result;
            }
            else
                result.IsRoot = true;

            _categories.Add( result );

            return result;
        }

        Category GetOrBuildCategory( Type categoryType )
        {
            var result   = _categories.FirstOrDefault( c => c.UnderlyingType == categoryType );
            if ( result == null )            
                result = BuildCategoryFromType( categoryType );

            return result;
        }

        [DebuggerDisplay("{UnderlyingType.Name} ({Type}): {Items.Count}")]
        public class Category
        {
            public Type               UnderlyingType;
            public CategoryType       Type;
            public List<CategoryItem> Items;
            public Boolean            IsRoot;

            public CategoryItem FindItem( Int32 value )
            {
                return Items.FirstOrDefault( i => i.Value == value );
            }
        }

        [DebuggerDisplay( "{Name} = {Value} => {Subcategory?.UnderlyingType.Name}" )]
        public class CategoryItem
        {
            public String   Name;
            public Int32    Value;
            public Category Subcategory;
        }

        public enum CategoryType
        {
            Int8,
            Int16,
            Enum
        }
    }
}