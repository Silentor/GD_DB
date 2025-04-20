using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    [CustomPropertyDrawer( typeof(GdRef) )]
    public class GdObjectRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var filterAttr = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>( ) ;
            if ( filterAttr == null )
            {
                 var selectedObject = GetGDObject( property );
                 GdFieldDrawerBase.DrawGDObjectField( position, label, property, fieldInfo, selectedObject, null, null, true , newObject =>
                 {
                     SetGDObject( property, newObject );
                 } );
            }
            else
            {
                //If GdObjectFilterAttribute present, lets GdFilterDrawer handle it
            }
        }

        public static ScriptableObject GetGDObject( SerializedProperty property )
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

        public static void SetGDObject( SerializedProperty property, ScriptableObject gdObject )
        {
            if ( gdObject )
            {
                var (part1, part2) = GuidToLongs.ToLongs( EditorDB.GetGDObjectGuid( gdObject ) ); 
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