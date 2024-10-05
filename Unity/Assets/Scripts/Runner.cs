using System;
using GDDB;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
        public TMP_Text DebugOutput;

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
            var loader = new GdAssetLoader( "Default" );
            //var loader = new GdJsonLoader( "Default" );
            var gdb         = loader.GetGameDataBase();
            gdb.Print();

            var inputDB        = new GdEditorLoader().GetGameDataBase();
            var gdbInputHash      = inputDB.RootFolder.GetFoldersStructureHash(); 
            var gdbLoadedHash     = gdb.RootFolder.GetFoldersStructureHash();
            var generatedRootType = GetRootFolderType( gdb );


            DebugOutput.text = $"GDB hash: {gdbLoadedHash}\nRoot sourcegen: {generatedRootType}";

            Debug.Log( $"Input hash {gdbInputHash}" );
            Debug.Log( $"Loaded hash {gdbLoadedHash}" );
            Debug.Log( $"Source generated root type {generatedRootType}" );

            if( gdbInputHash != gdbLoadedHash )
            {
                Debug.LogError( "Hashes are different" );

                CompareFolders( inputDB.RootFolder, gdb.RootFolder );
            }

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

        private GdLoader GetGD(  )
        {
#if UNITY_EDITOR
            return new GdEditorLoader( );
#else
            return new GdScriptableLoader( "Default" );
#endif
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
