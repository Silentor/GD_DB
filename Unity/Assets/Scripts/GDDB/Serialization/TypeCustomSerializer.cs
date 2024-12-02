using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;
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

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadVector3();
        }
    }

    public class Vector3IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( Vector3Int );

        public override JSONNode Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector3Int) obj;
            var result  = new JSONArray
                          {
                                  [ 0 ] = vector3[ 0 ],
                                  [ 1 ] = vector3[ 1 ],
                                  [ 2 ] = vector3[ 2 ]
                          };
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            var vector3Values = json.AsArray;
            var obj           = new UnityEngine.Vector3Int();
            obj[0] = vector3Values[0];
            obj[1] = vector3Values[1];
            obj[2] = vector3Values[2];
            return obj;
        }
    }


    public class Vector2Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2 );

        public override JSONNode Serialize(  Object obj )
        {
            var vec = (UnityEngine.Vector2) obj;
            var result = new JSONArray();
            result.WriteVector2( vec );
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadVector2();
        }
    }

    public class Vector2IntSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector2Int );

        public override JSONNode Serialize(  Object obj )
        {
            var vector3 = (UnityEngine.Vector2Int) obj;
            var result  = new JSONArray
                          {
                                  [ 0 ] = vector3[ 0 ],
                                  [ 1 ] = vector3[ 1 ],
                          };
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            var vector3Values = json.AsArray;
            var obj           = new UnityEngine.Vector2Int();
            obj[0] = vector3Values[0];
            obj[1] = vector3Values[1];
            return obj;
        }
    }

    public class QuaternionSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Quaternion );

        public override JSONNode Serialize(  Object obj )
        {
            var value = (UnityEngine.Quaternion) obj;
            var result = new JSONArray();
            result.WriteQuaternion( value );
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadQuaternion();
        }
    }

    public class BoundsSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Bounds );

        public override JSONNode Serialize(  Object obj )
        {
            var value = (UnityEngine.Bounds) obj;
            var result = new JSONArray();
            result[0] = value.center.x;
            result[1] = value.center.y;
            result[2] = value.center.z;
            result[3] = value.size.x;
            result[4] = value.size.y;
            result[5] = value.size.z;
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            var quatValues = json.AsArray;
            //var obj        = new UnityEngine.Bounds();
            var center = new UnityEngine.Vector3(
                    quatValues[0],
                    quatValues[1],
                    quatValues[2] );
            var size = new UnityEngine.Vector3(
                    quatValues[3],
                    quatValues[4],
                    quatValues[5] );

            return new Bounds( center, size );
        }
    }

    public class RectSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Rect );

        public override JSONNode Serialize(  Object obj )
        {
            var value = (UnityEngine.Rect) obj;
            var result = new JSONArray();
            result.WriteRect( value );
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadRect();
        }
    }

    public class ColorSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color );

        public override JSONNode Serialize(  Object obj )
        {
            var value  = (UnityEngine.Color) obj;
            var result = new JSONArray();
            result.WriteColor( value );
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadColor();
        }
    }

    public class Color32Serializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Color32 );

        public override JSONNode Serialize(  Object obj )
        {
            var value  = (UnityEngine.Color32) obj;
            var result = new JSONArray();
            result.WriteColor32( value );
            return result;
        }

        public override Object Deserialize( JSONNode json )
        {
            return json.ReadColor32();
        }
    }

    public class AnimationCurveSerializer : TypeCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.AnimationCurve );

        public override JSONNode Serialize(  Object obj )
        {
            var result = new JSONObject();
            var keys = new JSONArray();
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

        public override Object Deserialize( JSONNode json )
        {
            var ac        = (JSONObject)json;
            var keysToken = (JSONArray)ac[ "Keys" ];
            var keys      = new List<Keyframe>();
            for ( int i = 0; i < keysToken.Count; i++ )
            {
                keys.Add( DeserializeKey( (JSONObject)keysToken[ i ] ) );                
            }

            var result = new AnimationCurve( keys.ToArray() );
            result.preWrapMode  = (WrapMode)ac[ "PreWrapMode" ].AsInt;
            result.postWrapMode = (WrapMode)ac[ "PostWrapMode" ].AsInt;

            return result;
        }

        private JSONObject SerializeKey( Keyframe key )
        {
            var result = new JSONObject();
            result.Add( "Time",         key.time );
            result.Add( "Value",        key.value );
            result.Add( "InTangent",    key.inTangent );
            result.Add( "OutTangent",   key.outTangent );
            result.Add( "InWeight",     key.inWeight );
            result.Add( "OutWeight",    key.outWeight );
            result.Add( "WeightedMode", (Int32)key.weightedMode );
            return result;
        }

        private Keyframe DeserializeKey( JSONObject key )
        {
            var result = new Keyframe(
                    key[ "Time" ],
                    key[ "Value" ],
                    key[ "InTangent" ],
                    key[ "OutTangent" ],
                    key[ "InWeight" ],
                    key[ "OutWeight" ]
            )
            {
                    weightedMode = (WeightedMode)key[ "WeightedMode" ].AsInt
            };
            return result;
        }
    }
}

