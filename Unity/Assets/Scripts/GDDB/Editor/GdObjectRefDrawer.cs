using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdRef) )]
    public class GdObjectRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
  
            label    = EditorGUI.BeginProperty( position, label, property );
            position = EditorGUI.PrefixLabel( position, controlId, label );
            var propertyPosition = position;

            var fieldPos = position;
            fieldPos.width -= 20;
            var dropdownBtnPos = position;
            dropdownBtnPos.xMin = fieldPos.xMax;

            var oldGDObject = GetGDObject( property );
            EditorGUI.BeginChangeCheck();
            var newGDObject = EditorGUI.ObjectField( fieldPos, GUIContent.none, oldGDObject, typeof(ScriptableObject), false ) as ScriptableObject;
            if ( EditorGUI.EndChangeCheck() && GDDBEditor.AllObjects.Contains( newGDObject ) )
            {
                SetGDObject( property, newGDObject );
            }

            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) )
            {
                var gddb               = GDDBEditor.DB;     
                var selectedObject     = GetGDObject( property );
                var filterAttr         = fieldInfo.GetCustomAttribute( typeof(GdTypeFilterAttribute) ) ;
                var query              = (filterAttr as GdTypeFilterAttribute)?.Query;
                var components         = (filterAttr as GdTypeFilterAttribute)?.Components;
                var gddbBrowserContent = new GdDbBrowserPopupWindowContent( gddb, query, components, selectedObject, propertyPosition,
                        ( sender, obj) =>
                        {
                            SetGDObject( property, obj );
                        },
                        ( sender, obj) =>
                        {
                            SetGDObject( property, obj );
                            sender.editorWindow.Close();
                        } );

               PopupWindow.Show( propertyPosition, gddbBrowserContent );
            }

            EditorGUI.EndProperty();
        }

        private ScriptableObject GetGDObject( SerializedProperty property )
        {
            var gdid = new GdRef()
                       {
                               Serializalble1 = property.FindPropertyRelative( nameof(GdRef.Serializalble1) ).ulongValue,
                               Serializalble2 = property.FindPropertyRelative( nameof(GdRef.Serializalble2) ).ulongValue,
                       };
            var path = AssetDatabase.GUIDToAssetPath( gdid.GUID.ToString("N") );
            if ( !String.IsNullOrEmpty( path ) )
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>( path );
                return obj;
            }

            return null;
        }

        private void SetGDObject( SerializedProperty property, ScriptableObject gdObject )
        {
            if ( gdObject )
            {
                var gdId = new GdRef { GUID = GDDBEditor.GetGDObjectGuid( gdObject ) };
                property.FindPropertyRelative( nameof(GdRef.Serializalble1) ).ulongValue = gdId.Serializalble1;
                property.FindPropertyRelative( nameof(GdRef.Serializalble2) ).ulongValue = gdId.Serializalble2;
            }
            else
            {
                property.FindPropertyRelative( nameof(GdRef.Serializalble1) ).ulongValue = 0;
                property.FindPropertyRelative( nameof(GdRef.Serializalble2) ).ulongValue = 0;
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        private static class Resources
        {
            public static readonly GUIStyle PickerButton = EditorStyles.miniButton;
        }
    }
}