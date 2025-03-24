using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GDDB.Validations;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace GDDB.Editor.Validations
{
    /// <summary>
    /// Check database objects for errors. There are default and custom validations (via interface implementation)
    /// </summary>
    [InitializeOnLoad]
    public static class Validator
    {
        public static  IReadOnlyList<ValidationReport> Reports => _reports;

        public static event Action StartValidate; 
        public static event Action<IReadOnlyList<ValidationReport>> Validated; 

        static Validator( )
        {
            EditorDB.Updated       += ( ) => ValidateAsync();       
            GDObjectEditor.Changed += _ => ValidateAsync( TimeSpan.FromSeconds( 0.1 ));    //To react to unsaved GDObject editor changes, but do not mess with fast typing
            
            ValidateAsync();
        }

        private static List<AttributeValidatorData> PrepareAttributeValidation( )
        {
            var timer = DateTime.Now;

            var validators                    = new List<AttributeValidatorData>();
            var validatorsTypes              = TypeCache.GetTypesWithAttribute<CustomPropertyValidator>(  );
            foreach ( var validatorType in validatorsTypes )
            {
                if( validatorType.IsAbstract ) continue;

                //Get validated attribute type
                var attributeType = validatorType.GetCustomAttribute<CustomPropertyValidator>().ValidationAttributeType;

                var validatorData     = new AttributeValidatorData
                                        {
                                                ValidatorType = validatorType,
                                                AttributeType = attributeType,
                                        };

                validators.Add( validatorData );
            }

            //Debug.Log( $"[{nameof(Validator)}]-[{nameof(PrepareAttributeValidation)}] time {(DateTime.Now-timer).TotalMilliseconds:N1}" );

            return validators;
        }

        private static readonly List<ValidationReport>      _reports = new();
        private static readonly GDObjectValidatorVisitor    GDObjectAttributeValidatorVisitor = new ( _reports, PrepareAttributeValidation() );

        private static EditorCoroutine                 _validateAsyncCoroutine;


        public static IReadOnlyList<ValidationReport> Validate( )
        {
            var gddb    = EditorDB.DB;
            _reports.Clear();
            if ( gddb == null )                                    //No GDDB in project
                return    _reports;

            if ( _validateAsyncCoroutine != null )
            {
                EditorCoroutineUtility.StopCoroutine( _validateAsyncCoroutine );
                _validateAsyncCoroutine = null;
            }

            StartValidate?.Invoke();

            var timer            = DateTime.Now;
            int validatedCounter = 0;

            DefaultBDValidations( gddb, _reports );

            foreach ( var objData in gddb.AllObjects )
            {
                DefaultObjectValidations( objData.Object, objData.Folder, _reports );

                //Check validation attributes on gd object fields
                GDObjectAttributeValidatorVisitor.Iterate( objData.Object, objData.Folder );

                validatedCounter++;
            }

            var validationTime = DateTime.Now - timer;

            Debug.Log( $"[{nameof(Validator)}]-[{nameof(Validate)}] Validated {validatedCounter} objects, errors {_reports.Count}, time {validationTime.TotalMilliseconds} ms" );

            Validated?.Invoke( Reports );

            return Reports;
        }

        public static void ValidateAsync( TimeSpan startDelay = default )
        {
            if( _validateAsyncCoroutine != null )
                EditorCoroutineUtility.StopCoroutine( _validateAsyncCoroutine );
            _validateAsyncCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless( ValidateAsyncInternal( startDelay ) );
        }

        private static IEnumerator ValidateAsyncInternal( TimeSpan startDelay = default )
        {
            var gddb    = EditorDB.DB;
            if ( gddb == null )                                    //No GDDB in project
                yield break;

            yield return new EditorWaitForSeconds( (float)startDelay.TotalSeconds );

            _reports.Clear();
            var timer            = Stopwatch.StartNew();
            int validatedCounter = 0;

            DefaultBDValidations( gddb, _reports );

            foreach ( var objData in gddb.AllObjects )
            {
                timer.Start();

                DefaultObjectValidations( objData.Object, objData.Folder, _reports );

                try
                {
                    //Check validation attributes on gd object fields
                    GDObjectAttributeValidatorVisitor.Iterate( objData.Object, objData.Folder );
                }
                catch ( Exception e )
                {
                    _reports.Add( new ValidationReport( objData.Folder, objData.Object, $"Exception while validated gd object, object skipped: {e}" ) );
                }

                validatedCounter++;

                timer.Stop();

                yield return null;
            }

            _validateAsyncCoroutine = null;

            Debug.Log( $"[{nameof(ValidateAsyncInternal)}]-[{nameof(Validate)}] async Validated {validatedCounter} objects, errors {_reports.Count}, time {timer.ElapsedMilliseconds} ms" );

            Validated?.Invoke( Reports );
        }

        private static void DefaultBDValidations( GdDb db, List<ValidationReport> reports )
        {
            if( EditorDB.RootFolderPath == "Assets" )
                reports.Add( new ValidationReport( db.RootFolder, db.RootFolder.Objects.OfType<GDRoot>().First(), "Placing GDRoot object to Assets folders make all your assets like a game design data base. Its a bad idea, please select some subfolder for your game data base files." ) );
        }

        private static void DefaultObjectValidations( ScriptableObject obj, GdFolder folder, List<ValidationReport> reports )
        {
            if( obj is GDObject gdObject ) 
                CheckMissedComponentsValidation( gdObject, folder, reports );
        }

        private static void CheckMissedComponentsValidation( GDObject gdo, GdFolder folder, List<ValidationReport> reports )
        {
            var so        = new SerializedObject( gdo );
            var compsProp = so.FindProperty( "Components" );
            for ( int i = 0; i < compsProp.arraySize; i++ )
            {
                var compProp = compsProp.GetArrayElementAtIndex( i );
                if( compProp.managedReferenceValue == null )
                {
                    reports.Add( new ValidationReport( folder,  gdo, $"Component at index {i} is null or missed reference" ) );
                }
            }
        }

        // private struct TypeWithFields
        // {
        //     public Type        GDObjectType;
        //     public FieldInfo[] FieldsToCheck;
        //     public Attribute[] Attributes;
        //     public Boolean[]   IsCollectionAttribute;
        // }

        public struct AttributeValidatorData
        {
            public Type                         ValidatorType;
            public Type                         AttributeType;
            public BaseAttributeValidator       ValidatorInstance;
            //public TypeWithFields[]             ProcessedTypes;
        }
    }
}