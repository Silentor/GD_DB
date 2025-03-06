using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdFolderRef) )]
    public class GdFolderRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var controlId      = GUIUtility.GetControlID(FocusType.Keyboard, position);

            var isValueError   = false;
            var selectedFolder = GetGDFolder( property );
            if ( Event.current.type == EventType.Repaint )          //Try to minimize heavy checks
            {
                var allowedFolders     = GetQueriedFolders();
                var isAllowedNullValue = IsAllowedNullReference();
                isValueError       = (selectedFolder == null && !isAllowedNullValue) || ( allowedFolders != null && !allowedFolders.Contains( selectedFolder ) );
            }

            label    = EditorGUI.BeginProperty( position, label, property );
            position = EditorGUI.PrefixLabel( position, controlId, label, isValueError ? Resources.PrefixLabelErrorStyle : Resources.PrefixLabelStyle );
            var propertyPosition = position;

            var fieldPos = position;
            fieldPos.width -= 20;
            var dropdownBtnPos = position;
            dropdownBtnPos.xMin = fieldPos.xMax;
            
            var folderLabelContent = GetFolderControlContent( selectedFolder );
            EditorGUI.LabelField( fieldPos, GUIContent.none, folderLabelContent, isValueError ? Resources.FolderBoxErrorStyle : Resources.FolderBoxStyle );

            //Ping folder on click support
            if( Event.current.isMouse && Event.current.type == EventType.MouseDown && fieldPos.Contains( Event.current.mousePosition ) && selectedFolder != null )
            {
                var folderAssetPath = AssetDatabase.GUIDToAssetPath( selectedFolder.FolderGuid.ToString( "N" ) );
                var folderAsset     = AssetDatabase.LoadAssetAtPath<DefaultAsset>( folderAssetPath );
                EditorGUIUtility.PingObject( folderAsset );
            }

            //Drag and drop folders from Project view support
            if ( (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform ) && fieldPos.Contains( Event.current.mousePosition ) )
            {
                var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                if ( draggedObject is DefaultAsset folderAsset )
                {
                    var folderGuid = Guid.Parse( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( folderAsset ) ) );
                    var draggedFolder = GDDBEditor.DB.GetFolder( folderGuid );
                    var allowedFolders = GetQueriedFolders();
                    if ( draggedFolder != null && (allowedFolders == null || allowedFolders.Contains( draggedFolder )) )
                        if( Event.current.type == EventType.DragUpdated )
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        else
                            SetGDFolder( property, draggedFolder );
                }
            }

            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) || IsPressEnter( controlId ) )
            {
                var gddb               = GDDBEditor.DB;     
                var gddbBrowserContent = new GdDbBrowserPopupWindowContent( gddb, null, GetQueriedFolders(), selectedFolder, propertyPosition, IsAllowedNullReference(), GdDbBrowserWidget.EMode.Folders, 
                        ( sender, folder, _) =>
                        {
                            SetGDFolder( property, folder );
                        },
                        ( sender, folder, _) =>
                        {
                            SetGDFolder( property, folder );
                            sender.editorWindow.Close();
                        } );

               PopupWindow.Show( propertyPosition, gddbBrowserContent );
            }

            EditorGUI.EndProperty();
        }

        private static Boolean IsPressEnter(Int32 myControlId )
        {
            return Event.current.GetTypeForControl( myControlId ) == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter );
        }

        private GdFolder GetGDFolder( SerializedProperty property )
        {
            var folderGuid = GuidToLongs.ToGuid( property.FindPropertyRelative( "Part1" ).ulongValue, property.FindPropertyRelative( "Part2" ).ulongValue );
            var editorDB = GDDBEditor.DB;
            var folder = editorDB.GetFolder( folderGuid );
            return folder;
        }

        private GUIContent GetFolderControlContent( GdFolder selectedFolder ) 
        {
            if( selectedFolder != null )
            {
                var content = new GUIContent( selectedFolder.Name, Resources.FolderIcon );
                return content;
            }
            else
            {
                return new GUIContent( "None" );
            } 
        }

        private void SetGDFolder( SerializedProperty property, GdFolder folder )
        {
            if ( folder != null && folder.FolderGuid != Guid.Empty )
            {
                var (part1, part2)  = GuidToLongs.ToLongs( folder.FolderGuid ); 
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

        private IReadOnlyList<GdFolder> GetQueriedFolders( )
        {
            var filterAttr = fieldInfo.GetCustomAttribute( typeof(GdObjectFilterAttribute) ) ;
            if ( filterAttr == null )
                return null;

            var query      = (filterAttr as GdObjectFilterAttribute)?.Query;
            var editorDB   = GDDBEditor.DB;
            var resultFolders = new List<GdFolder>();
            editorDB.FindFolders( query, resultFolders );
            return resultFolders;
        }

        private Boolean IsAllowedNullReference( )
        {
            var filterAttr = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>( ) ;
            if ( filterAttr == null )
                return true;

            return filterAttr.AllowNullReference;
        }

        private static class Resources
        {
            public static readonly GUIStyle  PrefixLabelStyle      = new GUIStyle( EditorStyles.label ) ;
            public static readonly GUIStyle  PrefixLabelErrorStyle = new GUIStyle( PrefixLabelStyle ) { normal   = { textColor = Color.red }, focused = { textColor = Color.red } };
            public static readonly GUIStyle  FolderBoxStyle        = new ( GUI.skin.textField ) { imagePosition  = ImagePosition.ImageLeft };
            public static readonly GUIStyle  FolderBoxErrorStyle   = new ( FolderBoxStyle ) { normal             = { textColor = Color.red}};
            public static readonly GUIStyle  PickerButton          = EditorStyles.miniButton;
            public static readonly Texture2D FolderIcon            = UnityEngine.Resources.Load<Texture2D>( "folder_24dp" );
        }
    }
}