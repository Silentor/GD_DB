﻿using System;
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
            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
  
            label    = EditorGUI.BeginProperty( position, label, property );
            position = EditorGUI.PrefixLabel( position, controlId, label );
            var propertyPosition = position;

            var fieldPos = position;
            fieldPos.width -= 20;
            var dropdownBtnPos = position;
            dropdownBtnPos.xMin = fieldPos.xMax;

            var selectedFolder     = GetGDFolder( property );
            var folderLabelContent = GetFolderControlContent( selectedFolder );
            EditorGUI.LabelField( fieldPos, GUIContent.none, folderLabelContent, Resources.FolderBoxStyle );

            if( Event.current.isMouse && Event.current.type == EventType.MouseDown && fieldPos.Contains( Event.current.mousePosition ) && selectedFolder != null )
            {
                var folderAssetPath = AssetDatabase.GUIDToAssetPath( selectedFolder.FolderGuid.ToString( "N" ) );
                var folderAsset     = AssetDatabase.LoadAssetAtPath<DefaultAsset>( folderAssetPath );
                EditorGUIUtility.PingObject( folderAsset );
            }

            if ( GUI.Button( dropdownBtnPos, "\u02c5", Resources.PickerButton ) || IsPressEnter( controlId ) )
            {
                var gddb               = GDDBEditor.DB;     
                var filterAttr         = fieldInfo.GetCustomAttribute( typeof(GdTypeFilterAttribute) ) ;
                var query              = (filterAttr as GdTypeFilterAttribute)?.Query;
                var components         = (filterAttr as GdTypeFilterAttribute)?.Components;
                var gddbBrowserContent = new GdDbBrowserPopupWindowContent( gddb, query, components, selectedFolder, propertyPosition, GdDbBrowserWidget.EMode.Folders, 
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
            if ( folder.FolderGuid != Guid.Empty )
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

        private static class Resources
        {
            public static readonly GUIStyle  FolderBoxStyle = new ( GUI.skin.textField ) { imagePosition = ImagePosition.ImageLeft };
            public static readonly GUIStyle  PickerButton   = EditorStyles.miniButton;
            public static readonly Texture2D FolderIcon     = UnityEngine.Resources.Load<Texture2D>( "folder_24dp" );
        }
    }
}