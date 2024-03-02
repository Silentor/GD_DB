using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GDDB
{
    public class Runner : MonoBehaviour
    {

        private void Awake( )
        {
            var gddb = GetGD( "GD1" );
            Debug.Log( $"GD DB {gddb.Root.Id} loaded, objects count {gddb.AllObjects.Count}" );
            var img = FindObjectOfType<RawImage>();
            img.texture = gddb.GetComponents<GDComponentChild3>().First().TexValue;

            var _gddb2 = GetGD( "GD2" );
            Debug.Log( $"GD DB {_gddb2.Root.Id} loaded, objects count {_gddb2.AllObjects.Count}" );
        }

        private GdLoader GetGD( String name )
        {
#if UNITY_EDITOR
            return new GdEditorLoader( name );
#else
            return new GdScriptableLoader( name );
#endif
        }
    }
}
