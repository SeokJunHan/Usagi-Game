using System.Runtime.InteropServices;

namespace UsagiGame
{
    public static class WebVibration
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void UsagiVibrate(int milliseconds);
#endif
        public static void Vibrate(int milliseconds)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UsagiVibrate(milliseconds);
#endif
        }
    }
}
