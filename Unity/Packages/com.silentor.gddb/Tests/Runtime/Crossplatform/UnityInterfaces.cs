namespace UnityEngine
{
#if !UNITY_2021_2_OR_NEWER
    public interface ISerializationCallbackReceiver
    {
        /// <summary>
        ///   <para>Implement this method to receive a callback before Unity serializes your object.</para>
        /// </summary>
        void OnBeforeSerialize();

        /// <summary>
        ///   <para>Implement this method to receive a callback after Unity deserializes your object.</para>
        /// </summary>
        void OnAfterDeserialize();
    }
#endif
}