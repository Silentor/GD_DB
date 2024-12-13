using System;

namespace UnityEngine
{
#if !UNITY_2021_2_OR_NEWER
    public class Object
    {
        public String name;
    }

    public class ScriptableObject : Object
    {
        
    }
#endif
}