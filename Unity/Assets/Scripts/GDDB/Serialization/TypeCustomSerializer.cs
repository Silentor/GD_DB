using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Serialization
{
    public abstract class TypeCustomSerializer
    {
        public abstract Type SerializedType { get; }

        public abstract JToken Serialize(  Object obj );

        public abstract Object Deserialize( JToken json );
    }

    public class Vector3Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( Vector3 );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector3) obj;

            return new JArray
                         {
                                 vector3.x,
                                 vector3.y,
                                 vector3.z
                         }                ;
        }

        public override Object Deserialize( JToken json )
        {
            var vector3Values = (JArray)json;
            var obj           = new UnityEngine.Vector3();
            obj.x = vector3Values[0].Value<Single>();
            obj.y = vector3Values[1].Value<Single>();
            obj.z = vector3Values[2].Value<Single>();
            return obj;
        }
    }

    public class Vector3IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( Vector3Int );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector3Int) obj;

            return new JArray
                   {
                           vector3.x,
                           vector3.y,
                           vector3.z
                   }                ;
        }

        public override Object Deserialize( JToken json )
        {
            var vector3Values = (JArray)json;
            var obj           = new UnityEngine.Vector3Int();
            obj.x = vector3Values[0].Value<Int32>();
            obj.y = vector3Values[1].Value<Int32>();
            obj.z = vector3Values[2].Value<Int32>();
            return obj;
        }
    }


    public class Vector2Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2 );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector2) obj;

            return new JArray
                   {
                           vector3.x,
                           vector3.y,
                   } ;
        }

        public override Object Deserialize( JToken json )
        {
            var vector3Values = (JArray)json;
            var obj           = new UnityEngine.Vector2();
            obj.x = vector3Values[0].Value<Single>();
            obj.y = vector3Values[1].Value<Single>();
            return obj;
        }
    }

    public class Vector2IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2Int );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector2Int) obj;

            return new JArray
                   {
                           vector3.x,
                           vector3.y,
                   } ;
        }

        public override Object Deserialize( JToken json )
        {
            var vector3Values = (JArray)json;
            var obj           = new UnityEngine.Vector2Int();
            obj.x = vector3Values[0].Value<Int32>();
            obj.y = vector3Values[1].Value<Int32>();
            return obj;
        }
    }

    public class QuaternionSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Quaternion );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Quaternion) obj;

            return new JArray
                   {
                           vector3.x,
                           vector3.y,
                           vector3.z,
                           vector3.w,
                   }                ;
        }

        public override Object Deserialize( JToken json )
        {
            var quatValues = (JArray)json;
            var obj        = new UnityEngine.Quaternion();
            obj.x = quatValues[0].Value<Single>();
            obj.y = quatValues[1].Value<Single>();
            obj.z = quatValues[2].Value<Single>();
            obj.w = quatValues[3].Value<Single>();
            return obj;
        }
    }

    public class BoundsSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Bounds );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Bounds) obj;
            var center  = vector3.center; 
            var size  = vector3.size; 

            return new JArray
                   {
                           center.x,
                           center.y,
                           center.z,
                           size.x,
                           size.y,
                           size.z,
                   }    ;
        }

        public override Object Deserialize( JToken json )
        {
            var quatValues = (JArray)json;
            //var obj        = new UnityEngine.Bounds();
            var center = new UnityEngine.Vector3(
                    quatValues[0].Value<Single>(),
                    quatValues[1].Value<Single>(),
                    quatValues[2].Value<Single>() );
            var size = new UnityEngine.Vector3(
                    quatValues[3].Value<Single>(),
                    quatValues[4].Value<Single>(),
                    quatValues[5].Value<Single>() );

            return new Bounds( center, size );
        }
    }

    public class RectSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Rect );

        public override JToken Serialize(  Object obj )
        {
            var rect = (UnityEngine.Rect) obj;

            return new JArray
                   {
                           rect.x,
                           rect.y,
                           rect.width,
                           rect.height
                   }    ;
        }

        public override Object Deserialize( JToken json )
        {
            var values = (JArray)json;
            var obj        = new UnityEngine.Rect(
                    values[0].Value<Single>(),
                    values[1].Value<Single>(),
                    values[2].Value<Single>(),
                    values[3].Value<Single>()
                    );

            return obj;
        }
    }

    public class ColorSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color );

        public override JToken Serialize(  Object obj )
        {
            var color = (UnityEngine.Color) obj;

            return new JArray
                   {
                           color.r,
                           color.g,
                           color.b,
                           color.a
                   }    ;
        }

        public override Object Deserialize( JToken json )
        {
            var values = (JArray)json;
            var obj = new UnityEngine.Color(
                    values[0].Value<Single>(),
                    values[1].Value<Single>(),
                    values[2].Value<Single>(),
                    values[3].Value<Single>()
            );

            return obj;
        }
    }

    public class Color32Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color32 );

        public override JToken Serialize(  Object obj )
        {
            var color = (UnityEngine.Color32) obj;

            return new JArray
                   {
                           color.r,
                           color.g,
                           color.b,
                           color.a
                   }    ;
        }

        public override Object Deserialize( JToken json )
        {
            var values = (JArray)json;
            var obj = new UnityEngine.Color32(
                    values[0].Value<Byte>(),
                    values[1].Value<Byte>(),
                    values[2].Value<Byte>(),
                    values[3].Value<Byte>()
            );

            return obj;
        }
    }

    public class AnimationCurveSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.AnimationCurve );

        public override JToken Serialize(  Object obj )
        {
            var result = new JObject();
            var keys = new JArray();
            result.Add( "Keys", keys );
            var ac     = (UnityEngine.AnimationCurve) obj;
            foreach ( var key in ac.keys )
            {
                 keys.Add( SerializeKey( key ) );
            }

            result.Add( "PreWrapMode", (Int32)ac.preWrapMode );
            result.Add( "PostWrapMode", (Int32)ac.postWrapMode );

            return result;
        }

        public override Object Deserialize( JToken json )
        {
            var ac        = (JObject)json;
            var keysToken = (JArray)ac[ "Keys" ];
            var keys      = new List<Keyframe>();
            foreach ( var keyToken in keysToken.Children() )
            {
                keys.Add( DeserializeKey( (JObject)keyToken ) );                
            }

            var result = new AnimationCurve( keys.ToArray() );
            result.preWrapMode  = (WrapMode)ac[ "PreWrapMode" ].Value<Int32>();
            result.postWrapMode = (WrapMode)ac[ "PostWrapMode" ].Value<Int32>();

            return result;
        }

        private JObject SerializeKey( Keyframe key )
        {
            return new JObject()
                   {
                            { "Time", key.time },
                            { "Value", key.value },
                            { "InTangent", key.inTangent },
                            { "OutTangent", key.outTangent },
                            { "InWeight", key.inWeight },
                            { "OutWeight", key.outWeight },
                            { "WeightedMode", (Int32)key.weightedMode }
                     };
       }

        private Keyframe DeserializeKey( JObject key )
        {
            var result = new Keyframe(
                    key[ "Time" ].Value<Single>(),
                    key[ "Value" ].Value<Single>(),
                    key[ "InTangent" ].Value<Single>(),
                    key[ "OutTangent" ].Value<Single>(),
                    key[ "InWeight" ].Value<Single>(),
                    key[ "OutWeight" ].Value<Single>()
            );
            result.weightedMode = (WeightedMode)key[ "WeightedMode" ].Value<Int32>();
            return result;
        }
    }
}

