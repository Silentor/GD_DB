using System;
using GDDB;
using UnityEngine;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
        public Int32            TestField;
        public GDObject         TestObject;
        [GdTypeFilter("Mobs//", typeof(TestMobComponent))]
        public GdId             TestId;
        public GdType           TestType;
        public Classes          NullObject;
        [SerializeReference]
        public TestNullAbstract TestAbstract;

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private void Awake( )
        {
            var loader = new GdEditorLoader( );
            //var loader = new GdScriptableLoader( "Default" );
            var gdb         = loader.GetGameDataBase();
            //gdb.Root.Mobs.

            //gdb.Print();

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
            return new GdScriptableLoader( );
#endif
        }
    }

    [Serializable]
    public abstract class TestNullAbstract
    {
        public Int32 TestValue = 42;
    }     

}
