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
            var filterAttr = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>( ) ;
            if( filterAttr == null )
            {
                var selectedFolder = GetGDFolder( property );
                GdFieldDrawerBase.DrawGDFolderField( position, label, property, fieldInfo, selectedFolder, null, true , newFolder =>
                {
                    SetGDFolder( property, newFolder );
                } );
            }
            else
            {
                //If GdObjectFilterAttribute present, lets GdFilterDrawer handle it
            }
        }

        public static GdFolder GetGDFolder( SerializedProperty property )
        {
            var folderGuid = GuidToLongs.ToGuid( property.FindPropertyRelative( "Part1" ).ulongValue, property.FindPropertyRelative( "Part2" ).ulongValue );
            var editorDB = EditorDB.DB;
            var folder = editorDB.GetFolder( folderGuid );
            return folder;
        }

        public static void SetGDFolder( SerializedProperty property, GdFolder folder )
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
    }
}