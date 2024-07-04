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
            var generateCategoriesBtn = new Button( ( ) => GenerateCategories( settings ) ) { text = "Generate categories" };
            rootVisualElement.Add( generateCategoriesBtn );
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

            if ( !Directory.Exists( _settings.OutputFolderComponents ) )
            {
                Directory.CreateDirectory( _settings.OutputFolderComponents );
            }

            var componentTypeNames = new List<String>();
            foreach ( var className in classNames )
            {
                var ns            = namespaces[ rnd.Next( namespaces.Count ) ];
                var componentCode = GenerateComponentScript( ns, className );
                var path          = Path.Join( _settings.OutputFolderComponents, $"{className}.cs" );
                File.WriteAllText( path, componentCode );
                componentTypeNames.Add( $"{ns}.{className}" );
            }

            AssetDatabase.Refresh();
        }

        private void GenerateCategories( StressTestSettings settings )
        {
            var rnd = new Random();
             var enumNames = GenerateEnumNames( settings.CategoriesCount );
             
             var rootCategory = GenerateCategory( enumNames[0], 0, 4, rnd.Next( 8, 15 ), rnd );
             var itemsCounter = rootCategory.Items.Count;
             var categories   = new List<CategoryHolder>(  ) { rootCategory, };
             var infinityLoopCounter = 0;

             for ( int i = 1; i < settings.CategoriesCount; i++ )
             {
                 var parent   = categories[ rnd.Next( categories.Count ) ];
                 var freeItems = parent.Items.Where( i => i.Subcategory == null ).ToList();
                 if( freeItems.Any() && parent.Depth < 3 )
                 {
                     var parentItem   = freeItems[ rnd.Next( freeItems.Count ) ];
                     var remainsItems = Math.Max( (settings.CategoryItemsCount - itemsCounter) / (settings.CategoriesCount - categories.Count), 1 );
                     var itemsCount   = rnd.Next( Math.Max( remainsItems - remainsItems / 2, 1 ), remainsItems + remainsItems / 2 );
                     var category     = GenerateCategory( enumNames[ i ], parent.Depth + 1, 4, itemsCount, rnd );
                     category.Parent = parentItem;
                     parentItem.Subcategory = category;
                     itemsCounter += category.Items.Count;
                     categories.Add( category );
                 }
                 else
                 {
                     i--;
                     if ( infinityLoopCounter++ > 10000 )
                     {
                         Debug.LogError( $"Infinity loop defence abort GenerateCategories()" );
                         break;
                     }

                 }
             }

             //Save hierarchy to file
             if ( !Directory.Exists( settings.OutputFolderCategories ) )
             {
                 Directory.CreateDirectory( settings.OutputFolderCategories );
             }
             var str = GenerateCategoriesString( categories, $"{settings.RootNamespace}.Categories" );
             var categoriesFilePath = Path.Join( settings.OutputFolderCategories, "Categories.cs" );
             File.WriteAllText( categoriesFilePath, str );

             AssetDatabase.Refresh();

             Debug.Log( $"Generated categories {categories.Count}, category items {itemsCounter}" );
        }

        private void GenerateGDObjects( StressTestSettings settings )
        {
            var rnd = new Random();
            var gdos  = new List<GDObject>();
            var names = GenerateUniqueNouns( _settings.GDObjectsCount, new List<String>(), 2, 3 );
            var components = TypeCache.GetTypesDerivedFrom<GDComponent>().Where( t => t.Namespace?.StartsWith( settings.RootNamespace ) == true && !t.IsAbstract ).ToList();
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

        private IReadOnlyList<String> GenerateEnumNames(  Int32 count )
        {
            return GenerateUniqueNouns( count, new List<String>(), 1, 3 ).Select( s => $"E{s}" ).ToList();
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

        private CategoryHolder GenerateCategory( String name, Int32 depth, Int32 maxDepth, Int32 itemsCount, Random rnd )
        {
            var result = new CategoryHolder { TypeName = name, Depth = depth };
            if ( depth < maxDepth )
            {
                var itemNames = GenerateUniqueNouns( itemsCount, new List<String>(), 1, 2 );
                for ( var i = 0; i < itemsCount; i++ )
                {
                    result.Items.Add( new CategoryItemHolder
                                      {
                                              Name = itemNames[ i ],
                                              Category = result,
                                      } );
                }
            }

            return result;
        }

        private String GenerateCategoriesString( IReadOnlyList<CategoryHolder> categories, String @namespace )
        {
            var result = new StringBuilder();
            result.AppendLine( "using GDDB;" );
            result.AppendLine( $"namespace {@namespace}" );
            result.AppendLine( "{" );

            for ( int i = 0; i < categories.Count; i++ )
            {
                 var category = categories[ i ];
                 if ( category.Parent == null )
                     result.AppendLine( "    [Category]" );
                 else
                     result.AppendLine( $"    [Category( {category.Parent.Category.TypeName}.{category.Parent.Name} )]" );
                 result.AppendLine( $"    public enum {category.TypeName}" );
                 result.AppendLine( "    {" );

                 category.Items.ForEach( i =>
                 {
                     result.Append( "        " );
                     result.Append( i.Name );
                     result.AppendLine( "," );
                 } );

                 result.AppendLine( "    }" );
                 result.AppendLine();
            }
            result.AppendLine( "}" );

            return result.ToString();
        }

        private class CategoryHolder
        {
            public string                   TypeName;
            public List<CategoryItemHolder> Items = new();
            public CategoryItemHolder       Parent;
            public Int32                    Depth;
        }

        private class CategoryItemHolder
        {
            public String         Name;
            public CategoryHolder Category;
            public CategoryHolder Subcategory;
        }
    }
}