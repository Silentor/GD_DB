using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

namespace GDDB.Editor.Validations
{
    public abstract class BaseAttributeValidator 
    {
        public FieldInfo  fieldInfo { get; private set; }
        public Attribute  attribute { get; private set; }

        public virtual Boolean IsValidAtCollectionLevel => false;           //Imitate Unity drawer attributes behaviour
        public virtual Boolean IsValidAtItemLevel       => true;            //Imitate Unity drawer attributes behaviour

        protected abstract void ValidateField( SerializedProperty property, ScriptableObject gdObject, GdFolder folder, List<ValidationReport> reports );

        internal void ValidateFieldInternal( SerializedProperty property, Attribute attribute, FieldInfo field, ScriptableObject gdObject, GdFolder folder, List<ValidationReport> reports )
        {
            fieldInfo = field;
            attribute = attribute;
            ValidateField( property, gdObject, folder, reports );
        }
    }

}