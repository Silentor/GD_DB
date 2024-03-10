using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDObject", fileName = "GDObject", order = 0 )]
    public class GDObject : ScriptableObject
    {
        public String Name => name;


        [SerializeReference]
        public List<GDComponent> Components = new List<GDComponent>();

        public T GetComponent<T>() where T : GDComponent
        {
            return Components.Find( c => c is T ) as T;
        }
    }

    [Serializable]
    public abstract class GDComponent
    {

    }
    
}
