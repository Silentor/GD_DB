using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    /// <summary>
    /// Provides a tree of Categories for GdType
    /// </summary>
    public class GDTypeHierarchy
    {
        private readonly List<Category> _categories;
        private readonly StringBuilder  _stringBuilder = new ();        //For GetTypeString

        public readonly  Category       Root;

        public  GDTypeHierarchy( )
        {
            var startTimer = new Stopwatch();

            //Get all category types
            var categoryTypes = TypeCache.GetTypesWithAttribute<CategoryAttribute>();

            //Make categories tree
            //1st pass - create all categories
            var categories = new List<CategoryInternal>( categoryTypes.Count );
            foreach ( var categoryType in categoryTypes )
            {
                var cat = new CategoryInternal()
                                       {
                                               Attribute = categoryType.GetCustomAttribute<CategoryAttribute>(),
                                               UnderlyingType = categoryType,
                                       };
                cat.Items = cat.GetValues().ToList();
                cat.Type  = cat.Items.Count > 0 ? CategoryType.Enum : CategoryType.Int8;           
                categories.Add( cat );
            }
            //2nd pass - link parents/children
            foreach ( var category in categories )
            {
                if ( category.Attribute.ParentCategory != null )
                {
                    var parentCategory = categories.First( c => c.UnderlyingType == category.Attribute.ParentCategory );
                    category.Parent = parentCategory;
                    parentCategory.Items.First( i => i.Value == category.Attribute.ParentValue ).Subcategory = category;
                }
            }
            //3rd pass - set category index and validate
            var categoriesToRemove = new List<CategoryInternal>();
            foreach ( var category in categories )
            {
                category.Index = category.GetIndex();

                if ( category.Index > 3 )
                {
                    Debug.LogError( $"[GDTypeHierarchy] Category {category.UnderlyingType.Name} has too deep hierarchy {category.Index}, it will be ignored" );
                    categoriesToRemove.Add( category );
                }
            }
            var mainCategory = categories.Where( c => c.Parent == null ).ToArray();
            if( mainCategory.Length == 0 )
                throw new InvalidOperationException( "[GDTypeHierarchy] No main category found, should be one root category with Parent null" );
            if( mainCategory.Length > 1 )
                throw new InvalidOperationException( $"[GDTypeHierarchy] Too many main categories found ({mainCategory.Length}), should be one root category with Parent null" );
            if( mainCategory.First().Items.Any( i => i.Value == 0 ) )
                throw new InvalidOperationException( $"[GDTypeHierarchy] Zero item at main category {mainCategory.First().UnderlyingType.Name} is reserved, please start with 1" );
            foreach ( var catToRemove in categoriesToRemove )
            {
                categories.Remove( catToRemove );
            }
            //4th pass - calculate int value width
            foreach ( var category in categories )
            {
                foreach ( var itemInternal in category.Items )
                {
                     itemInternal.IsValue = itemInternal.Subcategory == null;
                     if ( itemInternal.IsValue && category.Type != CategoryType.Enum )
                         category.Type = Category.GetValueTypeForIndex( category.Index );
                }
            }

            //Build final categories
            categories.Sort( (c1, c2) => c1.Index.CompareTo( c2.Index ) );      //Topo sorting    
            _categories = new List<Category>( categories.Count );
            //Create categories
            foreach ( var categoryInternal in categories )
            {
                var parent = categoryInternal.Parent != null ? _categories.First( c => c.UnderlyingType == categoryInternal.Parent.UnderlyingType ) : null;
                var category = new Category( categoryInternal.Type, categoryInternal.UnderlyingType, parent, categoryInternal.Index );
                _categories.Add( category );
            }

            //Create category items
            for ( var i = 0; i < categories.Count; i++ )
            {
                var categoryInternal = categories[ i ];
                var category         = _categories[ i ];
                if(category.Type == CategoryType.Enum)
                {
                    category.Items = new List<CategoryItem>( categoryInternal.Items.Count );
                    foreach ( var internalItem in categoryInternal.Items )
                    {
                        var subcategory = internalItem.Subcategory != null ? _categories.First( c => c.UnderlyingType == internalItem.Subcategory.UnderlyingType ) : null;
                        var item = new CategoryItem( internalItem.Name, internalItem.Value, category, subcategory );
                        category.Items.Add( item );
                    }
                }
            }

            Root = _categories.FirstOrDefault( c => c.Parent == null );

            startTimer.Stop();

            var possibleTypesCount = 0;
            foreach ( var category in _categories )
            {
                if ( category.Type == CategoryType.Enum )
                    possibleTypesCount += category.Items.Count( i => i.Subcategory == null );
                else
                    possibleTypesCount += category.Count;
            }
            Debug.Log( $"[GDTypeHierarchy] Hierrarchy build time {startTimer.ElapsedMilliseconds} ms, possible types count {possibleTypesCount}" );
            PrintHierarchy();
        }

        public String GetTypeString( GdType type )
        {
            if ( type == default )
                return "None";

            _stringBuilder.Clear();
            GetTypeString( type, _stringBuilder );
            return _stringBuilder.ToString();
        }

        /// <summary>
        /// For given type check if every component is in proper category range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="incorrectIndex"></param>
        /// <returns></returns>
        public Boolean IsTypeInRange( GdType type, out Category incorrectCategory )
        {
            if ( type == default )
            {
                incorrectCategory = null;
                return true;
            }

            var metadata = GetMetadataOf( type );
            return metadata.IsTypeInRange( type, out incorrectCategory );
        }

        public Boolean IsTypeDefined( GdType type )
        {
            if ( type == default )
                return true;

            var metadata = GetMetadataOf( type );
            return metadata.IsTypeDefined( type );
        }

        public GdTypeMetadata GetMetadataOf( GdType type )
        {
            if ( type == default )
                return GdTypeMetadata.Default;

            if ( Root == null )
            {
                return new GdTypeMetadata( new List<Category>() );
            }

            var result = new List<Category>( 4 );
            var category = Root;
            for ( int i = 0; i < 4; i++ )
            {
                if ( category != null )
                {
                    result.Add( category );
                    var categoryItem = category.GetItem( type );
                    category    = categoryItem?.Subcategory;
                }
                else break;
            }

            return new GdTypeMetadata( result );
        }

        [MenuItem( "GDDB/Print hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
             var instance = new GDTypeHierarchy();
             Debug.Log( instance.PrintHierarchy() );
        }

        public String PrintHierarchy( )
        {
            if( _categories.Count == 0 )
                return "No categories found";

            _stringBuilder.Clear();
            _stringBuilder.Append( Root.UnderlyingType.Name );
            _stringBuilder.Append( " (" );
            _stringBuilder.Append( Root.Type );
            _stringBuilder.AppendLine( ") :" );
            PrintCategoryRecursive( Root, _stringBuilder, 0 );
            return _stringBuilder.ToString();
        }

        private void PrintCategoryRecursive( Category category, StringBuilder result, Int32 depth )
        {
            if ( category == null || category.Items == null )
                return;

            foreach ( var item in category.Items )
            {
                result.Append( ' ', depth * 4 );
                result.Append( category.UnderlyingType.Name );
                result.Append( "." );
                result.Append(  item.Name );
                if ( item.Subcategory != null )
                {
                    result.Append( " (" );
                    result.Append( item.Subcategory.Type );
                    result.AppendLine( ") :" );
                    PrintCategoryRecursive( item.Subcategory, result, depth + 1 );
                }
                else
                    result.AppendLine();
            }
        }

        private void GetTypeString( GdType type, StringBuilder result )
        {
            var metadata = GetMetadataOf( type );
            for ( var i = 0; i < metadata.Categories.Count; i++ )
            {
                var category = metadata.Categories[ i ];
                var value    = category.GetValue( type );
                var categoryItem = category.GetItem( value );
                result.Append( categoryItem != null ? categoryItem.Name : value.ToString( CultureInfo.InvariantCulture ) );
                if ( i < metadata.Categories.Count - 1 )
                    result.Append( '.' );    
            }
        }

        public class GdTypeMetadata
        {
            /// <summary>
            /// Metadata of default GdType
            /// </summary>
            public static readonly GdTypeMetadata Default = new ( new List<Category>() );

            public IReadOnlyList<Category> Categories => _categories;

            public UInt32 Mask
            {
                get
                {
                    if ( _mask == null )
                    {
                        _mask = 0;
                        for ( var i = 0; i < _categories.Length; i++ )
                        {
                            var category = _categories[ i ];
                            _mask |= category.GetMask() << Category.GetShift( i, category.Type );
                        }
                    }

                    return _mask.Value;
                }
            }

            public GdTypeMetadata( List<Category> categories )
            {
                _categories = categories.ToArray();
            }

            public Boolean IsTypeDefined( GdType type )
            {
                if ( type == default )
                    return true;

                return (type.Data & Mask)  == type.Data;
            }

            public GdType ClearUndefinedTypePart( GdType type )
            {
                return new GdType( type.Data & Mask );
            }
        

        public Boolean IsTypeInRange( GdType type, out Category incorrectCategory )
            {
                if( type == default )
                {
                    incorrectCategory = null;
                    return true;
                }

                var data = type.Data;
                for ( var i = 0; i < _categories.Length; i++ )
                {
                    var category = _categories[ i ];
                    if ( !category.IsCorrectValue( category.GetValue( data ) ) )
                    {
                        incorrectCategory = category;
                        return false;
                    }
                }

                incorrectCategory = null;
                return true;
            }

            public Boolean IsTypesEqual( GdType type1, GdType type2 )
            {
                if( type1 == default && type2 == default )
                    return true;

                return (type1.Data & Mask) == (type2.Data & Mask);
            }

            private readonly Category[] _categories;
            private UInt32? _mask;
        }

        [DebuggerDisplay("{UnderlyingType.Name} ({Type}): {Items.Count}")]
        public class Category
        {
            public readonly Type         UnderlyingType;
            public readonly CategoryType Type;
            public readonly Category     Parent;
            public readonly Int32        Index;
            public readonly String       Name;

            public  List<CategoryItem> Items = null;        //Null - default values for Int Type 

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
                    if ( Type == CategoryType.Enum )
                        return Items.Count;
                    return MaxValue - MinValue + 1;
                }
            }


            public Category( CategoryType type, Type underlyingType, Category parent, Int32 index )
            {
                UnderlyingType = underlyingType;
                Type           = type;
                Parent         = parent;
                Index          = index;
                Name           = underlyingType.Name;
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
                                        CategoryType.Enum => 0,
                                        CategoryType.Int8  => 0,
                                        CategoryType.Int16 => 8,
                                        CategoryType.Int24 => 16,
                                        CategoryType.Int32 => 24,
                                        _                 => throw new ArgumentOutOfRangeException()
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
                               _                   => throw new ArgumentOutOfRangeException()
                       };
            }
        }

        [DebuggerDisplay( "{Name} = {Value} => {Subcategory?.UnderlyingType.Name}" )]
        public class CategoryItem : IEquatable<CategoryItem>
        {
            public readonly String        Name;
            public readonly Int32         Value;
            public readonly Category      Owner;
            public readonly Category      Subcategory;

            public CategoryItem(String name, Int32 value, Category owner, Category subcategory = default )
            {
                Name        = name;
                Value       = value;
                Owner       = owner;
                Subcategory = subcategory;
            }
            
            public bool Equals(CategoryItem other)
            {
                return Owner == other?.Owner && Value == other?.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is CategoryItem other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return HashCode.Combine( Owner, Value );
            }
        }

        public enum CategoryType
        {
            Int8,           //Value 8 bits
            Int16,          //Value 16 bits
            Int24,          //Value 24 bits
            Int32,          //Value 32 bits
            Enum            //Namespace or enum value, always 8 bits
        }

        private class CategoryInternal
        {
            public CategoryAttribute  Attribute;
            public Type               UnderlyingType;
            public CategoryType       Type;
            public CategoryInternal   Parent;
            public List<ItemInternal> Items;

            public  Int32 Index;

            public Int32 GetIndex( )
            {
                var result            = 0;
                var inspectedCategory = this;

                while ( inspectedCategory.Parent != null )
                {
                    result++;
                    inspectedCategory = inspectedCategory.Parent;

                    if( result > 255 )          //Probably loop in tree
                        break;
                }

                return result;
            }

            public List<ItemInternal>  GetValues( )
            {
                var result = new List<ItemInternal>();
                foreach ( var field in UnderlyingType.GetFields( BindingFlags.Public | BindingFlags.Static ) )
                {
                    var item = new ItemInternal()
                               {
                                       Field = field,
                                       Name  = field.Name,
                                       Value = (Int32)field.GetValue( null ),
                               };
                    result.Add( item );                    
                }

                return result;
            }
        }

        private class ItemInternal
        {
            public FieldInfo        Field;
            public String           Name;
            public Int32            Value;
            public CategoryInternal Subcategory;
            public Boolean          IsValue;            //True - Last category in hierarchy is a value, false - its a part of namespace
        }
    }
}