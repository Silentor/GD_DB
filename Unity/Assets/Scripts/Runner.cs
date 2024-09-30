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

            var gdbInputHash = new GdEditorLoader().GetGameDataBase().RootFolder.GetFoldersStructureHash(); 
            var gdbLoadedHash = gdb.RootFolder.GetFoldersStructureHash();
            var generatedRootType = GetRootFolderType( gdb );


            DebugOutput.text = $"GDB hash: {gdbLoadedHash}\nRoot sourcegen: {generatedRootType}";

            Debug.Log( $"Input hash {gdbInputHash}" );
            Debug.Log( $"Loaded hash {gdbLoadedHash}" );
            Debug.Log( $"Source generated root type {generatedRootType}" );

            if( gdbInputHash != gdbLoadedHash )
            {
                Debug.LogError( "Hashes are different" );
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
