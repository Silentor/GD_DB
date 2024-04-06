using System;
using UnityEditor;
using UnityEditor.UIElements;
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

            var cat1Field = new PropertyField( cat1Prop, preferredLabel );
            cat1Field.style.flexGrow = 1;
            container.Add( cat1Field );

            var cat2Field = new PropertyField( cat2Prop, String.Empty );
            cat2Field.style.flexGrow = 1;
            container.Add( cat2Field );


            return container;

        }
    }
}