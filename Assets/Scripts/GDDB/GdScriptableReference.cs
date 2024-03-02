using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{                                 
    public class GdScriptableReference : ScriptableObject
    {
        public GDRoot         Root;
        public List<GDObject> Content = new List<GDObject>();
    }
}