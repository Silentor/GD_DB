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
            var t = new Test1()
                    {
                            StructValue = new Test2()
                                    {
                                            Value = 5
                                    }
                    };
            
            var str    = new StringWriter();
            var writer = new JsonTextWriter( str );
            var stream = JsonConvert.SerializeObject( t );
            Debug.Log( stream );
            
            var output = JsonConvert.DeserializeObject<Test1>( stream );
            
            Debug.Log( JsonConvert.SerializeObject( new Single[]{ 1.1f, 2, 3.5f } ) );

            foreach ( var member in typeof(Test2).GetMembers( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) )
            {
                Debug.Log( member.ToString() );                
            }
            


            var gddb = GetGD( "GD1" );
            Debug.Log( $"GD DB {gddb.Root.Id} loaded, objects count {gddb.AllObjects.Count}" );
            var img = FindObjectOfType<RawImage>();
            img.texture = gddb.GetComponents<GDComponentChild3>().First().TexValue;

            var _gddb2 = GetGD( "GD2" );
            Debug.Log( $"GD DB {_gddb2.Root.Id} loaded, objects count {_gddb2.AllObjects.Count}" );

            var jsongddb = new GdJsonLoader( "GD1" );
            Debug.Log( $"Read from json {jsongddb.Root.TestVector3}" );

            
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

    [Serializable]
    public class Test1
    {
        public Int32 Primitive = 2;
        [SerializeReference]
        public ITest StructValue;
    }

    public interface ITest{}

    [Serializable]
    public struct Test2 : ITest
    {
        public Int32 Value;

        //[NonSerialized]
        public Int32 _valueSecret;

        public Test2(Int32 value )
        {
            Value        = value;
            _valueSecret = 666;
        }
    }
}
