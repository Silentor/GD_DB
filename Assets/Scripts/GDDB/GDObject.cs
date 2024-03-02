using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDObject", fileName = "GDObject", order = 0 )]
    public class GDObject : ScriptableObject
    {
        public String Name { get; private set; }

        [SerializeField]
        private String _name;

        [SerializeReference]
        public List<GDComponent> Components = new List<GDComponent>();
    }

    [Serializable]
    public abstract class GDComponent
    {

    }
    
}
