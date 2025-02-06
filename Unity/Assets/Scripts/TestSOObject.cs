using System;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( fileName = "TestSOObject", menuName = "GDDB/TestSOObject", order = 0 )]
    public class TestSOObject : ScriptableObject
    {
        public Int32 SomeValue = 1;

        public GDObject             GDObjectReference;
        public TestSO2Object        SOObjectReference;
        public TestSOObject         SelfReference;
    }
}