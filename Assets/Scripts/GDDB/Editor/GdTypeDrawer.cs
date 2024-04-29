using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdType) )]
    public class GdTypeDrawer : PropertyDrawer
    {
        private readonly List<SerializedProperty> _catProperties = new ();
        private readonly List<Category>           _categories    = new ();
        private          Category                 _root;

        private VisualElement       _typeWidgetsContainer;

        private SerializedObject _serializedObject;

        public GdTypeDrawer( )
        {
             _root = BuildTypeHierarchy();
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            _serializedObject = property.serializedObject;
            _catProperties.Clear();
            _catProperties.Add( property.FindPropertyRelative( nameof(GdType.Cat1) ) );
            _catProperties.Add( property.FindPropertyRelative( nameof(GdType.Cat2) ) );
            _catProperties.Add( property.FindPropertyRelative( nameof(GdType.Cat3) ) );
            _catProperties.Add( property.FindPropertyRelative( nameof(GdType.Element) ) );

            var root = new VisualElement();

            root.style.flexDirection = FlexDirection.Row;
            var label = new Label( property.displayName );
            label.style.flexBasis = new StyleLength( new Length( 40, LengthUnit.Percent ) );
            root.Add( label );

            _typeWidgetsContainer                     = new VisualElement();
            _typeWidgetsContainer.style.flexDirection = FlexDirection.Row;
            _typeWidgetsContainer.style.flexGrow      = 1;
            //_typeWidgetsContainer.AddToClassList( "unity-base-field__aligned" );        
            root.Add( _typeWidgetsContainer );
            //var label = new Label( property.displayName );
            //label.AddToClassList( "unity-property-field__label" );
            //label.AddToClassList( "unity-base-field__label" );
            //label.AddToClassList( "unity-object-field__label" );
            //container.Add( label );

            CreateWidgets( 0, _root );

            return root;
        }

        private void CreateWidgets( Int32 index, Category category )
        {
            if( index >= _catProperties.Count )
                return;

            Debug.Log( $"Creating widget at {index} position, properties count {_catProperties.Count}" );

            //Clear this widget and all next
            while ( _typeWidgetsContainer.childCount > index )
                _typeWidgetsContainer.RemoveAt( index );

            var widget = CreateCategoryField( category, _catProperties[ index ].intValue, index );
            _typeWidgetsContainer.Add( widget );

            //Create next widget(s)
            var nextCategory = category?.FindItem( _catProperties[ index ].intValue )?.Subcategory;
            CreateWidgets( index + 1, nextCategory );
        }

        private VisualElement CreateCategoryField( Category category, Int32 value, Int32 index )
        {
            VisualElement result = null;
            if ( category == null )         //Default Int8 field
            {
                result                = new IntegerField( 3 ){ value = value };
                result.style.minWidth = 50;
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>, Int32>( OnChangeEnumWidget, index  );

                Debug.Log( $"Created int field, value {value}" );
            }
            else if ( category.Type == CategoryType.Enum )
            {
                result = new PopupField<Int32>( category.Items.Select( i => i.Value ).ToList(), value, i => category.FindItem( i ).Name,
                        i => category.FindItem( i ).Name );
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>, Int32>( OnChangeEnumWidget, index  );

                Debug.Log( $"Created popup field for {category.UnderlyingType} category" );
            }

            return result;

            //Rebuild widgets after change
            void OnChangeEnumWidget( ChangeEvent<Int32> evt, Int32 index )
            {
                _serializedObject.Update();
                _catProperties[ index ].intValue = evt.newValue;
                _serializedObject.ApplyModifiedProperties();

                var nextCategory = category?.FindItem( evt.newValue )?.Subcategory;
                CreateWidgets( index + 1, nextCategory );
            }
        }

        

        Category GetOrBuildCategory( Type categoryType )
        {
            var result   = _categories.FirstOrDefault( c => c.UnderlyingType == categoryType );
            if ( result == null )            
                result = BuildCategoryFromType( categoryType );

            return result;
        }

        private Category BuildTypeHierarchy( )
        {
            //Get all category types
            _categories.Clear();
            var categoryTypes = TypeCache.GetTypesWithAttribute<CategoryAttribute>();

            //Make categories tree
            foreach ( var categoryType in categoryTypes )
            {
                if( _categories.Exists( c => c.UnderlyingType == categoryType ) )
                    continue;

                BuildCategoryFromType( categoryType );
            }

            _root = _categories.First( c => c.IsRoot );
            return _root;
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
            public String       Name;
            public Int32        Value;
            public Category     Subcategory;
        }

        public enum CategoryType
        {
            Int8,
            Int16,
            Enum
        }
        
    }

    
    
}