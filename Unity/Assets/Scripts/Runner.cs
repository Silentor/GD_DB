using System;
using System.IO;
using System.Linq;
using GDDB;
using GDDB.Serialization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
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

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private void Awake( )
        {
            //var loader = new GdEditorLoader( );

            //Load GDDB from asset
            var dbInAsset = Resources.Load<DBAsset>( "Default.folders" );
            var aloader        = new GdAssetLoader( dbInAsset );

            //Load GDDB from JSON with Unity assets resolver
            var assetsResolver = Resources.Load<DirectAssetReferences>( "Default.assets" );
            var dbInJson       =  File.ReadAllText( Application.streamingAssetsPath + "/Default.gddb.json" );
            var jloader        = new GdJsonLoader( dbInJson, assetsResolver );

            var fromAssetGDB   = aloader.GetGameDataBase();
            fromAssetGDB.Print();
            var fromJsonGDB         = jloader.GetGameDataBase();

#if UNITY_EDITOR
            var inputDB           = new GdEditorLoader().GetGameDataBase();
            var gdbInputHash      = inputDB.RootFolder.GetFoldersStructureChecksum();
            Debug.Log( $"Editor hash {gdbInputHash}" );
#endif
            var agdbLoadedHash    = fromAssetGDB.RootFolder.GetFoldersStructureChecksum();
            var jgdbLoadedHash    = fromJsonGDB.RootFolder.GetFoldersStructureChecksum();
            var generatedRootType = GetRootFolderType( fromAssetGDB );

            DebugOutput.text = $"AGDB hash: {agdbLoadedHash}\nJGDB hash: {jgdbLoadedHash}\nRoot sourcegen: {generatedRootType}";

            
            Debug.Log( $"Asset loaded hash {agdbLoadedHash}" );
            Debug.Log( $"JSON loaded hash {jgdbLoadedHash}" );
            Debug.Log( $"Source generated root type {generatedRootType}" );

#if UNITY_EDITOR
            if( gdbInputHash != agdbLoadedHash )
            {
                Debug.LogError( "Hashes are different" );
                CompareFolders( inputDB.RootFolder, fromAssetGDB.RootFolder );
            }
#endif

            if ( agdbLoadedHash != jgdbLoadedHash )
            {
                Debug.LogError( "Hashes are different" );
                CompareFolders( fromAssetGDB.RootFolder, fromJsonGDB.RootFolder );
            }

            var textureFromGD = fromJsonGDB.Root.Test7.Mobs5.Folder.Objects.First( gdo => gdo.HasComponent<GDComponentChild3>() ).GetComponent<GDComponentChild3>();
            DebugImageOutput.texture = textureFromGD.TexValue;

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

        private String GetRootFolderType( GdDb db )
        {
            var prop = db.GetType().GetProperty( "Root" );
            if( prop != null )
                return prop.PropertyType.Name;

            return "???";
        }
    }

    [Serializable]
    public abstract class TestNullAbstract
    {
        public Int32 TestValue = 42;
    }     

}
