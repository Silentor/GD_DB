using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    public static class GdFieldDrawerBase
    {
        public static void DrawGDObjectField( Rect position, GUIContent label, SerializedProperty property, FieldInfo fieldInfo, ScriptableObject currentObject, IReadOnlyList<ScriptableObject> allowedObjects, IReadOnlyList<GdFolder> allowedFolders, Boolean allowNullObject, OnGdObjectSelected onSelected )
        {
            var controlId      = GUIUtility.GetControlID(FocusType.Keyboard, position);

            //Validation
            var isValueError   = false;
            if ( Event.current.type == EventType.Repaint )          //Try to minimize heavy checks
            {
                isValueError = (!currentObject && !allowNullObject) || ( currentObject && allowedObjects != null && !allowedObjects.Contains( currentObject ) );
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
            var guiContent = GetFieldContent( currentObject, property, fieldInfo );
            EditorGUI.LabelField( fieldPos, GUIContent.none, guiContent, isValueError ? Resources.FieldErrorStyle : Resources.FieldStyle );

            //todo implement custom icon tinting
            //GUI.color = Color.red;
            //GUI.DrawTexture( new Rect { x = fieldPos.x + 3, y = fieldPos.y + 1f, width = fieldPos.height - 2, height = fieldPos.height - 1 }, Resources.GDRootIcon, ScaleMode.ScaleToFit );
            //GUI.color = Color.white;
            

            //Ping folder on field click support
            if( Event.current.isMouse && Event.current.type == EventType.MouseDown && fieldPos.Contains( Event.current.mousePosition ) && currentObject != null )
            {
                EditorGUIUtility.PingObject( currentObject );
            }

            //Drag and drop folders from Project view support
            if ( (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform ) && fieldPos.Contains( Event.current.mousePosition ) )
            {
                var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                if ( draggedObject is ScriptableObject draggedSO )
                {
                    if ( allowedObjects == null || allowedObjects.Contains( draggedSO ) )
                        if( Event.current.type == EventType.DragUpdated )
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        else
                        {
                            onSelected?.Invoke( draggedSO );
                        }
                }
            }

            //Draw drop down folder browser
            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) || IsPressEnter( controlId ) )
            {
                var gddb    = EditorDB.DB;
                var gddbBrowserContent = new GdDbBrowserPopup( gddb, allowedObjects, allowedFolders, currentObject, propertyPosition, allowNullObject, GdDbBrowserWidget.EMode.Objects, 
                        ( sender, folder, obj ) =>
                        {
                            onSelected?.Invoke( obj );
                        },
                        ( sender, folder, obj ) =>
                        {
                            onSelected?.Invoke( obj );
                            sender.editorWindow.Close();
                        } );

               PopupWindow.Show( propertyPosition, gddbBrowserContent );
            }

            EditorGUI.EndProperty();
        }

        public static void DrawGDFolderField( Rect position, GUIContent label, SerializedProperty property, FieldInfo fieldInfo, GdFolder currentFolder, IReadOnlyList<GdFolder> allowedFolders, Boolean allowNullObject, OnGdFolderSelected onSelected )
        {
            var controlId      = GUIUtility.GetControlID(FocusType.Keyboard, position);

            //Validation
            var isValueError   = false;
            if ( Event.current.type == EventType.Repaint )          //Try to minimize heavy checks
            {
                isValueError = (currentFolder == null && !allowNullObject) || ( currentFolder != null && allowedFolders != null && !allowedFolders.Contains( currentFolder ) );
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
            var guiContent = GetFieldContent( currentFolder, property, fieldInfo );
            EditorGUI.LabelField( fieldPos, GUIContent.none, guiContent, isValueError ? Resources.FieldErrorStyle : Resources.FieldStyle );

            //todo implement custom icon tinting
            //GUI.color = Color.red;
            //GUI.DrawTexture( new Rect { x = fieldPos.x + 3, y = fieldPos.y + 1f, width = fieldPos.height - 2, height = fieldPos.height - 1 }, Resources.GDRootIcon, ScaleMode.ScaleToFit );
            //GUI.color = Color.white;
            

            //Ping folder on field click support
            if( Event.current.isMouse && Event.current.type == EventType.MouseDown && fieldPos.Contains( Event.current.mousePosition ) && currentFolder != null )
            {
                var folderAssetPath = AssetDatabase.GUIDToAssetPath( currentFolder.FolderGuid.ToString( "N" ) );
                var folderAsset     = AssetDatabase.LoadAssetAtPath<DefaultAsset>( folderAssetPath );
                EditorGUIUtility.PingObject( folderAsset );
            }

            //Drag and drop folders from Project view support
            if ( (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform ) && fieldPos.Contains( Event.current.mousePosition ) )
            {
                var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                if ( draggedObject is DefaultAsset folderAsset )
                {
                    var folderGuid     = Guid.Parse( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( folderAsset ) ) );
                    var draggedFolder  = EditorDB.DB.GetFolder( folderGuid );
                    if ( draggedFolder != null && (allowedFolders == null || allowedFolders.Contains( draggedFolder )) )
                        if( Event.current.type == EventType.DragUpdated )
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        else
                            onSelected?.Invoke( draggedFolder );
                }
            }

            //Draw drop down folder browser
            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) || IsPressEnter( controlId ) )
            {
                var gddb    = EditorDB.DB;
                var gddbBrowserContent = new GdDbBrowserPopup( gddb, null, allowedFolders, currentFolder, propertyPosition, allowNullObject, GdDbBrowserWidget.EMode.Folders, 
                        ( sender, folder, obj ) =>
                        {
                            onSelected?.Invoke( folder );
                        },
                        ( sender, folder, obj ) =>
                        {
                            onSelected?.Invoke( folder );
                            sender.editorWindow.Close();
                        } );

               PopupWindow.Show( propertyPosition, gddbBrowserContent );
            }

            EditorGUI.EndProperty();
        }

        private static GUIContent GetFieldContent( ScriptableObject selectedObject, SerializedProperty property, FieldInfo fieldInfo ) 
        {
            if( selectedObject != null )
            {
                var content  = new GUIContent( selectedObject.name, GetObjectIcon( selectedObject ), property.tooltip );
                return content;
            }
            else
            {
                return new GUIContent( $"None ({ObjectNames.NicifyVariableName(fieldInfo.FieldType.Name)})", tooltip: property.tooltip );
            } 
        }

        private static GUIContent GetFieldContent( GdFolder selectedFolder, SerializedProperty property, FieldInfo fieldInfo ) 
        {
            if( selectedFolder != null )
            {
                var content  = new GUIContent( selectedFolder.Name, GetFolderIcon( selectedFolder ), property.tooltip );
                return content;
            }
            else
            {
                return new GUIContent( $"None ({ObjectNames.NicifyVariableName(fieldInfo.FieldType.Name)})", tooltip: property.tooltip );
            } 
        }


        public static Texture2D GetObjectIcon( ScriptableObject selectedObject )
        {
            if ( selectedObject is GDRoot )
                return Icons.GDRootIcon;
            else if ( selectedObject is GDObject )
                return Icons.GDObjectIcon;
            return Icons.SObjectIcon;
        }

        public static Texture2D GetFolderIcon( GdFolder selectedFolder )
        {
            return Icons.FolderIcon;
        }


        private static Boolean IsPressEnter(Int32 myControlId )
        {
            return Event.current.GetTypeForControl( myControlId ) == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter );
        }
        
        public delegate void OnGdObjectSelected( ScriptableObject gdObject );
        public delegate void OnGdFolderSelected( GdFolder gdFolder );

        private static class Resources
        {
            public static readonly GUIStyle  PrefixLabelStyle      = new GUIStyle( EditorStyles.label ) ;
            public static readonly GUIStyle  PrefixLabelErrorStyle = new GUIStyle( PrefixLabelStyle ) { normal   = { textColor = Color.red }, focused = { textColor = Color.red } };
            public static readonly GUIStyle  FieldStyle        = new ( GUI.skin.textField ) { imagePosition  = ImagePosition.ImageLeft, };
            public static readonly GUIStyle  FieldErrorStyle   = new ( FieldStyle ) { normal             = { textColor = Color.red}};
            public static readonly GUIStyle  PickerButton          = EditorStyles.miniButton;

            
        }
    }
}