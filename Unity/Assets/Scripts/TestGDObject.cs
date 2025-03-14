using System;
using GDDB;
using UnityEngine;

namespace GDDB_User
{
    public class TestGDObject : GDObject
    {
        public Int32 GDObjectProp = -1;

        private void Awake( )
        {
            //Debug.Log( "Awake" );
        }

        private void OnEnable( )
        {
            //Debug.Log( "OnEnable" );
        }
    }
}