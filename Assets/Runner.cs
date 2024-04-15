using System;
using GDDB;
using UnityEngine;

namespace GDDB_User
{
    public class Runner : MonoBehaviour
    {

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
