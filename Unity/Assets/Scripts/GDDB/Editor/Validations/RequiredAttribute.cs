﻿using System;
using System.Collections.Generic;
using GDDB.Validations;
using UnityEngine;

namespace GDDB.Editor.Validations
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            var isNull = (property.propertyType    == SerializedPropertyType.ObjectReference  && !property.objectReferenceValue) 
                         || (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue == null)
                         || (property.propertyType == SerializedPropertyType.String && String.IsNullOrEmpty( property.stringValue )
                             || (property.isArray && property.arraySize == 0 ));

            var oldColor                           = GUI.color;
            if ( isNull )                
                GUI.color = new Color( 1, 0, 0, 1 );

            EditorGUI.PropertyField( position, property, label );

            if ( isNull )
            {
                GUI.DrawTexture( new Rect( position.x + EditorGUIUtility.labelWidth - 20, position.y, 18, 18 ), Resources.ErrorIcon );
                GUI.color = oldColor;
            }
        }

        private static class Resources
        {
            public static readonly Texture2D ErrorIcon = EditorGUIUtility.Load( "d_console.erroricon" ) as Texture2D;
        }
    }

    public class RequiredAttributeValidator : BaseAttributeValidator<RequiredAttribute>
    {
        public override void ValidateField(SerializedProperty property, ScriptableObject gdObject, GdFolder folder, List<ValidationReport> reports )
        {
            var isNull = (property.propertyType    == SerializedPropertyType.ObjectReference  && !property.objectReferenceValue) 
                         || (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue == null)
                         || (property.propertyType == SerializedPropertyType.String && String.IsNullOrEmpty( property.stringValue )
                         || (property.isArray && property.arraySize == 0 ));

            if( isNull )
                reports.Add( new ValidationReport( folder, gdObject, $"Field '{fieldInfo}' marked as Required but do not has value. Property path {property.propertyPath}" ) );
        }
    }
    
}
