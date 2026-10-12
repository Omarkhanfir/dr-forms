using System.Runtime.InteropServices;
using UnityEngine;

namespace DrForms.Prototype
{
    internal static class PrototypeBrowserInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PrototypeControls_Init();
        [DllImport("__Internal")] private static extern int PrototypeControls_Focused();
        [DllImport("__Internal")] private static extern int PrototypeControls_Inside();
        [DllImport("__Internal")] private static extern int PrototypeControls_Epoch();
        [DllImport("__Internal")] private static extern int PrototypeControls_Touch();
        public static void Initialize() => PrototypeControls_Init();
        public static bool Focused => PrototypeControls_Focused() != 0;
        public static bool Inside => PrototypeControls_Inside() != 0;
        public static int Epoch => PrototypeControls_Epoch();
        public static bool TouchMode => Application.isMobilePlatform || PrototypeControls_Touch() != 0;
#else
        public static void Initialize() { }
        public static bool Focused => Application.isFocused;
        public static bool Inside => true; // Native/editor pointer bounds are checked in screen coordinates.
        public static int Epoch => 0;
        public static bool TouchMode => Application.isMobilePlatform;
#endif
    }
}
