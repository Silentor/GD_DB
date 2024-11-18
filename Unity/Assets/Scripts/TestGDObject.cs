using System;
using UnityEngine;

namespace GDDB
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