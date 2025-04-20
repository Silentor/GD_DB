namespace UnityEngine.Profiling
{
#if !UNITY_2021_2_OR_NEWER
    public class CustomSampler
    {
        public static CustomSampler Create( string name )
        {
            return new CustomSampler();
        }

        public void Begin()
        {
            
        }

        public void End()
        {
            
        }
    }
#endif
}