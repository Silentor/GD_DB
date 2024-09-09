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
            var loader = new GdScriptableLoader( "Default" );
            var gdb         = loader.GetGameDataBase();

            gdb.Print();

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
