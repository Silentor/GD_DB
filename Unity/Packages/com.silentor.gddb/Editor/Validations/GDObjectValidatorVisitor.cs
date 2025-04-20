using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gddb.Validations;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor.Validations
{
    public class GDObjectValidatorVisitor : SerializedObjectVisitor
    {
        private readonly List<ValidationReport>                          _reports;
        private readonly IReadOnlyList<Validator.AttributeValidatorData> _validatorsCache;
        private          GdFolder                                        _folder;

        public GDObjectValidatorVisitor( List<ValidationReport> reports, IReadOnlyList<Validator.AttributeValidatorData> validatorsCache )
        {
            _reports         = reports;
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
        }

        protected override EVisitResult VisitProperty(SerializedProperty prop, FieldInfo fieldInfo, EFieldKind fieldKind )
        {
            var attribs = fieldInfo.GetCustomAttributes().ToArray();
            foreach ( var attrib in attribs )
            {
                foreach ( var validatorData in _validatorsCache )
                {
                    if ( validatorData.AttributeType == attrib.GetType() )
                    {
                        var validator = validatorData.ValidatorInstance ?? (BaseAttributeValidator)Activator.CreateInstance( validatorData.ValidatorType );
                        if ( CheckCollectionApplicability( fieldKind, validator ) )
                        {
                            validator.ValidateFieldInternal( prop, attrib, fieldInfo, (ScriptableObject)prop.serializedObject.targetObject, _folder, _reports );
                        }
                    }
                }
            }

            return EVisitResult.Continue;
        }

        private static Boolean CheckCollectionApplicability( EFieldKind fieldKind, BaseAttributeValidator validator )
        {
            if ( fieldKind == EFieldKind.Collection )
                return validator.IsValidAtCollectionLevel;
            return validator.IsValidAtItemLevel;
        }

    }
}