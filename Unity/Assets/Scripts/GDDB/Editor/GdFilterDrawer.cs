using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Gddb.Editor
{
    [CustomPropertyDrawer( typeof(GdObjectFilterAttribute) )]
    public class GdFilterDrawer : PropertyDrawer
    {
        private readonly List<ScriptableObject> _resultObjects = new ();
        private readonly List<GdFolder>         _resultFolders = new ();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            if ( property.propertyType == SerializedPropertyType.ObjectReference && typeof(ScriptableObject).IsAssignableFrom(fieldInfo.FieldType) )      //Draw scriptable object with filter attribute
            {
                //var timer = System.Diagnostics.Stopwatch.StartNew();

                //Apply filters
                var    attr         = (GdObjectFilterAttribute)attribute;
                _resultObjects.Clear();
                _resultFolders.Clear();
                var searchResult = EditorDB.DB.FindObjects( attr.Query, _resultObjects, _resultFolders ).FindObjectType(fieldInfo.FieldType);
                if ( attr.ObjectType != null && attr.ObjectType != fieldInfo.FieldType )
                    searchResult.FindObjectType( attr.ObjectType );
                if ( attr.Components != null )
                    searchResult.FindComponents( attr.Components );

                //Debug.Log( $"field {property.name}, instance {GetHashCode()}, event {Event.current.type}, filter time {(timer.Elapsed.TotalMilliseconds * 1000):N} mks" );

                var selectedObject = property.objectReferenceValue as ScriptableObject;
                GdFieldDrawerBase.DrawGDObjectField( position, label, property, fieldInfo, selectedObject, _resultObjects, _resultFolders, attr.AllowNullReference, selectedObj =>
                {
                    property.serializedObject.Update();
                    property.objectReferenceValue = selectedObj;
                    property.serializedObject.ApplyModifiedProperties();
                } );
            }
            else if( property.propertyType == SerializedPropertyType.Generic && typeof(GdRef).IsAssignableFrom( fieldInfo.FieldType ) )         //Draw GdRef with filter attribute
            {
                var  attr  = (GdObjectFilterAttribute)attribute;
                _resultObjects.Clear();
                _resultFolders.Clear();
                var searchResult = EditorDB.DB.FindObjects( attr.Query, _resultObjects, _resultFolders );
                if ( attr.ObjectType != null )
                    searchResult.FindObjectType( attr.ObjectType );
                if ( attr.Components != null )
                    searchResult.FindComponents( attr.Components );

                var selectedObject = GdObjectRefDrawer.GetGDObject( property );
                GdFieldDrawerBase.DrawGDObjectField( position, label, property, fieldInfo, selectedObject, _resultObjects, _resultFolders, attr.AllowNullReference, selectedObj =>
                {
                    property.serializedObject.Update();
                    GdObjectRefDrawer.SetGDObject( property, selectedObj );
                    property.serializedObject.ApplyModifiedProperties();
                } );
            }
            else if ( property.propertyType == SerializedPropertyType.Generic && typeof(GdFolderRef).IsAssignableFrom( fieldInfo.FieldType ) )         //Draw GdFolderRef with filter attribute
            {
                var  attr  = (GdObjectFilterAttribute)attribute;
                _resultFolders.Clear();
                EditorDB.DB.FindFolders( attr.Query, _resultFolders );
                
                var selectedFolder = GdFolderRefDrawer.GetGDFolder( property );
                GdFieldDrawerBase.DrawGDFolderField( position, label, property, fieldInfo, selectedFolder, _resultFolders, attr.AllowNullReference, selectedObj =>
                {
                    property.serializedObject.Update();
                    GdFolderRefDrawer.SetGDFolder( property, selectedObj );
                    property.serializedObject.ApplyModifiedProperties();
                } );
            }
            else
            {
                EditorGUI.LabelField( position, label, new GUIContent($"[{nameof(GdObjectFilterAttribute)}] doesn't supported on field type '{fieldInfo.FieldType.Name}'", Resources.ErrorIcon) );
            }
        }

        public static class Resources
        {
            public static readonly Texture2D ErrorIcon = (Texture2D)EditorGUIUtility.IconContent( "Error" ).image;
        }
    }
}