using System;
using GDDB;
using TestGdDb;
using UnityEditor.Compilation;
using UnityEngine;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {
        public Int32  TestField;
        public GDObject TestObject;
        public GdId   TestId;
        public GdType TestType;

        //[GdTypeFilter(MainCategory.Mobs, EMobs.Elves)]
        //[GdTypeFilter(MainCategory.Game)]                      
        //public GdType TestTypeRestrictedMobs;

        private void Awake( )
        {
            var loader = new GdJsonLoader( "GD1" );

            var gdb         = loader.GetGameDataBase();
            //var testGetMobs = gdb.GetMobs(  );          //Source generated
        }

        private GdLoader GetGD( String name )
        {
#if UNITY_EDITOR
            return new GdEditorLoader( name );
#else
            return new GdScriptableLoader( name );
#endif
        }
    }

}
