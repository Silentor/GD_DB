using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace GDDB
{
    public class Runner : MonoBehaviour
    {

        private void Awake( )
        {
            var soLoader = new GdJsonLoader( "GD1" );
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
