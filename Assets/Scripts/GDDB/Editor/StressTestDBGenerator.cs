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
        private string[]           _types;

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

            var generateScriptsBtn = new Button( ( ) => GenerateComponents( settings ) ) { text = "Generate GDComponents" };
            rootVisualElement.Add( generateScriptsBtn );
            var generateObjectsBtn = new Button( ( ) => GenerateGDObjects( settings ) ) { text = "Generate GDObjects" };
            rootVisualElement.Add( generateObjectsBtn );

            _nouns = (Resources.Load( "NounsList" ) as TextAsset).text.Split( "\r\n" );
            _verbs = (Resources.Load( "VerbsList" ) as TextAsset).text.Split( "\r\n" );
            _types = (Resources.Load( "TypesList" ) as TextAsset).text.Split( "\r\n" );

            if ( _nouns.Length < 10 || _verbs.Length < 10 )
                Debug.LogError( "Nouns or Verbs list is too small" );
        }

        private void GenerateComponents( StressTestSettings settings )
        {
            _settings = settings;
            var rnd = new Random();

            var namespaces = GenerateNamespaces(  );
            var classNames = GenerateClassNames(  );

            if ( !Directory.Exists( _settings.OutputFolderScripts ) )
            {
                var dir = Directory.CreateDirectory( _settings.OutputFolderScripts );
                Debug.Log( dir.FullName );
            }

            var componentTypeNames = new List<String>();
            foreach ( var className in classNames )
            {
                var ns            = namespaces[ rnd.Next( namespaces.Count ) ];
                var componentCode = GenerateComponentScript( ns, className );
                var path          = Path.Join( _settings.OutputFolderScripts, $"{className}.cs" );
                File.WriteAllText( path, componentCode );
                componentTypeNames.Add( $"{ns}.{className}" );
            }

            AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );

            //Never gets here 
            //Wait for compiling and domain reload
            //EditorApplication.isCompiling


            
        }

        private void GenerateGDObjects( StressTestSettings settings )
        {
            var rnd = new Random();
            var gdos  = new List<GDObject>();
            var names = GenerateUniqueNouns( _settings.GDObjectsCount, new List<String>(), 2, 3 );
            var components = TypeCache.GetTypesDerivedFrom<GDComponent>().Where( t => t.Namespace.StartsWith( settings.RootNamespace ) && !t.IsAbstract ).ToList();
            while ( gdos.Count < _settings.GDObjectsCount )
            {
                var go = ScriptableObject.CreateInstance<GDObject>();
                go.name       = names[ rnd.Next( names.Count ) ];
                go.Components = new List<GDComponent>();

                var componentsCount = rnd.Next( 1, 5 + 1 );
                for ( var i = 0; i < componentsCount; i++ )
                {
                    var componentType = components[ rnd.Next( components.Count ) ];
                    var component         = (GDComponent)Activator.CreateInstance( componentType );
                    go.Components.Add( component );
                }

                gdos.Add( go );
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach ( var gdo in gdos )
                {
                    AssetDatabase.CreateAsset( gdo, Path.Join( _settings.OutputFolderDB, $"{gdo.name}.asset" ) );
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        

        private IReadOnlyList<String> GenerateClassNames(  )
        {
            return GenerateUniqueNouns( _settings.ComponentScriptsCount, new List<String>(), 2, 3 );
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
            var members = GenerateMembers( componentName );
            members.ForEach( m => result.AppendLine( $"        {m}" ) );
            result.AppendLine( "    }" );
            result.AppendLine( "}" );

            return result.ToString();
        }

        private List<String> GenerateMembers( String className )
        {
            var rnd = new Random();
            var result = new List<String>();

            var fieldsCount = rnd.Next( 1, 5 + 1 );
            var fieldNames = GenerateUniqueNouns( fieldsCount, new String[]{className}, 1, 3 );

            foreach ( var fieldName in fieldNames )
            {
                var fieldType = _types[ rnd.Next( _types.Length ) ];
                result.Add( $"public {fieldType} {fieldName};" );
            }

            return result;
        }

        private List<String> GenerateUniqueNouns( Int32 count, IReadOnlyList<String> excludeList, Int32 minWordsCount, Int32 maxWordsCount )
        {
            var result = new List<String>( count );
            var rnd    = new Random();

            while( result.Count < count )
            {
                var nameLength = rnd.Next( minWordsCount, maxWordsCount + 1 );
                var partsList   = new List<String>( nameLength );

                while( partsList.Count < nameLength )
                {
                    var namePart = _nouns[ rnd.Next( _nouns.Length ) ];
                    if( !partsList.Contains( namePart ) )
                        partsList.Add( namePart );
                }

                var fullName = String.Join( "", partsList );
                if ( !result.Contains( fullName ) && !excludeList.Contains( fullName ) )
                    result.Add( fullName );
            }

            return result;
        }
    }
}