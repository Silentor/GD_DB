using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Check database objects for errors. There are default and custom validations (via interface implementation)
    /// </summary>
    [InitializeOnLoad]
    public static class Validator
    {
        public static IReadOnlyList<ValidationReport> Reports => _reports;

        public static event Action<IReadOnlyList<ValidationReport>> Validated; 

        static Validator( )
        {
            GDAssets.GDDBAssetsChanged.Subscribe( 500, OnGDDBChanged );      //After editor GDDB updated and Before project window drawer
            OnGDDBChanged( Array.Empty<GDObject>(), ArraySegment<String>.Empty );
            GDObjectEditor.Changed += _ => Validate();
        }

        private static readonly List<ValidationReport> _reports = new();

        public static IReadOnlyList<ValidationReport> Validate( )
        {
            var gddb    = GDBEditor.DB;
            _reports.Clear();

            if (  gddb == null )                                    //No GDDB in project
                return Array.Empty<ValidationReport>();

            var timer            = DateTime.Now;
            int validatedCounter = 0;

            foreach ( var folder in gddb.RootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var obj in folder.Objects )
                {
                    if ( obj is GDObject gdo )
                    {
                        if ( !gdo.EnabledObject ) continue;
                        DefaultGDOValidations( gdo, folder, _reports );
                        validatedCounter++;
                    }
                }
            }

            var validationTime = DateTime.Now - timer;

            // if ( _reports.Any() )
            // {
            //     foreach ( var report in _reports )
            //     {
            //         Debug.LogError( $"[{nameof(Validator)}] Error at {report.Folder.GetPath()}/{report.GdObject.Name}: {report.Message}", report.GdObject );
            //     }
            // }

            Debug.Log( $"[{nameof(Validator)}] Validation taken {validationTime.Milliseconds} ms, validated {validatedCounter} objects, errors {_reports.Count}" );

            Validated?.Invoke( Reports );

            return Reports;
        }

        private static void OnGDDBChanged(IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            Validate();
        }

        private static void DefaultGDOValidations( GDObject gdo, GdFolder folder, List<ValidationReport> reports )
        {
             CheckMissedComponentsValidation( gdo, folder, reports );
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
    }
}