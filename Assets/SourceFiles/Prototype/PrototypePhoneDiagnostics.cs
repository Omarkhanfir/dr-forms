#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DrForms.Prototype
{
    // Temporary, read-only Web development diagnostics. No input or gameplay setters.
    [DefaultExecutionOrder(1000)]
    public sealed class PrototypePhoneDiagnostics : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PrototypeDiagnostics_Report(string json);
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_WEBGL && DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (!Application.absoluteURL.Contains("devDiagnostics=1")) return;
            var host = new GameObject("Temporary Phone Diagnostics");
            DontDestroyOnLoad(host);
            host.AddComponent<PrototypePhoneDiagnostics>();
#endif
        }

        private PrototypeControls controls;
        private PrototypeQuestions questions;
        private PrototypeWaves waves;
        private PrototypePlayerHealth health;
        private PrototypeCombat combat;
        private PrototypeJoystick joystick;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private readonly Queue<string> errors = new Queue<string>();
        private float nextReport;
        private int unityErrorCount, touchStarts, peakTouches;
        private string lastHit = "none";
        private Vector2 lastTouch;
        private bool sawTouch;

        private void Awake() => Application.logMessageReceived += CaptureLog;
        private void OnDestroy() => Application.logMessageReceived -= CaptureLog;
        private void CaptureLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            unityErrorCount++;
            var text = type + ": " + message + " " + stack;
            errors.Enqueue(text.Length > 1000 ? text.Substring(0, 1000) : text);
            while (errors.Count > 3) errors.Dequeue();
        }

        private void LateUpdate()
        {
            var pressed = 0;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed) { pressed++; lastTouch = touch.position.ReadValue(); sawTouch = true; }
                    if (touch.press.wasPressedThisFrame) touchStarts++;
                }
            peakTouches = Mathf.Max(peakTouches, pressed);
            if (Time.realtimeSinceStartup < nextReport) return;
            nextReport = Time.realtimeSinceStartup + 0.25f;
            if (controls == null)
            {
                controls = FindFirstObjectByType<PrototypeControls>();
                if (controls != null)
                {
                    questions = controls.GetComponent<PrototypeQuestions>();
                    waves = controls.GetComponent<PrototypeWaves>();
                    health = controls.GetComponent<PrototypePlayerHealth>();
                    combat = controls.GetComponent<PrototypeCombat>();
                    joystick = FindFirstObjectByType<PrototypeJoystick>();
                }
            }
            var events = EventSystem.current;
            if (events != null && sawTouch)
            {
                hits.Clear();
                events.RaycastAll(new PointerEventData(events) { position = lastTouch }, hits);
                lastHit = hits.Count == 0 ? "none" : hits[0].gameObject.name;
            }
            var state = controls == null ? "startup/no player" : health != null && health.IsDefeated ? "defeat"
                : waves != null && waves.Victory ? "victory" : questions != null && questions.Paused ? "question"
                : !controls.GameplayEnabled ? "blocked" : waves != null && waves.OnBreak ? "wave break" : "combat";
            var snapshot = new Snapshot
            {
                build = PrototypeControls.BuildId, frame = Time.frameCount, realtime = Time.realtimeSinceStartup, gameTime = Time.time, timeScale = Time.timeScale,
                state = state, applicationFocus = Application.isFocused, bridgeFocus = PrototypeBrowserInput.Focused,
                focus = controls != null && controls.HasFocus, epoch = PrototypeBrowserInput.Epoch,
                canPlay = controls != null && controls.CanPlay, gameplayEnabled = controls != null && controls.GameplayEnabled,
                waitingForRelease = controls != null && controls.WaitingForRelease,
                controlsEnabled = controls != null && controls.isActiveAndEnabled,
                touchMode = controls != null && controls.TouchMode, unityTouchDevice = Touchscreen.current != null,
                unityTouches = pressed, unityTouchStarts = touchStarts, peakUnityTouches = peakTouches,
                pointerPressed = PrototypeControls.AnyPointerPressed(), lastTouchHit = lastHit,
                eventSystem = events != null && events.isActiveAndEnabled,
                uiModule = events != null && events.currentInputModule != null
                    ? events.currentInputModule.GetType().Name + "/" + events.currentInputModule.isActiveAndEnabled : "none",
                selectedUI = events != null && events.currentSelectedGameObject != null ? events.currentSelectedGameObject.name : "none",
                questionOverlay = questions != null && questions.Paused,
                endOverlay = controls != null && controls.DefeatVisible,
                wave = waves != null ? waves.WaveNumber : 0, health = health != null ? health.Health : -1,
                joystick = joystick != null ? joystick.Value : Vector2.zero,
                shots = combat != null ? combat.ShotsFired : 0,
                position = controls != null ? controls.transform.position : Vector3.zero,
                unityErrorCount = unityErrorCount, unityErrors = errors.ToArray()
            };
#if UNITY_WEBGL && !UNITY_EDITOR
            PrototypeDiagnostics_Report(JsonUtility.ToJson(snapshot));
#endif
        }

        [System.Serializable]
        private struct Snapshot
        {
            public int frame, epoch, unityTouches, unityTouchStarts, peakUnityTouches, wave, health, shots, unityErrorCount;
            public float realtime, gameTime, timeScale;
            public bool applicationFocus, bridgeFocus, focus, canPlay, gameplayEnabled, waitingForRelease, controlsEnabled;
            public bool touchMode, unityTouchDevice, pointerPressed, eventSystem, questionOverlay, endOverlay;
            public string build, state, lastTouchHit, uiModule, selectedUI;
            public Vector2 joystick;
            public Vector3 position;
            public string[] unityErrors;
        }
    }
}
#endif
