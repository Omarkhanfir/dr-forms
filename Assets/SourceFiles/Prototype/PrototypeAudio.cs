using System.Runtime.InteropServices;
using UnityEngine;
namespace DrForms.Prototype
{
    // Browser synthesis uses no downloaded audio. A trusted canvas gesture unlocks
    // the context; returning from background resumes it on the next fresh gesture.
    internal static class PrototypeAudio
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PrototypeAudio_Init();
        [DllImport("__Internal")] private static extern int PrototypeAudio_Muted();
        [DllImport("__Internal")] private static extern void PrototypeAudio_Toggle();
        [DllImport("__Internal")] private static extern void PrototypeAudio_Play(int kind);
        public static void Initialize() => PrototypeAudio_Init();
        public static bool Muted => PrototypeAudio_Muted() != 0;
        public static void ToggleMute() => PrototypeAudio_Toggle();
        public static void Play(int kind) => PrototypeAudio_Play(kind);
#else
        public static void Initialize() { }
        public static bool Muted => PlayerPrefs.GetInt("GardenQuestMute", 0) != 0;
        public static void ToggleMute() { PlayerPrefs.SetInt("GardenQuestMute", Muted ? 0 : 1); PlayerPrefs.Save(); }
        public static void Play(int kind) { } // Procedural browser sound is Web-only.
#endif
    }
}
