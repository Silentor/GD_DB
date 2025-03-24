using System;
using System.Collections.Generic;
using System.Reflection;
using GDDB.Validations;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor.Validations
{
    public class GDObjectValidatorVisitor : SerializedObjectVisitor
    {
        private readonly List<ValidationReport>                          _reports;
        private readonly IReadOnlyList<Validator.AttributeValidatorData> _validatorsCache;
        private          GdFolder                                        _folder;

        public GDObjectValidatorVisitor( List<ValidationReport> reports, IReadOnlyList<Validator.AttributeValidatorData> validatorsCache )
        {
            _reports              = reports;
            _validatorsCache = validatorsCache;
        }

        /// <summary>
        /// Iterate over gd object and its compopnents. Validate marked fields
        /// </summary>
        /// <param name="gdobj"></param>
        /// <param name="folder"></param>
        public void Iterate( ScriptableObject gdobj, GdFolder folder )
        {
            //Setup context
            _folder = folder;

            var so = new SerializedObject( gdobj );
            Iterate( so );

            if ( gdobj is GDObject )
            {
                var componentsProp = so.FindProperty( "Components" );
                for ( var i = 0; i < componentsProp.arraySize; i++ )
                {
                    var compProp = componentsProp.GetArrayElementAtIndex( i );
                    IterateObject( compProp, compProp.managedReferenceValue.GetType() );
                }
            }

        }

        protected override EVisitResult VisitProperty(SerializedProperty prop, FieldInfo fieldInfo )
        {
            if( fieldInfo.DeclaringType == typeof(GDObject ) && fieldInfo.Name == "Components" )
                return EVisitResult.SkipChildren;

            var attribs = fieldInfo.GetCustomAttributes();
            foreach ( var attrib in attribs )
            {
                foreach ( var validatorData in _validatorsCache )
                {
                    if ( validatorData.AttributeType == attrib.GetType() )
                    {
                        var validator = validatorData.ValidatorInstance ?? (BaseAttributeValidator)Activator.CreateInstance( validatorData.ValidatorType );
                        validator.ValidateFieldInternal( prop, attrib, fieldInfo, (ScriptableObject)prop.serializedObject.targetObject, _folder, _reports );
                    }
                }
            }

            return EVisitResult.Continue;
        }
    }
}