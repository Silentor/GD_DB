using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

namespace GDDB.Editor.Validations
{
    [RequireDerived]
    public abstract class BaseAttributeValidator<TAttribute> : BaseAttributeValidator where TAttribute : Attribute
    {
        public FieldInfo  fieldInfo { get; private set; }
        public TAttribute attribute { get; private set; } 

        //public abstract void ValidateField( ScriptableObject gdObject, GdFolder folder, TAttribute attribute, FieldInfo field, List<ValidationReport> reports );
        public abstract void ValidateField( SerializedProperty property, ScriptableObject gdObject, GdFolder folder, List<ValidationReport> reports );

        internal override void ValidateFieldInternal( SerializedProperty property, Attribute attribute, FieldInfo field, ScriptableObject gdObject, GdFolder folder, List<ValidationReport> reports )
        {
            fieldInfo = field;
            attribute = (TAttribute)attribute;
            ValidateField( property, gdObject, folder, reports );
        }
    }

    public abstract class BaseAttributeValidator
    {
        internal abstract void ValidateFieldInternal( SerializedProperty     property, Attribute attribute, FieldInfo field, ScriptableObject gdObject,
                                                      GdFolder               folder,
                                                      List<ValidationReport> reports );
        
    }
}