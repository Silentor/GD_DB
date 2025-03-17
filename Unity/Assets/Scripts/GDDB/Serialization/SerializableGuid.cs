using System;
using System.Runtime.InteropServices;

namespace GDDB.Serialization
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct SerializableGuid
    {
        [FieldOffset(0)]
        public Guid   Guid;
        [FieldOffset(0)]
        public UInt64 Part1;
        [FieldOffset(8)]
        public UInt64 Part2;

        public SerializableGuid(Guid guid ) : this()
        {
            Guid = guid;
        }

        public static implicit operator Guid(SerializableGuid serializableGuid) => serializableGuid.Guid;

        public static implicit operator SerializableGuid(Guid guid) => new SerializableGuid(guid);
    }

#if UNITY_EDITOR

    [UnityEditor.CustomPropertyDrawer(typeof(SerializableGuid))]
    public class SerializableGuidDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI( UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label )
        {
            UnityEditor.EditorGUI.BeginProperty( position, label, property );

            position = UnityEditor.EditorGUI.PrefixLabel( position, label );

            var guid = new SerializableGuid(){ Part1 = property.FindPropertyRelative( nameof(SerializableGuid.Part1) ).ulongValue, Part2 = property.FindPropertyRelative( nameof(SerializableGuid.Part2) ).ulongValue};
            UnityEditor.EditorGUI.LabelField( position, guid.Guid.ToString() );

            UnityEditor.EditorGUI.EndProperty( );
        }
    } 

#endif
}