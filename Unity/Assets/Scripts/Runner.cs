using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GDDB;
using GDDB.Serialization;
using TMPro;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
        public DBScriptableObject DB;

        public TMP_Text DebugOutput;
        public RawImage DebugImageOutput;

        public Int32            TestField;
        public GDObject         TestDirectObject;
        [GdTypeFilter("Mobs//", typeof(TestMobComponent))]
        public GdId             TestIdReference;
        public Object           TestFolderReference;
        public Classes          NullObject;
        [SerializeReference]
        public TestNullAbstract TestAbstract;

        public TextAsset FolderJson;

        private GdDb _soGDDB;
        private GdDb _jsonGDDB;
        private GdDb _editorGDDB;

        private CustomSampler _sjsonSampler = CustomSampler.Create( "SimpleJson.Parse" );

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private void Awake( )
        {
            var test = TestObject.CreateInstance<Test2>();
            Assert.IsTrue( test.Id == 42 );

            if ( FolderJson )
            {
                var       json       = FolderJson.text;
                using var strReader  = new StringReader( json );
                using var jsonReader = new Newtonsoft.Json.JsonTextReader( strReader );
                var       deser      = new FoldersJsonSerializer();
                var       folder     = deser.Deserialize( jsonReader, null, out var hash );
            }


            //var loader = new GdEditorLoader( );

            // var all = Resources.LoadAll( "" );
            // foreach ( var res in all )
            // {
            //     Debug.Log( res.name );
            // }

            //Load GDDB from asset
            DBScriptableObject db;
            db = DB ? DB : Resources.Load<DBScriptableObject>( "DefaultGDDB" );
            var     aloader   = new GdScriptableObjectLoader( db );
            _soGDDB   = aloader.GetGameDataBase();

            //Load GDDB from JSON with Unity assets resolver
            var assetsResolver = Resources.Load<DirectAssetReferences>( "DefaultGDDBAssetsRef" );
            using var dbInJson       =  File.OpenRead( Application.streamingAssetsPath + "/DefaultGDDB.json" );
            var jloader        = new GdJsonLoader( dbInJson, assetsResolver );
            _jsonGDDB         = jloader.GetGameDataBase();

#if UNITY_EDITOR
            _editorGDDB           = new GdEditorLoader().GetGameDataBase();
#endif
            UpdateDebugLabel();


            //var textureFromGD = fromJsonGDB.Root.Test7.Mobs5.Folder.Objects.First( gdo => gdo.HasComponent<GDComponentChild3>() ).GetComponent<GDComponentChild3>();
            //DebugImageOutput.texture = textureFromGD.TexValue;

            //var a = gdb.Root.Space_folder2;

            // var properties = gdb.Root.Mobs_Space._1_digit_start_folder.GetType().GetProperties();
            // foreach ( var propertyInfo in properties )
            // {
            //     Debug.Log( propertyInfo.Name );
            // }
            //var a = gdb.Root.Mobs.Humans2;
            //var humans = gdb.Root.Mobs.Humans_1;
            // foreach ( var human in humans )
            // {
            //     Debug.Log( $"Human: {human.Name}" );
            // }

            //var testGetMobs = gdb.GetMobs(  );          //Source generated

            
        }

        private void UpdateDebugLabel( )
        {
            var    soGdbLoadedHash   = _soGDDB.RootFolder.GetFoldersChecksum();
            var    jsonGdbLoadedHash = _jsonGDDB.RootFolder.GetFoldersChecksum();
            var    generatedRootType = GetRootFolderTypeReflection( _soGDDB );
            UInt64 editorGDDBHash;
#if UNITY_EDITOR
            editorGDDBHash = _editorGDDB.RootFolder.GetFoldersChecksum();
            DebugOutput.text = $"Editor hash: {editorGDDBHash}\nSO GDB hash: {soGdbLoadedHash}\nJSON GDB hash: {jsonGdbLoadedHash}\nRoot sourcegen: {generatedRootType}\nRoot SO folder: {_soGDDB.RootFolder.Name}\nRoot json folder: {_jsonGDDB.RootFolder.Name}";

            Debug.Log( $"editor hash {editorGDDBHash}" );
#else
            DebugOutput.text = $"AGDB hash: {soGdbLoadedHash}\nJGDB hash: {jsonGdbLoadedHash}\nRoot sourcegen: {generatedRootType}\nRoot SO folder: {_soGDDB.RootFolder.Name}\nRoot json folder: {_jsonGDDB.RootFolder.Name}";
#endif
            Debug.Log( $"so hash {soGdbLoadedHash}" );
            Debug.Log( $"json hash {jsonGdbLoadedHash}" );
            Debug.Log( $"Asset loaded root folder name {_soGDDB.RootFolder.Name}" );
            Debug.Log( $"Json loaded root folder name {_jsonGDDB.RootFolder.Name}" );
            Debug.Log( $"Json loaded root folder name {_jsonGDDB.RootFolder.Name}" );

#if UNITY_EDITOR
            if( editorGDDBHash != soGdbLoadedHash )
            {
                Debug.LogError( "Input and SO db hashes are different" );
                CompareFolders( _editorGDDB.RootFolder, _soGDDB.RootFolder );
            }

            if( editorGDDBHash != jsonGdbLoadedHash )
            {
                Debug.LogError( "Input and json db hashes are different" );
                CompareFolders( _editorGDDB.RootFolder, _jsonGDDB.RootFolder );
            }
#endif

            if ( soGdbLoadedHash != jsonGdbLoadedHash )
            {
                Debug.LogError( "SO and json hashes are different" );
                CompareFolders( _soGDDB.RootFolder, _jsonGDDB.RootFolder );
            }

        }

        private Boolean CompareFolders( Folder f1, Folder f2 )
        {
            using var f1Iter = f1.EnumerateFoldersDFS().GetEnumerator();
            using var f2Iter = f2.EnumerateFoldersDFS().GetEnumerator();
            Boolean f1HasNext = false, f2HasNext = false;

            while ( (f1HasNext = f1Iter.MoveNext()) && (f2HasNext = f2Iter.MoveNext()) )
            {
                if ( f1Iter.Current.GetPath() != f2Iter.Current.GetPath() )
                {
                    Debug.LogError( $"Different folders {f1Iter.Current.GetPath()} vs {f2Iter.Current.GetPath()}" );
                    return false;
                }    
            }

            if( !f1HasNext && f2HasNext )
            {
                Debug.LogError( "f1 has less folders" );
                return false;
            }
            else if( f1HasNext && !f2HasNext )
            {
                Debug.LogError( "f1 has more folders" );
                return false;
            }

            return true;
        }

        private String GetRootFolderTypeReflection( GdDb db )
        {
            var prop = db.GetType().GetProperty( "Root" );
            if( prop != null )
                return $"{prop.PropertyType.FullName}";

            return "???";
        }

        public void LoadJSONViaJsonNet( )
        {
            //Load GDDB from JSON with Unity assets resolver
            var assetsResolver = Resources.Load<DirectAssetReferences>( "DefaultGDDBAssetsRef" );
            var jsonFileName   = Application.streamingAssetsPath + "/DefaultGDDB.json";
            using var jsonStream       =  File.OpenRead( jsonFileName );
            var jloader        = new GdJsonLoader( jsonStream, assetsResolver );
            _jsonGDDB         = jloader.GetGameDataBase();
        }
    }

    [Serializable]
    public abstract class TestNullAbstract
    {
        public Int32 TestValue = 42;
    }     

    public class TestObject : Object
    {
        public static T CreateInstance<T>( ) where T : TestObject
        {
            var  type     = typeof(T);
             var constrs  = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy, null, Array.Empty<Type>(), null);
            var  instance = (T)constrs.Invoke(null);
            return instance;
        }

        public static TestObject CreateInstance( Type type )
        {
            var constr   = type.TypeInitializer;
            var instance = (TestObject)constr.Invoke(null);
            return instance;
        }

    }

    public class Test2 : TestObject
    {
        public Int32 Id = 42;
    }

}
