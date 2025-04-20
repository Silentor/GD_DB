using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gddb
{
    [CreateAssetMenu( menuName = "Gddb/GDRoot", fileName = "GDRoot", order = 0 )]
    public class GDRoot : GDObject
    {
        public String   Id        = "UniqueName";
        public Int32    Version;
    }
}
