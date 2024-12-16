using System;
using System.Reflection;
using GDDB;

namespace UnityEngine
{
#if !UNITY_2021_2_OR_NEWER
    public class Object : ISerializationCallbackReceiver 
    {
        public String name;

        public HideFlags hideFlags;

        public Int32 GetInstanceID( )
        {
            return GetHashCode();
        }

        public static implicit operator Boolean( Object? obj )
        {
            return obj != null;
        } 

        public virtual void OnBeforeSerialize( )
        {
            
        }

        public virtual void OnAfterDeserialize( )
        {
            
        }
    }

    [Flags]
    public enum HideFlags
    {
        /// <summary>
        ///   <para>A normal, visible object. This is the default.</para>
        /// </summary>
        None = 0,
        /// <summary>
        ///   <para>The object will not appear in the hierarchy.</para>
        /// </summary>
        HideInHierarchy = 1,
        /// <summary>
        ///   <para>It is not possible to view it in the inspector.</para>
        /// </summary>
        HideInInspector = 2,
        /// <summary>
        ///   <para>The object will not be saved to the Scene in the editor.</para>
        /// </summary>
        DontSaveInEditor = 4,
        /// <summary>
        ///   <para>The object is not editable in the Inspector.</para>
        /// </summary>
        NotEditable = 8,
        /// <summary>
        ///   <para>The object will not be saved when building a player.</para>
        /// </summary>
        DontSaveInBuild = 16, // 0x00000010
        /// <summary>
        ///   <para>The object will not be unloaded by Resources.UnloadUnusedAssets.</para>
        /// </summary>
        DontUnloadUnusedAsset = 32, // 0x00000020
        /// <summary>
        ///   <para>The object will not be saved to the Scene. It will not be destroyed when a new Scene is loaded. It is a shortcut for HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset.</para>
        /// </summary>
        DontSave = DontUnloadUnusedAsset | DontSaveInBuild | DontSaveInEditor, // 0x00000034
        /// <summary>
        ///   <para>The GameObject is not shown in the Hierarchy, not saved to the Scene, and not unloaded by Resources.UnloadUnusedAssets.</para>
        /// </summary>
        HideAndDontSave = DontSave | NotEditable | HideInHierarchy, // 0x0000003D
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>( ) where T : ScriptableObject
        {
            var  type     = typeof(T);
            return (T)CreateInstance( type );
        }

        public static ScriptableObject CreateInstance( Type type )
        {
            var constr  = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy, null, Array.Empty<Type>(), null);
            var  instance = (ScriptableObject)constr.Invoke(null);
            return instance;
        }
    }
#endif
}