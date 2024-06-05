using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;
using Random = System.Random;

namespace GDDB.Editor
{
    public class StressTestDBGenerator : EditorWindow
    {
        private String[]           _nouns;
        private String[]           _verbs;
        private StressTestSettings _settings;

        [MenuItem( "GDDB/Generate GDDB" )]
        private static void ShowWindow( )
        {
            // if ( !Directory.Exists( "Assets/TestFile/" ) )
            //     Directory.CreateDirectory( "Assets/TestFile/" );
            // File.AppendAllText( "Assets/TestFile/file.txt", "test text" );
            // AssetDatabase.Refresh();
            // return;

            var window = GetWindow<StressTestDBGenerator>();
            window.titleContent = new GUIContent( "GDDB Generator" );
            window.Show();
        }

        private void CreateGUI( )
        {
            var settings = StressTestSettings.instance;
            settings.Save();
            var so       = new SerializedObject( settings );

            var settingsWidget = new InspectorElement( so );
            rootVisualElement.Add( settingsWidget );

            var startBtn = new Button( ( ) => GenerateDB( settings ) ) { text = "Generate DB" };
            rootVisualElement.Add( startBtn );

            _nouns = (Resources.Load( "NounsList" ) as TextAsset).text.Split( "\r\n" );
            _verbs = (Resources.Load( "VerbsList" ) as TextAsset).text.Split( "\r\n" );

            if ( _nouns.Length < 10 || _verbs.Length < 10 )
                Debug.LogError( "Nouns or Verbs list is too small" );
        }

        private void GenerateDB(StressTestSettings settings )
        {
            _settings = settings;
            var rnd = new Random();

            var namespaces = GenerateNamespaces(  );
            var classNames = GenerateClassNames(  );

            if ( !Directory.Exists( _settings.OutputFolder ) )
            {
                var dir = Directory.CreateDirectory( _settings.OutputFolder );
                Debug.Log( dir.FullName );
            }

            foreach ( var className in classNames )
            {
                var ns            = namespaces[ rnd.Next( namespaces.Count ) ];
                var componentCode = GenerateComponentScript( ns, className );
                var path          = Path.Join( _settings.OutputFolder, $"{className}.cs" );
                File.WriteAllText( path, componentCode );
            }

            AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
        }

        private IReadOnlyList<String> GenerateNamespaces( )
        {
            var rnd    = new Random();
            var result = new List<String>( _settings.ComponentNamespacesCount );
            result.Add( _settings.RootNamespace );

            while ( result.Count < _settings.ComponentNamespacesCount )
                for ( var i = 0; i < 10; i++ )
                    GenerateNamespaceRecursive( _settings.RootNamespace, 1, rnd, result );

            return result;
        }

        private IReadOnlyList<String> GenerateClassNames(  )
        {
            var rnd    = new Random();
            var result = new List<String>( _settings.ComponentScriptsCount );

            while ( result.Count < _settings.ComponentScriptsCount )
            {
                var nameLength = rnd.Next( 2, 3 + 1 );
                var nameList   = new List<String>( nameLength );

                for ( var j = 0; j < nameLength; j++ )
                {
                    var name = String.Empty;
                    while ( nameList.Contains( name = _nouns[ rnd.Next( _nouns.Length ) ] ) )
                        ;
                    nameList.Add( name );
                }

                var className = String.Join( "", nameList );
                if ( !result.Contains( className ) )
                    result.Add( className );
            }

            return result;
        }

        private void GenerateNamespaceRecursive( String ns, Int32 depth, Random rnd, List<String> result )
        {
            if ( result.Count > _settings.ComponentNamespacesCount )
                return;

            if ( rnd.NextDouble() < depth / 4 )    //Discard too deep namespaces
                return;

            String name;
            while ( ns.Contains( name = _nouns[ rnd.Next( _nouns.Length ) ] ) )
                ;
            ns = String.Concat( ns, ".", name );

            if ( !result.Contains( ns ) )
            {
                result.Add( ns );
                var childsCount = rnd.Next( 1, 3 + 1 );
                for ( var i = 0; i < childsCount; i++ ) GenerateNamespaceRecursive( ns, depth + 1, rnd, result  );
            }
        }

        private String GenerateComponentScript( String namespaceName, String componentName )
        {
            var result = new StringBuilder();
            result.AppendLine( "using GDDB;" );
            result.AppendLine( $"namespace {namespaceName}" );
            result.AppendLine( "{" );
            result.AppendLine( $"    public class {componentName} : GDComponent" );
            result.AppendLine( "    {" );
            result.AppendLine( "    }" );
            result.AppendLine( "}" );

            return result.ToString();
        }
    }
}