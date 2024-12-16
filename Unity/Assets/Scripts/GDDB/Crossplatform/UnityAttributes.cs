using System;

namespace UnityEngine
{
#if !UNITY_2021_2_OR_NEWER
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeReference : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
      /// <summary>
      ///   <para>The display name for this type shown in the Assets/Create menu.</para>
      /// </summary>
      public string menuName { get; set; }

      /// <summary>
      ///   <para>The default file name used by newly created instances of this type.</para>
      /// </summary>
      public string fileName { get; set; }

      /// <summary>
      ///   <para>The position of the menu item within the Assets/Create menu.</para>
      /// </summary>
      public int order { get; set; }
    }

   public sealed class SerializeField : Attribute
  {
  }

#endif
}