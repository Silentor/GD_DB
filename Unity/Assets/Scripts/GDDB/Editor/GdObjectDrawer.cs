using System;
using System.Collections.Generic;
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
            var controlId      = GUIUtility.GetControlID(FocusType.Keyboard, position);

            //Validation
            var isValueError   = false;
            var selectedObject = GetGDObject( property );
            if ( Event.current.type == EventType.Repaint )          //Try to minimize heavy checks
            {
                var (allowedObjects, allowedFolders)     = GetAllowedObjects();
                var isAllowedNullValue = IsAllowedNullReference();
                isValueError       = (selectedObject == null && !isAllowedNullValue) || ( allowedObjects != null && !allowedObjects.Contains( selectedObject ) );
            }

            //Draw prefix label
            label    = EditorGUI.BeginProperty( position, label, property );
            position = EditorGUI.PrefixLabel( position, controlId, label, isValueError ? Resources.PrefixLabelErrorStyle : Resources.PrefixLabelStyle );
            var propertyPosition = position;

            //Draw field
            var fieldPos = position;
            fieldPos.width -= 20;
            var dropdownBtnPos = position;
            dropdownBtnPos.xMin = fieldPos.xMax;
            var guiContent = GetFieldContent( selectedObject );
            EditorGUI.LabelField( fieldPos, GUIContent.none, guiContent, isValueError ? Resources.FieldErrorStyle : Resources.FieldStyle );

            //Ping folder on field click support
            if( Event.current.isMouse && Event.current.type == EventType.MouseDown && fieldPos.Contains( Event.current.mousePosition ) && selectedObject != null )
            {
                EditorGUIUtility.PingObject( selectedObject );
            }

            //Drag and drop folders from Project view support
            if ( (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform ) && fieldPos.Contains( Event.current.mousePosition ) )
            {
                var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                if ( draggedObject is ScriptableObject draggedSO )
                {
                    var (allowedObjects, _) = GetAllowedObjects();
                    if ( allowedObjects == null || allowedObjects.Contains( draggedSO ) )
                        if( Event.current.type == EventType.DragUpdated )
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        else
                            SetGDObject( property, draggedSO );
                }
            }

            //Draw drop down folder browser
            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) || IsPressEnter( controlId ) )
            {
                var gddb    = GDDBEditor.DB;
                var allowed = GetAllowedObjects();
                var gddbBrowserContent = new GdDbBrowserPopupWindowContent( gddb, allowed.resultObjects, allowed.resultFolders, selectedObject, propertyPosition, IsAllowedNullReference(), GdDbBrowserWidget.EMode.Objects, 
                        ( sender, folder, obj ) =>
                        {
                            SetGDObject( property, obj );
                        },
                        ( sender, folder, obj ) =>
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
            var guid = GuidToLongs.ToGuid( property.FindPropertyRelative( "Part1" ).ulongValue, property.FindPropertyRelative( "Part2" ).ulongValue );
            var path = AssetDatabase.GUIDToAssetPath( guid.ToString("N") );
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
                var (part1, part2) = GuidToLongs.ToLongs( GDDBEditor.GetGDObjectGuid( gdObject ) ); 
                property.FindPropertyRelative( "Part1" ).ulongValue = part1;
                property.FindPropertyRelative( "Part2" ).ulongValue = part2;
            }
            else
            {
                property.FindPropertyRelative( "Part1" ).ulongValue = 0;
                property.FindPropertyRelative( "Part2" ).ulongValue = 0;
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        private GUIContent GetFieldContent( ScriptableObject selectedObject ) 
        {
            if( selectedObject != null )
            {
                var content = new GUIContent( selectedObject.name, GetObjectIcon( selectedObject ) );
                return content;
            }
            else
            {
                return new GUIContent( "None" );
            } 
        }

        public static Texture2D GetObjectIcon( ScriptableObject selectedObject )
        {
            if ( selectedObject is GDRoot )
                return Resources.GDRootIcon;
            else if ( selectedObject is GDObject )
                return Resources.GDObjectIcon;
            return Resources.SObjectIcon;
        }

        private (List<ScriptableObject> resultObjects, List<GdFolder> resultFolders) GetAllowedObjects( )
        {
            var filterAttr = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>( ) ;
            if ( filterAttr == null )
                return (null, null);

            var query         = filterAttr.Query;
            var components    = filterAttr.Components;
            var editorDB      = GDDBEditor.DB;
            var resultObjects = new List<ScriptableObject>();
            var resultFolders = new List<GdFolder>();
            editorDB.FindObjects( query, resultObjects, resultFolders ).FindObjectType( filterAttr.ObjectType ).FindComponents( filterAttr.Components );
            return ( resultObjects, resultFolders );
        }

        private Boolean IsAllowedNullReference( )
        {
            var filterAttr = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>( ) ;
            if ( filterAttr == null )
                return true;

            return filterAttr.AllowNullReference;
        }

        private static Boolean IsPressEnter(Int32 myControlId )
        {
            return Event.current.GetTypeForControl( myControlId ) == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter );
        }

        private static class Resources
        {
            public static readonly GUIStyle  PrefixLabelStyle      = new GUIStyle( EditorStyles.label ) ;
            public static readonly GUIStyle  PrefixLabelErrorStyle = new GUIStyle( PrefixLabelStyle ) { normal   = { textColor = Color.red }, focused = { textColor = Color.red } };
            public static readonly GUIStyle  FieldStyle        = new ( GUI.skin.textField ) { imagePosition  = ImagePosition.ImageLeft };
            public static readonly GUIStyle  FieldErrorStyle   = new ( FieldStyle ) { normal             = { textColor = Color.red}};
            public static readonly GUIStyle  PickerButton          = EditorStyles.miniButton;

            public static readonly Texture2D GDRootIcon = UnityEngine.Resources.Load<Texture2D>( "database_24dp" );
            public static readonly Texture2D GDObjectIcon = UnityEngine.Resources.Load<Texture2D>( "description_24dp" );
            public static readonly Texture2D SObjectIcon = (Texture2D)EditorGUIUtility.IconContent( EditorGUIUtility.isProSkin ? "d_ScriptableObject Icon" : "ScriptableObject Icon" ).image;
        }
    }
}