using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDRoot", fileName = "GDRoot", order = 0 )]
    public class GDRoot : GDObject
    {
        public String Id        = "UniqueName";
        public Int32 Version;

        public Vector3 TestVector3 = new Vector3( 1.1f, 2.0f, 3.5f );
    }
}
