using System;
using System.Linq;
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

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            //var label = new Label( property.displayName );
            //label.AddToClassList( "unity-property-field__label" );
            //label.AddToClassList( "unity-base-field__label" );
            //label.AddToClassList( "unity-object-field__label" );
            //container.Add( label );

            var category1Attrib = TypeCache.GetTypesWithAttribute<Category1Attribute>();
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
                container.Add( cat1Field );

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
                container.Add( cat1Field );
            }

            

            var cat2Field = new PropertyField( cat2Prop, String.Empty );
            cat2Field.style.flexGrow = 1;
            container.Add( cat2Field );


            return container;

            
        }

        
    }
}