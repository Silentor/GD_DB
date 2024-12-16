namespace UnityEngine
{
#if !UNITY_2021_2_OR_NEWER
    public class Debug
    {
        public static void LogError( String message )
        {
            System.Diagnostics.Debug.WriteLine( message );
        }

        public static void Log( String message )
        {
            System.Diagnostics.Debug.WriteLine( message );
        }
    }
#endif
}