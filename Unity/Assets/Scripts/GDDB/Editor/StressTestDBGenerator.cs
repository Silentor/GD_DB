using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private          String[] _nouns;
        private          String[] _verbs;
        private          String[] _types;
        private readonly String[] _wordDelimiters = new String[]{", ", ".", " ", "-", "\r\n", "\t", String.Empty, };
        private readonly Int32[] _int32Values = new []{ 0, 1, -1, Int32.MaxValue, Int32.MinValue };
        private readonly Double[] _doubleValues = new []{ 0, 1, -1, 0.5, -0.5, Double.MinValue, Double.MaxValue, Double.Epsilon, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity,  };
        private readonly Single[] _singleValues = new []{ 0, 1, -1, 0.5f, -0.5f, Single.MinValue, Single.MaxValue, Single.Epsilon, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity,  };

        private Type[] _gdObjects;
        private Type[] _gdComponents;

        private void OnDestroy( )
        {
            StressTestSettings.instance.Save();
        }

        private void CreateGUI( )
        {
            var settings = StressTestSettings.instance;
            var so       = new SerializedObject( settings );
            var settingsWidget = new InspectorElement( so );
            rootVisualElement.Add( settingsWidget );

            var generateScriptsBtn = new Button( ( ) => GenerateComponents( settings ) ) { text = "Generate GDComponents" };
            rootVisualElement.Add( generateScriptsBtn );
            var generateObjectsBtn = new Button( ( ) => GenerateGDDatabase( settings ) ) { text = "Generate GDObjects" };
            rootVisualElement.Add( generateObjectsBtn );

            _nouns = (Resources.Load( "NounsList" ) as TextAsset).text.Split( "\n" ).Select( word => word.Trim() ).ToArray();
            _verbs = (Resources.Load( "VerbsList" ) as TextAsset).text.Split( "\n" ).Select( word => word.Trim() ).ToArray();
            _types = (Resources.Load( "TypesList" ) as TextAsset).text.Split( "\n" ).Select( word => word.Trim() ).ToArray();

            if ( _nouns.Length < 10 || _verbs.Length < 10 )
                Debug.LogError( "Nouns or Verbs list is too small" );
        }

        private void GenerateComponents( StressTestSettings settings )
        {
            settings.Save();

            var rnd = new Random();

            var namespaces = GenerateNamespaces( settings );
            var classNames = GenerateClassNames( settings );

            if ( !Directory.Exists( settings.OutputFolderComponents ) )
            {
                Directory.CreateDirectory( settings.OutputFolderComponents );
            }

            for ( int i = 0; i < settings.GDObjectScriptsCount; i++ )
            {
                var ns        = namespaces[ rnd.Next( namespaces.Count ) ];
                var className = classNames[ i ];
                var script    = GenerateGDObjectScript( ns, className );
                var path      = Path.Join( settings.OutputFolderComponents, $"{className}.cs" );
                File.WriteAllText( path, script );
            }

            for ( int i = settings.GDObjectScriptsCount; i < classNames.Count; i++ )
            {
                var ns        = namespaces[ rnd.Next( namespaces.Count ) ];
                var className = classNames[ i ];
                var script    = GenerateComponentScript( ns, className );
                var path      = Path.Join( settings.OutputFolderComponents, $"{className}.cs" );
                File.WriteAllText( path, script );          
            }
           
            AssetDatabase.Refresh();
        }

        private void GenerateGDDatabase( StressTestSettings settings )
        {
            settings.Save();

            var rnd = new Random();
            _gdObjects = TypeCache.GetTypesDerivedFrom<GDObject>().Where( t => t.Namespace?.StartsWith( settings.RootNamespace ) == true && !t.IsAbstract ).ToArray();
            _gdComponents = TypeCache.GetTypesDerivedFrom<GDComponent>().Where( t => t.Namespace?.StartsWith( settings.RootNamespace ) == true && !t.IsAbstract ).ToArray();

            //Prepare output folder
            if ( !AssetDatabase.IsValidFolder( settings.OutputFolderDB ) )
            {
                var baseFolderGuid = AssetDatabase.CreateFolder( Path.GetDirectoryName( settings.OutputFolderDB ), Path.GetFileName( settings.OutputFolderDB ) );
                if ( String.IsNullOrEmpty( baseFolderGuid ) )
                {
                    Debug.LogError( $"[{nameof(StressTestDBGenerator)}]-[{nameof(GenerateGDDatabase)}] Base folder for objects {settings.OutputFolderDB} is not created properly!" );
                    return;
                }
            }

            var names = GenerateUniqueNouns( settings.GDObjectsCount, new List<String>(), 2, 3 );
            var objectsCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                GenerateGDObjectsInFolderRecursive( ref objectsCount, 0, settings.OutputFolderDB, names, settings, rnd );
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private void GenerateGDObjectsInFolderRecursive( ref Int32 objectsCount, Int32 folderDepth, String folderPath, IReadOnlyList<String> objectNames, StressTestSettings settings, Random rnd )
        {
            if ( objectsCount >= settings.GDObjectsCount )
                return;

            if ( rnd.NextDouble() < 0.5 )
                GenerateCatalogFolder( ref objectsCount, folderDepth, folderPath, objectNames, settings, rnd );
            else
                GenerateRandomObjectsFolder( ref objectsCount, folderDepth, folderPath, objectNames, settings, rnd );


        }

        private void GenerateRandomObjectsFolder( ref Int32 objectsCount, Int32 folderDepth, String folderPath, IReadOnlyList<String> objectNames, StressTestSettings settings, Random rnd )
        {
            if ( objectsCount >= settings.GDObjectsCount )
                return;

            //Generate files
            var objectsCountInFolder = rnd.Next( settings.ObjectsPerFolders.x, settings.ObjectsPerFolders.y + 1 );
            while ( objectsCountInFolder-- > 0 && objectsCount < settings.GDObjectsCount )
            {
                var gdType = rnd.NextDouble() > 0.5 ? typeof(GDObject) : _gdObjects[ rnd.Next( _gdObjects.Length ) ];
                var go     = GDObject.CreateInstance( gdType );
                if( gdType != typeof(GDObject) )
                    FillObjectValues( go, rnd );
                go.name       = objectNames[ rnd.Next( objectNames.Count ) ];
                go.Components = new List<GDComponent>();

                var componentsCount = rnd.Next( 1, settings.MaxComponentsPerObject + 1 );
                for ( var i = 0; i < componentsCount; i++ )
                {
                    var componentType = _gdComponents[ rnd.Next( _gdComponents.Length ) ];
                    var component     = (GDComponent)Activator.CreateInstance( componentType );
                    FillObjectValues( component, rnd );
                    go.Components.Add( component );
                }

                AssetDatabase.CreateAsset( go, Path.Join( folderPath, $"{go.name}.asset" ) );
                objectsCount++;
            }

            if ( folderDepth >= settings.SubfoldersMaxDepth || objectsCount >= settings.GDObjectsCount )
                return;

            var subfoldersCount = rnd.Next( settings.SubfoldersCount.x, settings.SubfoldersCount.y + 1 );
            for ( int i = 0; i < subfoldersCount; i++ )
            {
                var subfolderName = objectNames[ rnd.Next( objectNames.Count ) ];
                var subfolderPath = Path.Join( folderPath, subfolderName );
                if( !AssetDatabase.IsValidFolder( subfolderPath ) )
                    AssetDatabase.CreateFolder( folderPath, subfolderName );

                GenerateGDObjectsInFolderRecursive( ref objectsCount, folderDepth + 1, subfolderPath, objectNames, settings, rnd );

                if ( objectsCount >= settings.GDObjectsCount )
                    return;
            }
        }

        private void GenerateCatalogFolder( ref Int32 objectsCount, Int32 folderDepth, String folderPath, IReadOnlyList<String> objectNames, StressTestSettings settings, Random rnd )
        {
             var objectCountInFolder      = rnd.Next( settings.ObjectsPerFolders.x, settings.ObjectsPerFolders.y + 1 );
             var componentsCount  = rnd.Next( 1, settings.MaxComponentsPerObject + 1 );
             var gdObjectType     = _gdObjects[ rnd.Next( _gdObjects.Length ) ];
             var gdComponentTypes = new Type[componentsCount];
             for ( int i = 0; i < componentsCount; i++ )                 
                 gdComponentTypes[ i ] = _gdComponents[ rnd.Next( _gdComponents.Length ) ];
             var baseName = objectNames[ rnd.Next( objectNames.Count ) ];

             var counter = 0;
             while ( objectCountInFolder-- > 0 && objectsCount < settings.GDObjectsCount )
             {
                 var go = GDObject.CreateInstance( gdObjectType );
                 if( gdObjectType != typeof(GDObject) )
                    FillObjectValues( go, rnd );
                 go.name       = $"{baseName}_{counter++:00}";
                 go.Components = new List<GDComponent>();
                 foreach ( var componentType in gdComponentTypes )
                 {
                     var component         = (GDComponent)Activator.CreateInstance( componentType );      
                     FillObjectValues( component, rnd );
                     go.Components.Add( component );
                 }

                 //Some items in catalog may have additional component
                 if( rnd.NextDouble() < 0.1f )
                     go.Components.Add( (GDComponent)Activator.CreateInstance( _gdComponents[rnd.Next( _gdComponents.Length )] ) );

                 AssetDatabase.CreateAsset( go, Path.Join( folderPath, $"{go.name}.asset" ) );
                 objectsCount++;
             }              

             if ( folderDepth >= settings.SubfoldersMaxDepth || objectsCount >= settings.GDObjectsCount )
                 return;

             var subfoldersCount = rnd.Next( settings.SubfoldersCount.x, settings.SubfoldersCount.y + 1 );
             for ( int i = 0; i < subfoldersCount; i++ )
             {
                 var subfolderName = objectNames[ rnd.Next( objectNames.Count ) ];
                 var subfolderPath = Path.Join( folderPath, subfolderName );
                 if( !AssetDatabase.IsValidFolder( subfolderPath ) )
                     AssetDatabase.CreateFolder( folderPath, subfolderName );

                 GenerateGDObjectsInFolderRecursive( ref objectsCount, folderDepth + 1, subfolderPath, objectNames, settings, rnd );

                 if ( objectsCount >= settings.GDObjectsCount )
                     return;
             }

        }



        private IReadOnlyList<String> GenerateClassNames(StressTestSettings settings  )
        {
            return GenerateUniqueNouns( settings.ComponentScriptsCount + settings.GDObjectScriptsCount, new List<String>(), 2, 3 );
        }

        private IReadOnlyList<String> GenerateNamespaces(StressTestSettings settings )
        {
            var rnd    = new Random();
            var result = new List<String>( settings.ComponentNamespacesCount );
            result.Add( settings.RootNamespace );

            while ( result.Count < settings.ComponentNamespacesCount )
                for ( var i = 0; i < 10; i++ )
                    GenerateNamespaceRecursive( settings.RootNamespace, 1, rnd, settings, result );

            return result;
        }



        private void GenerateNamespaceRecursive(    String ns, Int32 depth, Random rnd, StressTestSettings settings, List<String> result )
        {
            if ( result.Count > settings.ComponentNamespacesCount )
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
                for ( var i = 0; i < childsCount; i++ ) GenerateNamespaceRecursive( ns, depth + 1, rnd, settings, result  );
            }
        }

        private void FillObjectValues( Object component, Random rnd )
        {
            var fields = component.GetType().GetFields( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public );
            foreach ( var field in fields )
            {
                var value = GenerateValue( field.FieldType, rnd );
                if( value != null )
                    field.SetValue( component, value );
            }

            Object GenerateValue( Type valueType, Random rnd )
            {
                if ( valueType == typeof( String ) )
                    return GenerateStringValue();
                else if ( valueType == typeof( Int32 ) )
                    return GenerateInt32Value( );
                else if ( valueType == typeof( double ) )
                    return GenerateDoubleValue();
                else if ( valueType == typeof( Single ) )
                    return GenerateSingleValue();
                else if ( valueType == typeof( Boolean ) )
                    return rnd.NextDouble() > 0.5 ;
                else if ( valueType == typeof( Vector2 ) )
                    return (Vector2)GenerateVector3Value();
                else if ( valueType == typeof( Vector3 ) )
                    return GenerateVector3Value();
                else if ( valueType == typeof( Vector4 ) )
                    return new Vector4( (Single)rnd.NextDouble() * 100, (Single)rnd.NextDouble() * 100, (Single)rnd.NextDouble() * 100, (Single)rnd.NextDouble() * 100 );
                else if ( valueType == typeof( Color ) )
                    return new Color( (Single)rnd.NextDouble(), (Single)rnd.NextDouble(), (Single)rnd.NextDouble(), (Single)rnd.NextDouble() );
                else if ( valueType == typeof( Color32 ) )
                    return new Color32( (Byte)rnd.Next( 0, 255 ), (Byte)rnd.Next( 0, 255 ), (Byte)rnd.Next( 0, 255 ), (Byte)rnd.Next( 0, 255 ) );
                else if ( valueType == typeof( Quaternion ) )
                    return Quaternion.Euler( GenerateVector3Value() );
                else if ( valueType == typeof( Matrix4x4 ) )
                    return Matrix4x4.TRS( GenerateVector3Value(), Quaternion.Euler( GenerateVector3Value() ), GenerateVector3Value() );
                else if ( valueType == typeof( Bounds ) )
                    return new Bounds( GenerateVector3Value(), GenerateVector3Value() );
                else if ( valueType == typeof( Rect ) )
                    return new Rect( GenerateVector3Value(), GenerateVector3Value() );
                else if ( valueType == typeof( AnimationCurve ) )
                    return GenerateAnimationCurveValue();
                else if ( valueType == typeof( Guid ) )
                    return rnd.NextDouble() < 0.1 ? Guid.Empty : Guid.NewGuid();
                else if ( valueType.IsArray )
                {
                    var count = rnd.Next( 0, 5 + 1 );
                    var elementType = valueType.GetElementType();
                    var result = Array.CreateInstance( elementType, count );
                    for ( int i = 0; i < count; i++ )
                    {
                        var value = GenerateValue( elementType, rnd );
                        if( value != null )
                            result.SetValue( value, i );
                    }

                    return result;
                }
                else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof( List<> ) )
                {
                    var count = rnd.Next( 0, 5 + 1 );
                    var elementType = valueType.GetGenericArguments()[0];
                    var result = Activator.CreateInstance( valueType );
                    var addMethod = valueType.GetMethod( "Add" );
                    for ( int i = 0; i < count; i++ )
                    {
                        var value = GenerateValue( elementType, rnd );
                        if( value != null )
                            addMethod.Invoke( result, new Object[]{ value } );
                    }

                    return result;
                }

                return null;
            }

            String GenerateStringValue( )
            {
                var wordsCount = rnd.Next( 0, 5 + 1 );
                var result     = String.Empty;
                for ( int i = 0; i < wordsCount; i++ )
                {
                    result += rnd.Next() > 0.5 ? _nouns[ rnd.Next( _nouns.Length ) ] : _verbs[ rnd.Next( _verbs.Length ) ];
                    if( i > 0 && i < wordsCount - 1 )
                        result += _wordDelimiters[ rnd.Next( _wordDelimiters.Length ) ];
                }

                return result;
            }

            Int32 GenerateInt32Value( )
            {
                return rnd.NextDouble() > 0.5 ? _int32Values[ rnd.Next( _int32Values.Length ) ] : rnd.Next( Int32.MinValue, Int32.MaxValue ); 
            }

            Double GenerateDoubleValue( )
            {
                return rnd.NextDouble() > 0.5 ? _doubleValues[ rnd.Next( _doubleValues.Length ) ] : (rnd.NextDouble(  ) - 0.5) * 100; 
            }

            Single GenerateSingleValue( )
            {
                return rnd.NextDouble() > 0.5 ? _singleValues[ rnd.Next( _singleValues.Length ) ] : (Single)((rnd.NextDouble(  ) - 0.5) * 100); 
            }

            Vector3 GenerateVector3Value( )
            {
                return  new Vector3( (Single)rnd.NextDouble() * 100, (Single)rnd.NextDouble() * 100, (Single)rnd.NextDouble() * 100 ) ; 
            }

            AnimationCurve GenerateAnimationCurveValue( )
            {
                var keysCount = rnd.Next( 2, 10 + 1 );
                var keys = new Keyframe[ keysCount ];
                for ( int i = 0; i < keysCount; i++ )
                {
                    keys[ i ] = rnd.NextDouble() > 0.5 
                            ? new Keyframe( (Single)rnd.NextDouble(), (Single)rnd.NextDouble() ) 
                            : new Keyframe( (Single)rnd.NextDouble(), (Single)rnd.NextDouble(), (Single)rnd.NextDouble(), (Single)rnd.NextDouble() );
                }
                return new AnimationCurve( keys );
            }
        }

        private String GenerateGDObjectScript( String namespaceName, String gdoName )
        {
            var result = new StringBuilder();
            result.AppendLine( "using GDDB;" );
            result.AppendLine( $"namespace {namespaceName}" );
            result.AppendLine( "{" );
            result.AppendLine( $"    public class {gdoName} : GDObject" );
            result.AppendLine( "    {" );
            var members = GenerateMembers( gdoName );
            members.ForEach( m => result.AppendLine( $"        {m}" ) );
            result.AppendLine( "    }" );
            result.AppendLine( "}" );

            return result.ToString();
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
            var infinityLoopDefenceCounter = 0; 

            while( result.Count < count )
            {
                var nameLength = rnd.Next( minWordsCount, maxWordsCount + 1 );
                var partsList   = new List<String>( nameLength );

                while( partsList.Count < nameLength )
                {
                    var namePart = _nouns[ rnd.Next( _nouns.Length ) ];
                    if( !partsList.Contains( namePart ) )
                        partsList.Add( namePart );

                    if(infinityLoopDefenceCounter++ > 10000)
                    {
                        Debug.LogError( "Infinity loop defence abort GenerateUniqueNouns()" );
                        break;
                    }
                }

                var fullName = String.Join( "", partsList );
                if ( !result.Contains( fullName ) && !excludeList.Contains( fullName ) )
                    result.Add( fullName );
            }

            return result;
        }
    }
}