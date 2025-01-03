using System;
using System.IO;
using System.Reflection;
using GDDB;
using GDDB.Serialization;
using TMPro;
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
        private GdDb _binaryGDDB;
        private GdDb _editorGDDB;

        private CustomSampler _sjsonSampler = CustomSampler.Create( "SimpleJson.Parse" );

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private void Awake( )
        {
            //var typeStr = "GDDB.TestGDObject, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            //var typeStr = "GDDB.TestGDObject, Assembly-CSharp";
            var typeStr = "GDDB.TestGDObject";
            var type    = Type.GetType( typeStr );
            Assert.IsTrue( type != null );

            var test = TestObject.CreateInstance<Test2>();
            Assert.IsTrue( test.Id == 42 );

            if ( FolderJson )
            {
                var       json       = FolderJson.text;
                var       deser      = new FolderSerializer();
                var       reader     = new JsonNetReader( json,  false );
                var       folder     = deser.Deserialize( reader, null, out var hash );
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
            Debug.Log( $"Loaded db from SO, loaded hash {_soGDDB.Hash}" );

            //Load GDDB from JSON with Unity assets resolver
            var assetsResolver = Resources.Load<DirectAssetReferences>( "DefaultGDDBAssetsRef" );
            using var dbInJson       =  File.OpenRead( Application.streamingAssetsPath + "/DefaultGDDB.json" );
            using var stringReader   = new StreamReader( dbInJson );
            var jsonloader        = new GdFileLoader( stringReader, assetsResolver );
            _jsonGDDB         = jsonloader.GetGameDataBase();
            Debug.Log( $"Loaded db from json, loaded hash {_jsonGDDB.Hash}" );

            //Load GDDB from binary with Unity assets resolver
            using var fileStraem = File.OpenRead( Application.streamingAssetsPath + "/DefaultGDDB.bin" );
            var binLoader = new GdFileLoader( fileStraem, assetsResolver );
            _binaryGDDB = binLoader.GetGameDataBase();
            Debug.Log( $"Loaded db from binary, loaded hash {_binaryGDDB.Hash}" );

#if UNITY_EDITOR
            _editorGDDB           = new GdEditorLoader().GetGameDataBase();
            Debug.Log( $"Loaded db from editor assets, loaded hash {_editorGDDB.Hash}" );
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
            var    binGdbLoadedHash = _binaryGDDB.RootFolder.GetFoldersChecksum();
            var    generatedRootType = GetRootFolderTypeReflection( _soGDDB );
            UInt64 editorGDDBHash;
#if UNITY_EDITOR
            editorGDDBHash = _editorGDDB.RootFolder.GetFoldersChecksum();
            DebugOutput.text = $"Editor hash: {editorGDDBHash}\nSO GDB hash: {soGdbLoadedHash}\nJSON GDB hash: {jsonGdbLoadedHash}\nbin GDB hash: {binGdbLoadedHash}\nRoot sourcegen: {generatedRootType}\nRoot SO folder: {_soGDDB.RootFolder.Name}\nRoot json folder: {_jsonGDDB.RootFolder.Name}";

            Debug.Log( $"editor hash {editorGDDBHash}" );
#else
            DebugOutput.text = $"AGDB hash: {soGdbLoadedHash}\nJGDB hash: {jsonGdbLoadedHash}\nbin GDB hash: {binGdbLoadedHash}\nRoot sourcegen: {generatedRootType}\nRoot SO folder: {_soGDDB.RootFolder.Name}\nRoot json folder: {_jsonGDDB.RootFolder.Name}";
#endif
            Debug.Log( $"so hash {soGdbLoadedHash}" );
            Debug.Log( $"json hash {jsonGdbLoadedHash}" );
            Debug.Log( $"bin hash {binGdbLoadedHash}" );
            Debug.Log( $"Asset loaded root folder name {_soGDDB.RootFolder.Name}" );
            Debug.Log( $"Json loaded root folder name {_jsonGDDB.RootFolder.Name}" );
            Debug.Log( $"Binary loaded root folder name {_binaryGDDB.RootFolder.Name}" );

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
            
            if ( binGdbLoadedHash != jsonGdbLoadedHash )
            {
                Debug.LogError( "bin and json hashes are different" );
                CompareFolders( _binaryGDDB.RootFolder, _jsonGDDB.RootFolder );
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

        public void LoadDBViaJsonNet( )
        {
            //Load GDDB from JSON with Unity assets resolver
            var       assetsResolver = Resources.Load<DirectAssetReferences>( "Default.assets" );
            var       jsonFileName   = Application.streamingAssetsPath + "/DefaultGDDB.json";
            using var jsonStream     =  File.OpenRead( jsonFileName );
            using var streamReader   = new StreamReader( jsonStream );
            var       jloader        = new GdFileLoader( streamReader, assetsResolver );
            _jsonGDDB         = jloader.GetGameDataBase();
        }

        public void LoadDBViaBinary( )
        {
            //Load GDDB from JSON with Unity assets resolver
            var       assetsResolver = Resources.Load<DirectAssetReferences>( "Default.assets" );
            var       binFileName    = Application.streamingAssetsPath + "/DefaultGDDB.bin";
            using var binStream      =  File.OpenRead( binFileName );
            var       bloader        = new GdFileLoader( binStream, assetsResolver );
            _binaryGDDB         = bloader.GetGameDataBase();
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
