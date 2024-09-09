using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdId) )]
    public class GdIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            //var totalPos  = position;
            //var fieldPos  = position; fieldPos.xMin += EditorGUIUtility.labelWidth + 2;
            //var buttonPos = Resources.PickerButton.margin.Remove(new Rect(position.xMax - 19, position.y, 19, position.height));

            var totalPosition = position;
            label    = EditorGUI.BeginProperty( position, label, property );
            position = EditorGUI.PrefixLabel( position, controlId, label );

            var fieldPos = position;
            fieldPos.width -= 20;
            var dropdownBtnPos = position;
            dropdownBtnPos.xMin = fieldPos.xMax;

            var oldGDObject = GetGDObject( property );
            EditorGUI.BeginChangeCheck();
            var newGDObject = EditorGUI.ObjectField( fieldPos, GUIContent.none, oldGDObject, typeof(GDObject), false ) as GDObject;
            if ( EditorGUI.EndChangeCheck() )
            {
                SetGDObject( property, newGDObject );
            }

            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) )
            {
                var gddb         = new GdEditorLoader(  ).GetGameDataBase();     
                var dropDownRect = GUIUtility.GUIToScreenRect( totalPosition );               //Because PropertyDrawer use local coords
                dropDownRect.y += 50;
                var selectedObject = GetGDObject( property );
                var filterAttr     = fieldInfo.GetCustomAttribute( typeof(GdTypeFilterAttribute) ) ;
                var query          = (filterAttr as GdTypeFilterAttribute)?.Query;
                var components     = (filterAttr as GdTypeFilterAttribute)?.Components;
                var treeBrowser    = GdDbTreeWindow.Open( gddb, query, components, selectedObject, dropDownRect );
                treeBrowser.Selected += ( gdObject ) =>
                {
                    SetGDObject( property, gdObject );
                    Debug.Log( $"Selected {gdObject.Name}" );
                };
                treeBrowser.Chosed += ( gdObject ) =>
                {
                    SetGDObject( property, gdObject );
                    Debug.Log( $"Chosed {gdObject.Name}" );
                };
            }



            //GUI.TextField( fieldPos, displayedObject ? displayedObject.name : "None" );
            // var btnPos = position;
            // btnPos.xMin = btnPos.xMax - 20;
            // if ( GUI.Button( btnPos, "", Resources.PickerButton ) )
            // {
            //     EditorGUIUtility.ShowObjectPicker<GDObject>(null, false, "t:GDObject", controlId);
            //     Debug.Log( "Show picker" );
            // }

            // switch ( Event.current.type )
            // {
            //       case EventType.ExecuteCommand:
            //           if ( Event.current.commandName == "ObjectSelectorUpdated" )
            //           {
            //               var obj = EditorGUIUtility.GetObjectPickerObject();
            //               if ( obj is GDObject gdObject )
            //               {
            //                   var assetGuid = gdObject.Guid;
            //                   var gdId = new GdId { GUID = assetGuid };
            //                   property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue = gdId.Serializalble1;
            //                   property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue = gdId.Serializalble2;
            //                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier( obj, out var unityguid, out Int64 _ );
            //                   Debug.Log( $"Selected {gdObject.name}, my guid {assetGuid}, unity guid {unityguid}" );
            //                   property.serializedObject.ApplyModifiedProperties();
            //               }
            //           }
            //           break; 
            // }
            EditorGUI.EndProperty();
        }

        private GDObject GetGDObject( SerializedProperty property )
        {
            var gdid = new GdId()
                       {
                               Serializalble1 = property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue,
                               Serializalble2 = property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue,
                       };
            var path = AssetDatabase.GUIDToAssetPath( gdid.GUID.ToString("N") );
            if ( !String.IsNullOrEmpty( path ) )
            {
                var obj = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                return obj;
            }

            return null;
        }

        private void SetGDObject( SerializedProperty property, GDObject gdObject )
        {
            if ( gdObject )
            {
                var gdId = new GdId { GUID = gdObject.Guid };
                property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue = gdId.Serializalble1;
                property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue = gdId.Serializalble2;
            }
            else
            {
                property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue = 0;
                property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue = 0;
            }
            property.serializedObject.ApplyModifiedProperties();
        }
                                       
        // public override VisualElement CreatePropertyGUI(SerializedProperty property)
        // {
        //     return null;
        //
        //     var root      = new VisualElement(){style = { flexDirection = FlexDirection.Row}};
        //     var guidLabel = new Label();
        //     guidLabel.TrackPropertyValue(  property );
        //     guidLabel.RegisterCallback<SerializedPropertyChangeEvent, Label>( RefreshGuidLabel, guidLabel );
        //     RefreshGuidLabel( property, guidLabel );
        //
        //     root.Add( guidLabel );
        //
        //     var btn = new Button( ( ) =>
        //     {
        //         var g = new GdId  { GUID = Guid.NewGuid() };
        //         property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue = g.Serializalble1;
        //         property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue = g.Serializalble2;
        //         property.serializedObject.ApplyModifiedProperties();
        //     } );
        //     btn.text = "Rnd";
        //     root.Add( btn );
        //
        //     return root;
        // }

        // private void RefreshGuidLabel(SerializedPropertyChangeEvent evt, Label label )
        // {
        //    RefreshGuidLabel(  evt.changedProperty, label );
        // }

        // private void RefreshGuidLabel(  SerializedProperty property, Label label )
        // {
        //     var g = new GdId()
        //             {
        //                     Serializalble1 = property.FindPropertyRelative( nameof(GdId.Serializalble1) ).ulongValue,
        //                     Serializalble2 = property.FindPropertyRelative( nameof(GdId.Serializalble2) ).ulongValue,
        //             };
        //     label.text = g.GUID.ToString();
        // }

        private static class Resources
        {
            public static readonly GUIStyle PickerButton = EditorStyles.miniButton;
        }
    }
}