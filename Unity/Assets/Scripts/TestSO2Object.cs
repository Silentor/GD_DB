using System;
using GDDB;
using UnityEngine;

namespace GDDB_User
{
    [CreateAssetMenu( fileName = "TestSO2Object", menuName = "GDDB/TestSO2Object", order = 0 )]
    public class TestSO2Object : ScriptableObject
    {
        public Int32 SomeValue = 1;

        public GDObject GDObjectReference;
    }
}