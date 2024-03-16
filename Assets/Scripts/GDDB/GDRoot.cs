using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDRoot", fileName = "GDRoot", order = 0 )]
    public class GDRoot : GDObject
    {
        public String   Id        = "UniqueName";
        public Int32    Version;
    }
}
