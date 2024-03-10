using System;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( fileName = "TestSO", menuName = "TestSO", order = 0 )]
    public class TestSO : ScriptableObject
    {
        public Test1 TestValue;

        public Vector3 TestVec3 = new Vector3( 1, 2, 3.5f ); 

        private void Reset( )
        {
            TestValue.StructValue = new Test2(42) {  };
        }
    }
}