using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using Object = System.Object;

namespace GDDB.Serialization
{
    [RequireDerived]
    public abstract class TypeCustomSerializer
    {
        public abstract Type SerializedType { get; }

        public abstract JToken Serialize(  Object obj );

        public abstract Object Deserialize( JsonReader json );
    }

    public class Vector3Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( Vector3 );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector3) obj;
            var result = new JArray { vector3.x, vector3.y, vector3.z };
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var result = new Vector3( 
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value );
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }

    public class Vector3IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( Vector3Int );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector3Int) obj;
            var result  = new JArray { vector3.x, vector3.y, vector3.z };
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var result = new Vector3Int( 
                    json.ReadAsInt32().Value,
                    json.ReadAsInt32().Value,
                    json.ReadAsInt32().Value );
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }


    public class Vector2Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2 );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector2) obj;
            var result  = new JArray { vector3.x, vector3.y };
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var result = new Vector2( 
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value );
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }

    public class Vector2IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2Int );

        public override JToken Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector2Int) obj;
            var result  = new JArray { vector3.x, vector3.y };
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var result = new Vector2Int( 
                    json.ReadAsInt32().Value,
                    json.ReadAsInt32().Value );
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }

    public class QuaternionSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Quaternion );

        public override JToken Serialize(  Object obj )
        {
            var value = (UnityEngine.Quaternion) obj;
            var result  = new JArray { value.x, value.y, value.z, value.w };
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var result = new Quaternion( 
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value );
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }

    public class BoundsSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Bounds );

        public override JToken Serialize(  Object obj )
        {
            var value  = (UnityEngine.Bounds) obj;
            var result = new JArray(value.center.x,
                                 value.center.y,
                                 value.center.z,
                                 value.size.x,
                                 value.size.y,
                                 value.size.z);
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var center = new UnityEngine.Vector3(
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value
                    );
            var size = new UnityEngine.Vector3(
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value
                    );
            json.EnsureNextToken( JsonToken.EndArray );

            return new Bounds( center, size );
        }
    }

    public class RectSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Rect );

        public override JToken Serialize(  Object obj )
        {
            var value  = (UnityEngine.Rect) obj;
            var result = new JArray(value.position.x,
                                 value.position.y,
                                 value.size.x,
                                 value.size.y);
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var center = new UnityEngine.Vector2(
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value
            );
            var size = new UnityEngine.Vector2(
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value
            );
            json.EnsureNextToken( JsonToken.EndArray );

            return new Rect( center, size );
        }
    }

    public class ColorSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color );

        public override JToken Serialize(  Object obj )
        {
            var value  = (UnityEngine.Color) obj;
            var result = new JArray(value.r,
                                 value.g,
                                 value.b,
                                 value.a);
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var value = new Color( 
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value,
                    (Single)json.ReadAsDouble().Value
                    );
            json.EnsureNextToken( JsonToken.EndArray );

            return value;
        }
    }

    public class Color32Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color32 );

        public override JToken Serialize(  Object obj )
        {
            var value  = (UnityEngine.Color32) obj;
            var result = new JArray(value.r,
                                 value.g,
                                 value.b,
                                 value.a);
            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var value = new Color32( 
                    (Byte)json.ReadAsInt32().Value,
                    (Byte)json.ReadAsInt32().Value,
                    (Byte)json.ReadAsInt32().Value,
                    (Byte)json.ReadAsInt32().Value
            );
            json.EnsureNextToken( JsonToken.EndArray );

            return value;
        }
    }

    public class AnimationCurveSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.AnimationCurve );

        public override JToken Serialize(  Object obj )
        {
            var result = new JArray();
                                    
            var ac     = (UnityEngine.AnimationCurve) obj;
            result.Add( ac.keys.Length );
            result.Add( (Int32)ac.preWrapMode );
            result.Add( (Int32)ac.postWrapMode );
            foreach ( var key in ac.keys )
            {
                result.Add( SerializeKey( key ) );
            }

            return result;
        }

        public override Object Deserialize( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartArray );
            var count = json.ReadAsInt32().Value;
            var preWrapMode  = (WrapMode)json.ReadAsInt32().Value;
            var postWrapMode = (WrapMode)json.ReadAsInt32().Value;
            var keys = new Keyframe[ count ];
            for ( int i = 0; i < count; i++ )
            {
                 keys.SetValue( DeserializeKey( json ), i );
            }
            var result = new AnimationCurve( keys );
            result.preWrapMode  = preWrapMode;
            result.postWrapMode = postWrapMode;
            json.EnsureNextToken( JsonToken.EndArray );

            return result;
        }

        private JArray SerializeKey( Keyframe key )
        {
            var result = new JArray(key.time,
                                 key.value,
                                 key.inTangent,
                                 key.outTangent,
                                 key.inWeight,
                                 key.outWeight,
                                 (Int32)key.weightedMode );
            return result;
        }

        private Keyframe DeserializeKey( JsonReader json )
        {
            json.EnsureNextToken( JsonToken.StartArray );
            var time = (Single)json.ReadAsDouble().Value;
            var value = (Single)json.ReadAsDouble().Value;
            var inTangent = (Single)json.ReadAsDouble().Value;
            var outTangent = (Single)json.ReadAsDouble().Value;
            var inWeight = (Single)json.ReadAsDouble().Value;
            var outWeight = (Single)json.ReadAsDouble().Value;
            var weightedMode = (WeightedMode)json.ReadAsInt32().Value;
            var result = new Keyframe( time, value, inTangent, outTangent, inWeight, outWeight );
            result.weightedMode = weightedMode;
            json.EnsureNextToken( JsonToken.EndArray );
            return result;
        }
    }
}

