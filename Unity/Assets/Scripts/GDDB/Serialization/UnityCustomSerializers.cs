using System;
using UnityEngine;

namespace GDDB.Serialization
{
    public class Vector3Serializer : TypeCustomSerializer<Vector3>
    {
        public override void Serialize(  WriterBase writer, Vector3 obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteValue( obj.z );
            writer.WriteEndArray( );
        }

        public override Vector3 Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Vector3(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue() );
            reader.ReadEndArray( );
            return result;
        }
    }

    public class Vector3IntSerializer : TypeCustomSerializer<Vector3Int>
    {
        public override void Serialize(  WriterBase writer, Vector3Int obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteValue( obj.z );
            writer.WriteEndArray( );
        }

        public override Vector3Int Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Vector3Int(
                    reader.ReadInt32Value(),
                    reader.ReadInt32Value(),
                    reader.ReadInt32Value() );
            reader.ReadEndArray( );
            return result;
        }
    }


    public class Vector2Serializer : TypeCustomSerializer<Vector2>
    {
        public override void Serialize(  WriterBase writer, Vector2 obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteEndArray( );
        }

        public override Vector2 Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Vector2(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue() );
            reader.ReadEndArray( );
            return result;
        }
    }

    public class Vector2IntSerializer : TypeCustomSerializer<Vector2Int>
    {
        public override void Serialize(  WriterBase writer, Vector2Int obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteEndArray( );
        }

        public override Vector2Int Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Vector2Int(
                    reader.ReadInt32Value(),
                    reader.ReadInt32Value() );
            reader.ReadEndArray( );
            return result;
        }
    }

    public class QuaternionSerializer : TypeCustomSerializer<Quaternion>
    {
        public override void Serialize(WriterBase writer, Quaternion obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteValue( obj.z );
            writer.WriteValue( obj.w );
            writer.WriteEndArray( );
        }

        public override Quaternion Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Quaternion(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue() );
            reader.ReadEndArray( );
            return result;
        }
    }

    public class BoundsSerializer : TypeCustomSerializer<Bounds>
    {
        public override void Serialize( WriterBase writer,  Bounds obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.center.x );
            writer.WriteValue( obj.center.y );
            writer.WriteValue( obj.center.z );
            writer.WriteValue( obj.size.x );
            writer.WriteValue( obj.size.y );
            writer.WriteValue( obj.size.z );
            writer.WriteEndArray( );
        }

        public override Bounds Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var center = new UnityEngine.Vector3(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue()
                    );
            var size = new UnityEngine.Vector3(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue()
                    );
            reader.ReadEndArray(  );

            return new Bounds( center, size );
        }
    }

    public class RectSerializer : TypeCustomSerializer<Rect>
    {
        public override void Serialize( WriterBase writer,  Rect obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.x );
            writer.WriteValue( obj.y );
            writer.WriteValue( obj.width );
            writer.WriteValue( obj.height );
            writer.WriteEndArray( );
        }

        public override Rect Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Rect(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue() );
            reader.ReadEndArray(  );

            return result;
        }
    }

    public class ColorSerializer : TypeCustomSerializer<Color>
    {
        public override void Serialize( WriterBase writer,  Color obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.r );
            writer.WriteValue( obj.g );
            writer.WriteValue( obj.b );
            writer.WriteValue( obj.a );
            writer.WriteEndArray( );
        }

        public override Color Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Color(
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue(),
                    reader.ReadSingleValue() );
            reader.ReadEndArray(  );

            return result;
        }
    }

    public class Color32Serializer : TypeCustomSerializer<Color32>
    {
        public override void Serialize( WriterBase writer,  Color32 obj )
        {
            writer.WriteStartArray( );
            writer.WriteValue( obj.r );
            writer.WriteValue( obj.g );
            writer.WriteValue( obj.b );
            writer.WriteValue( obj.a );
            writer.WriteEndArray( );
        }

        public override Color32 Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray( );
            var result = new Color32(
                    reader.ReadUInt8Value(),
                    reader.ReadUInt8Value(),
                    reader.ReadUInt8Value(),
                    reader.ReadUInt8Value()
                    );
            reader.ReadEndArray(  );

            return result;
        }
    }

    public class AnimationCurveSerializer : TypeCustomSerializer<AnimationCurve>
    {
        public override void Serialize(  WriterBase writer, AnimationCurve obj )
        {
            writer.WriteStartArray();
            writer.WriteValue( obj.keys.Length );
            writer.WriteValue( (Byte)obj.preWrapMode );
            writer.WriteValue( (Byte)obj.postWrapMode );
            foreach ( var key in obj.keys )
            {
                SerializeKey( writer, key );
            }
            writer.WriteEndArray();
        }

        public override AnimationCurve Deserialize( ReaderBase reader )
        {
            reader.EnsureStartArray();

            var count = reader.ReadIntegerValue();
            var preWrapMode  = (WrapMode)reader.ReadIntegerValue();
            var postWrapMode = (WrapMode)reader.ReadIntegerValue();
            var keys = new Keyframe[ count ];
            for ( int i = 0; i < count; i++ )
            {
                 keys.SetValue( DeserializeKey( reader ), i );
            }
            var result = new AnimationCurve( keys );
            result.preWrapMode  = preWrapMode;
            result.postWrapMode = postWrapMode;

            reader.ReadEndArray( );

            return result;
        }

        private void SerializeKey( WriterBase writer, Keyframe key )
        {
            writer.WriteStartArray();
            writer.WriteValue( key.time );
            writer.WriteValue( key.value );
            writer.WriteValue( key.inTangent );
            writer.WriteValue( key.outTangent );
            writer.WriteValue( key.inWeight );
            writer.WriteValue( key.outWeight );
            writer.WriteValue( (Byte)key.weightedMode );
            writer.WriteEndArray();
        }

        private Keyframe DeserializeKey( ReaderBase reader )
        {
            reader.ReadStartArray();
            var time = reader.ReadSingleValue();
            var value = reader.ReadSingleValue();
            var inTangent = reader.ReadSingleValue();
            var outTangent = reader.ReadSingleValue();
            var inWeight = reader.ReadSingleValue();
            var outWeight = reader.ReadSingleValue();
            var weightedMode = (WeightedMode)reader.ReadIntegerValue();
            var result = new Keyframe( time, value, inTangent, outTangent, inWeight, outWeight );
            result.weightedMode = weightedMode;
            reader.ReadEndArray();

            return result;
        }
    }
}

