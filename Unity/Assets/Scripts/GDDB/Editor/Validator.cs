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
            //GDAssetProcessor.GDDBAssetsChanged.Subscribe( 500, OnGDDBChanged );      
            EditorDB.Updated += ( ) => Validate();                      //After editor GDDB updated and Before project window drawer
            GDObjectEditor.Changed += _ => Validate();
            Validate();
        }

        private static readonly List<ValidationReport> _reports = new();

        public static IReadOnlyList<ValidationReport> Validate( )
        {
            var gddb    = EditorDB.DB;
            _reports.Clear();
            if ( gddb == null )                                    //No GDDB in project
                return    _reports;

            var timer            = DateTime.Now;
            int validatedCounter = 0;

            DefaultBDValidations( gddb, _reports );

            foreach ( var folder in gddb.RootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var obj in folder.Objects )
                {
                    DefaultObjectValidations( obj, folder, _reports );
                    validatedCounter++;
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

            Debug.Log( $"[{nameof(Validator)}] Validation taken {validationTime.TotalMilliseconds} ms, validated {validatedCounter} objects, errors {_reports.Count}" );

            Validated?.Invoke( Reports );

            return Reports;
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
    }
}