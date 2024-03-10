using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDDB
{
    public abstract class GdJsonCustomSerializer
    {
        public abstract Type SerializedType { get; }

        public abstract void Serialize(  Object obj, JsonWriter writer );

        public abstract Object Deserialize( JToken json );
    }

    public class Vector3Serializer : GdJsonCustomSerializer
    {
        public override Type SerializedType => typeof( UnityEngine.Vector3 );

        public override void Serialize(  Object obj, JsonWriter writer )
        {
            var vector3 = (UnityEngine.Vector3) obj;

            var oldFormatting = writer.Formatting;
            writer.Formatting = Formatting.None;

            writer.WriteStartArray();
            writer.WriteValue( vector3.x );
            writer.WriteValue( vector3.y );
            writer.WriteValue( vector3.z );
            writer.WriteEndArray();

            writer.Formatting = oldFormatting;

            //return new Single[] { vector3.x, vector3.y, vector3.z };
            //return $"{{\"x\":{obj.x},\"y\":{obj.y},\"z\":{obj.z}}}";
            //return String.Format( CultureInfo.InvariantCulture, "[ {0}, {1}, {2} ]", vector3.x, vector3.y, vector3.z );
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
}