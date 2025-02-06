using System;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( fileName = "TestSO2Object", menuName = "GDDB/TestSO2Object", order = 0 )]
    public class TestSO2Object : ScriptableObject
    {
        public Int32 SomeValue = 1;

        public GDObject GDObjectReference;
    }
}