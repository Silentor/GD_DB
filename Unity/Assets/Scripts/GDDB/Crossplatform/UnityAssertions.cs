using System;

namespace UnityEngine.Assertions
{
#if !UNITY_2021_2_OR_NEWER
    public static class Assert
    {

        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed");
            }
        }
       
    }
#endif
}