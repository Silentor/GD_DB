using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    [CustomPropertyDrawer( typeof(GDObject), true )]
    public class GdObjectDrawer : PropertyDrawer
    {
        private readonly List<ScriptableObject> _resultObjects = new ();
        private readonly List<GdFolder>         _resultFolders = new ();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var filterAttr       = fieldInfo.GetCustomAttribute<GdObjectFilterAttribute>();
            if ( filterAttr == null )
            {
                IReadOnlyList<ScriptableObject> filteredObjects = null;
                IReadOnlyList<GdFolder> filteredFolders = null;
                if ( fieldInfo.FieldType != typeof(GDObject) )      //Need filter by field type
                {
                    var db = EditorDB.DB;
                    _resultObjects.Clear();
                    _resultFolders.Clear();
                    db.FindObjects( null, _resultObjects, _resultFolders ).FindObjectType( fieldInfo.FieldType );
                    filteredObjects = _resultObjects;
                    filteredFolders = _resultFolders;
                }
                var selectedGdObject = (GDObject)property.objectReferenceValue;
                GdFieldDrawerBase.DrawGDObjectField( position, label, property, fieldInfo, selectedGdObject, filteredObjects, filteredFolders, true , newObject =>
                {
                    property.serializedObject.Update();
                    property.objectReferenceValue = newObject;
                    property.serializedObject.ApplyModifiedProperties();
                } );
            }
            else
            {
                //If GdObjectFilterAttribute present, lets GdFilterDrawer handle it
            }
        }

   
       
    }
}