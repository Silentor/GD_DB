using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions.Extensions;
using GDDB;
using GDDB.Serialization;
using GeneratedGDDB.Lunch.Thought.Attraction;
using GeneratedGDDB.Property.Cover.Position;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
        [Tooltip("Test tooltip")]
        public ScriptableObject RawSO;
        public TestSOObject     RawTestSO;
        public GDObject         RawGDO;

        [Tooltip("Test tooltip")]
        [GdObjectFilter]
        public ScriptableObject FilteredSO;
        [GdObjectFilter(  )]
        public TestSOObject     FilteredSO_Descendant;
        [GdObjectFilter]
        public GDObject         FilteredGDO;
        [GdObjectFilter( typeof(RequestIce2) )]
        public RequestIce       FilteredGDO_Descendant;
        [GdObjectFilter( typeof(UmbrellaPeace) )]
        public GDObject         FilteredGDO_Comp;
        [GdObjectFilter]
        public Texture2D     FilteredTexture;
        [GdObjectFilter]
        public String     FilteredString;


        [GdObjectFilter( "Mobs/**/Skins*")]
        public GdFolderRef TestFolderRefernce;

        [GdObjectFilter( typeof(TestMobComponent))]
        public GdRef        TestObjectReference;

        public DBScriptableObject                     DB;
        public TMP_Text DebugOutput;
        public RawImage DebugImageOutput;

        public Int32            TestField;
        public GDObject         TestDirectObject;
        
        
        public Object           TestFolderReference;
        public Classes          NullObject;
        [SerializeReference]
        public TestNullAbstract TestAbstract;

        public TextAsset FolderJson;

        private GdDb _soGDDB;
        private GdDb _jsonGDDB;
        private GdDb _binaryGDDB;
        private GdDb _editorGDDB;

        private readonly CustomSampler _loadBinaryFileSampler = CustomSampler.Create( "Runner.LoadBinaryFile" );

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private IEnumerator Start( )
        {
            var memory = new MemoryStream();
            using var writer = new System.IO.BinaryWriter( memory );
            writer.Write( (Byte)1 );
            writer.Write( (UInt64)2 );

            var memory2 = new MemoryStream( memory.ToArray() );
            using var reader = new System.IO.BinaryReader( memory2 );
            if( reader.ReadByte() !=  1 ) throw new Exception("Invalid byte");
            if( reader.ReadUInt64() != 2 ) throw new Exception("Invalid ulong");

            Debug.Log( $"Its ok, buffer size {memory.ToArray().Length} bytes" );

            //Load GDDB from SO asset
            DBScriptableObject db;
            db = DB ? DB : Resources.Load<DBScriptableObject>( "DefaultGDDB" );
            var     aloader   = new GdScriptableObjectLoader( db );
            _soGDDB   = aloader.GetGameDataBase();
            Debug.Log( $"Loaded db from SO, loaded hash {_soGDDB.Hash}" );

            yield return StartCoroutine( LoadDBViaJsonNetAsync() );

            yield return StartCoroutine( LoadDBViaBinaryAsync() );

#if UNITY_EDITOR
            _editorGDDB           = new GdEditorLoader().GetGameDataBase();
            Debug.Log( $"Loaded db from editor assets, loaded hash {_editorGDDB.Hash}" );
#endif
            UpdateDebugLabel();

            //Benchmarks
            for ( int i = 0; i < 3; i++ )
            {

                //Find obj by guid
                var timer = Stopwatch.StartNew();
                foreach ( var obj in _binaryGDDB.AllObjects.Select( ao => ao.Object ) )
                {
                    if ( obj is GDObject gdo )
                    {
                        var obj2 = _binaryGDDB.GetObject( new GdRef( gdo.Guid ) );
                    }
                }

                timer.Stop();
                Debug.Log( $"Find object by guid time {timer.Elapsed.TotalMilliseconds * 1000} mks" );

                //Find obj by obj type
                //Prepare some obj types
                //Get most common types
                var types = _binaryGDDB.AllObjects.Select( obj => obj.Object.GetType() ).GroupBy( t => t ). OrderByDescending( tg => tg.Count() )
                                       .Select( tg => new { tg.Key, Count = tg.Count() } ).ToArray();

                var resultObjects = new List<ScriptableObject>();
                var resultFolders = new List<GdFolder>();
                timer.Restart();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 0 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 1 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 2 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 3 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 4 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                _binaryGDDB.FindObjects( null, resultObjects, resultFolders ).FindObjectType( types[ 5 ].Key );
                resultObjects.Clear();
                resultFolders.Clear();
                timer.Stop();
                Debug.Log( $"Find objects by main object type, time {timer.Elapsed.TotalMilliseconds * 1000} mks" );

                yield return null;
            }



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

        private Boolean CompareFolders( GdFolder f1, GdFolder f2 )
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
            StartCoroutine( LoadDBViaJsonNetAsync() );
        }

        private IEnumerator LoadDBViaJsonNetAsync( )
        {
            yield return null;
            GC.Collect();
            yield return null;

            yield return StartCoroutine( SaveFileFromStreamingAssetsToPersistent( "DefaultGDDB.json" ) );

            var assetResolver = Resources.Load<DirectAssetReferences>( "Default.assets" );

            yield return null;
            GC.Collect();
            yield return null;


            LoadJsonFromFileStream( assetResolver );

            yield return null;
            GC.Collect();
            yield return null;

            LoadJsonFromStringBuffer( assetResolver );

            yield return null;
            GC.Collect();
            yield return null;

            LoadJsonFromBytesBuffer( assetResolver );

            yield return null;
            GC.Collect();
            yield return null;
        }


        public void LoadDBViaBinary( )
        {
            StartCoroutine( LoadDBViaBinaryAsync() );
        }

        private IEnumerator LoadDBViaBinaryAsync( )
        {
            yield return null;
            GC.Collect();
            yield return null;

            yield return StartCoroutine( SaveFileFromStreamingAssetsToPersistent( "DefaultGDDB.bin" ) );

            var assetResolver = Resources.Load<DirectAssetReferences>( "Default.assets" );

            yield return null;
            GC.Collect();
            yield return null;

            LoadBinaryFromFileStream( assetResolver );

            yield return null;
            GC.Collect();
            yield return null;

            LoadBinaryFromMemoryBuffer( assetResolver );

            yield return null;
            GC.Collect();
            yield return null;
        }

        private IEnumerator SaveFileFromStreamingAssetsToPersistent( String fileName )
        {
            var inputPath  = Application.streamingAssetsPath + "/" + fileName;
            var outputPath = Application.persistentDataPath + "/" + fileName;

            var www = UnityEngine.Networking.UnityWebRequest.Get( inputPath );
            www.SendWebRequest();
            while (!www.downloadHandler.isDone)
            {
                yield return null;
            }

            var bytes = www.downloadHandler.data;
            File.WriteAllBytes( outputPath, bytes );
        }

        private void LoadJsonFromStringBuffer( IGdAssetResolver assetResolver )
        {
            var jsonString = File.ReadAllText( Application.persistentDataPath + "/DefaultGDDB.json" );
            var jloader   = new GdFileLoader( jsonString, assetResolver );
            _jsonGDDB         = jloader.GetGameDataBase();
        }

        private void LoadJsonFromBytesBuffer( IGdAssetResolver assetResolver )
        {
            var       jsonBytes  = File.ReadAllBytes( Application.persistentDataPath + "/DefaultGDDB.json" );
            using var jsonStream = new StreamReader( new MemoryStream( jsonBytes ), Encoding.UTF8, false );
            var       jloader   = new GdFileLoader( jsonStream, assetResolver );
            _jsonGDDB         = jloader.GetGameDataBase();
        }

        private void LoadJsonFromFileStream( IGdAssetResolver assetResolver )
        {
            using var textFile = File.OpenText( Application.persistentDataPath + "/DefaultGDDB.json" );
            var jloader  = new GdFileLoader( textFile, assetResolver );
            _jsonGDDB         = jloader.GetGameDataBase();
        }

        private void LoadBinaryFromMemoryBuffer( IGdAssetResolver assetResolver )
        {
            _loadBinaryFileSampler.Begin();
            var bytes = File.ReadAllBytes( Application.persistentDataPath + "/DefaultGDDB.bin" );
            _loadBinaryFileSampler.End();
            var jloader = new GdFileLoader( bytes, assetResolver );
            _binaryGDDB = jloader.GetGameDataBase();
        }

        private void LoadBinaryFromFileStream( IGdAssetResolver assetResolver )
        {
            using var fileStream = File.OpenRead( Application.persistentDataPath + "/DefaultGDDB.bin" );
            var jloader = new GdFileLoader( fileStream, assetResolver );
            _binaryGDDB = jloader.GetGameDataBase();
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

    [Preserve]
    public class Test2 : TestObject
    {
        public Int32 Id = 42;
    }

}
