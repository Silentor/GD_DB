using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdType) )]
    public class GdTypeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            var cat1Prop = property.FindPropertyRelative( nameof(GdType.Cat1) );
            var cat2Prop = property.FindPropertyRelative( nameof(GdType.Cat2) );
            var cat3Prop = property.FindPropertyRelative( nameof(GdType.Cat3) );
            var elemProp = property.FindPropertyRelative( nameof(GdType.Element) );

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            //var label = new Label( property.displayName );
            //label.AddToClassList( "unity-property-field__label" );
            //label.AddToClassList( "unity-base-field__label" );
            //label.AddToClassList( "unity-object-field__label" );
            //container.Add( label );

            var rootCategory = BuildTypeHierarchy();
            if ( rootCategory.Type == CategoryType.Enum )
            {
                var cat1Field = new PopupField<Int32>( preferredLabel, cat1Indexes,
                        Array.IndexOf( cat1Values, cat1Prop.intValue ),
                        Category1FormatListItem, Category1FormatListItem );
            }

            var category1Attrib = TypeCache.GetTypesWithAttribute<MainCategoryAttribute>();
            var cat1EnumType        = category1Attrib.FirstOrDefault();
            if ( cat1EnumType != default )
            {
                var cat1Names   = Enum.GetNames( cat1EnumType );
                var cat1Values  = Enum.GetValues( cat1EnumType ).Cast<Int32>().ToArray();
                var cat1Indexes = Enumerable.Range( 0, cat1Names.Length ).ToList();
                var cat1Field = new PopupField<Int32>( preferredLabel, cat1Indexes,
                        Array.IndexOf( cat1Values, cat1Prop.intValue ),
                        Category1FormatListItem, Category1FormatListItem );
                cat1Field.style.flexGrow = 1;
                cat1Field.RegisterValueChangedCallback( Category1EnumChanged );
                root.Add( cat1Field );

                String Category1FormatListItem( Int32 index )
                {
                    return cat1Names[ index ];
                }

                void Category1EnumChanged(ChangeEvent<Int32> evt )
                {
                    cat1Prop.intValue = cat1Values[ evt.newValue ];
                    cat1Prop.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                var cat1Field = new PropertyField( cat1Prop, preferredLabel );
                cat1Field.style.flexGrow = 1;
                root.Add( cat1Field );
            }

            

            var cat2Field = new PropertyField( cat2Prop, String.Empty );
            cat2Field.style.flexGrow = 1;
            root.Add( cat2Field );


            return root;

            
        }

        private Category BuildTypeHierarchy( )
        {
            var categoryEnums = TypeCache.GetTypesWithAttribute<CategoryAttribute>();


            var root              = new Category();
            var mainCategoryTypes = TypeCache.GetTypesWithAttribute<MainCategoryAttribute>();
            var mainCategoryEnum  = mainCategoryTypes.FirstOrDefault();
            if ( mainCategoryEnum != default )
            {
                root.Type = CategoryType.Enum;
                //var cat1Names  = Enum.GetNames( mainCategoryEnum );
                //var cat1Values = Enum.GetValues( mainCategoryEnum ).Cast<Int32>().ToArray();
                foreach ( var enumCategoryField in mainCategoryEnum.GetFields() )
                {
                     var name = enumCategoryField.Name;
                     var intValue = (Int32)enumCategoryField.GetValue( null );
                     var item = new CategoryItem(){Name = name, Value = intValue};
                     root.Items.Add( item );
                }


            }
            else
            {
                root.Type = CategoryType.Int8;
            }

            return root;
        }

        private Category BuildCategoryFromEnum( Type categoryEnum )
        {
            var result = new Category(){Type = CategoryType.Enum};
            foreach ( var enumCategoryField in categoryEnum.GetFields() )
            {
                var name     = enumCategoryField.Name;
                var intValue = (Int32)enumCategoryField.GetValue( null );
                var item     = new CategoryItem() { Name = name, Value = intValue };
                var subcategoryAttribute = enumCategoryField.GetCustomAttribute<SubcategoryAttribute>();
                if( subcategoryAttribute != null  )
                {
                      var subCategoryEnum = subcategoryAttribute.SubcategoryEnum;
                      item.Subcategory = BuildCategoryFromEnum( subCategoryEnum );
                }
                result.Items.Add( item );
            }

            return result;
        }

        public class Category
        {
            public CategoryType       Type;
            public List<CategoryItem> Items;
        }

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