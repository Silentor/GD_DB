using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu ( fileName = "TestGDObject", menuName = "Create TestGDObject", order = 0 )]
    public class TestGDObject : GDObject
    {
        private void Awake( )
        {
            Debug.Log( "Awake" );
        }

        private void OnEnable( )
        {
            Debug.Log( "OnEnable" );
        }
    }
}