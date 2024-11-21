using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [InitializeOnLoad]
    public static class Validator
    {
        public static IReadOnlyList<ValidationReport> Reports => _reports;

        public static event Action<IReadOnlyList<ValidationReport>> Validated; 

        static Validator( )
        {
            GDAssets.GDDBAssetsChanged.Subscribe( 500, OnGDDBChanged );      //After editor GDDB updated and Before project window drawer
            OnGDDBChanged( Array.Empty<GDObject>(), ArraySegment<String>.Empty );
        }

        private static readonly List<ValidationReport> _reports = new();

        private static void OnGDDBChanged(IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            var gddb    = GDBEditor.GDB;
            _reports.Clear();                     
            var timer = DateTime.Now;
            int validatedCounter = 0;

            foreach ( var folder in gddb.RootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdo in folder.Objects )
                {
                    if ( gdo.EnabledObject )
                    {
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
        }

        private static void DefaultGDOValidations( GDObject gdo, Folder folder, List<ValidationReport> reports )
        {
             CheckMissedComponentsVaslidation( gdo, folder, reports );
        }

        private static void CheckMissedComponentsVaslidation( GDObject gdo, Folder folder, List<ValidationReport> reports )
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