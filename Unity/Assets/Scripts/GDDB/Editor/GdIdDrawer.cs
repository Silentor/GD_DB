using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdId) )]
    public class GdIdDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root      = new VisualElement(){style = { flexDirection = FlexDirection.Row}};
            var guidLabel = new Label();
            guidLabel.TrackPropertyValue(  property );
            guidLabel.RegisterCallback<SerializedPropertyChangeEvent, Label>( RefreshGuidLabel, guidLabel );
            RefreshGuidLabel( property, guidLabel );

            root.Add( guidLabel );

            var btn = new Button( ( ) =>
            {
                var g = new GdId  { GUID = Guid.NewGuid() };
                property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue = g.Serializalble1;
                property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue = g.Serializalble2;
                property.serializedObject.ApplyModifiedProperties();
            } );
            btn.text = "Rnd";
            root.Add( btn );

            return root;
        }

        private void RefreshGuidLabel(SerializedPropertyChangeEvent evt, Label label )
        {
           RefreshGuidLabel(  evt.changedProperty, label );
        }

        private void RefreshGuidLabel(  SerializedProperty property, Label label )
        {
            var g = new GdId()
                    {
                            Serializalble1 = property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue,
                            Serializalble2 = property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue,
                    };
            label.text = g.GUID.ToString();
        }


    }
}