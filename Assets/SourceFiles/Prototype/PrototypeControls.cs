using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    public sealed class PrototypeControls : MonoBehaviour
    {
        public RectTransform UIRoot => safeArea;
        public bool TouchMode { get; private set; }
        public bool GameplayEnabled { get; private set; } = true;
        public bool CanPlay => GameplayEnabled && HasFocus && !waitingForRelease && Time.frameCount > inputResumeFrame;
        public bool WaitingForRelease => waitingForRelease;
        public bool DefeatVisible => defeatPanel != null && defeatPanel.gameObject.activeSelf;
        public bool VictoryVisible => DefeatVisible && waves != null && waves.Victory;
        private PrototypeWaves waves;
        private Text waveLabel, endTitle;
        public Vector2 RestartCenter => RectTransformUtility.WorldToScreenPoint(null, restartButton.position);
        private static bool restartPending;
        private bool waitingForRelease, requireFreshPointer, questionPointerMustMove;
        private int inputResumeFrame = -1;
        private RectTransform defeatPanel, restartButton;
        private Text healthLabel;
        private Image healthFill;
        private PrototypePlayerHealth playerHealth;
        public bool HasFocus => Application.isFocused && PrototypeBrowserInput.Focused;
        public bool AttackActive => Time.unscaledTime < attackUntil;
        public int AttackCount { get; private set; }
        public Vector2 JoystickValue => CanPlay ? joystick.Value : Vector2.zero;
        public Vector2 JoystickCenter => RectTransformUtility.WorldToScreenPoint(null, joystick.transform.position);
        public Vector2 AttackCenter => RectTransformUtility.WorldToScreenPoint(null, attackButton.position);
        public Vector2 HudCenter => RectTransformUtility.WorldToScreenPoint(null, hud.position);
        public float JoystickScreenRadius => joystick.Radius * canvas.scaleFactor;
        private Canvas canvas;
        private RectTransform safeArea, attackButton, hud;
        private PrototypeJoystick joystick;
        private GraphicRaycaster raycaster;
        private EventSystem eventSystem;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData uiPointer;
        private Text instruction;
        private string normalInstruction;
        private Sprite circle;
        private Texture2D circleTexture;
        private float attackUntil;
        private int epoch;
        private bool pointerReady, showedAttack;
        private Vector2 lastPointer;
        private Rect lastSafeArea;
        private bool focusClockPaused;
        private float scaleBeforeFocusLoss;

        private void Awake()
        {
            PrototypeBrowserInput.Initialize();
            TouchMode = PrototypeBrowserInput.TouchMode;
            epoch = PrototypeBrowserInput.Epoch;
            playerHealth = GetComponent<PrototypePlayerHealth>();
            waves = GetComponent<PrototypeWaves>();
            waitingForRelease = restartPending;
            requireFreshPointer = restartPending;
            restartPending = false;
            lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            CreateUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void PrepareInput()
        {
            if (!HasFocus && !focusClockPaused)
            {
                scaleBeforeFocusLoss = Time.timeScale;
                Time.timeScale = 0;
                focusClockPaused = true;
            }
            else if (HasFocus && focusClockPaused)
            {
                Time.timeScale = scaleBeforeFocusLoss;
                focusClockPaused = false;
            }
            if (waitingForRelease && !AnyPointerPressed())
            {
                waitingForRelease = false;
                // Ignore the reload frame's release/click before accepting fresh input.
                inputResumeFrame = Time.frameCount;
            }
            if (playerHealth != null)
            {
                healthLabel.text = "Health " + playerHealth.Health + " / " + playerHealth.MaximumHealth;
                healthFill.rectTransform.anchorMax = new Vector2((float)playerHealth.Health / playerHealth.MaximumHealth, 1);
                defeatPanel.gameObject.SetActive(playerHealth.IsDefeated || (waves != null && waves.Victory));
                endTitle.text = playerHealth.IsDefeated ? "DEFEATED" : "VICTORY";
                restartButton.GetComponentInChildren<Text>().text = playerHealth.IsDefeated ? "Retry" : "New Run";
                waveLabel.text = waves == null ? "" : "Wave " + waves.WaveNumber + " / " + waves.WaveCount
                    + (waves.OnBreak ? " — Next wave in " + Mathf.CeilToInt(waves.BreakRemaining) + "s" : "");
            }
            if (!HasFocus || epoch != PrototypeBrowserInput.Epoch)
            {
                Suspend();
                waitingForRelease = true;
                requireFreshPointer = true;
                lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                epoch = PrototypeBrowserInput.Epoch;
            }
            if (lastSafeArea != Screen.safeArea) UpdateSafeArea();
            if (showedAttack == AttackActive) return;
            showedAttack = AttackActive;
            instruction.text = showedAttack ? "FIRED\n" + normalInstruction : normalInstruction;
            instruction.color = showedAttack ? new Color(1, 0.9f, 0.2f) : Color.white;
        }

        public bool ReadDesktopPointer(out Vector2 position, out bool changed)
        {
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            changed = false;
            if (TouchMode || !CanPlay || Mouse.current == null || !PrototypeBrowserInput.Inside
                || position.x < 0 || position.y < 0 || position.x >= Screen.width || position.y >= Screen.height
                || IsOverUI(position))
            {
                pointerReady = false;
                return false;
            }
            if (requireFreshPointer)
            {
                if ((position - lastPointer).sqrMagnitude < 9
                    && (questionPointerMustMove || !Mouse.current.leftButton.wasPressedThisFrame)) return false;
                questionPointerMustMove = false;
                requireFreshPointer = false;
            }
            // Browser pixel rounding can turn subpixel noise into a one-pixel event.
            changed = !pointerReady || (position - lastPointer).sqrMagnitude >= 9;
            if (changed) lastPointer = position;
            pointerReady = true;
            if (Mouse.current.leftButton.wasPressedThisFrame) RequestAttack();
            return true;
        }

        private bool IsOverUI(Vector2 position)
        {
            uiPointer.position = position;
            uiHits.Clear();
            // Explicit raycast avoids relying on EventSystem's previous-frame hover state.
            eventSystem.RaycastAll(uiPointer, uiHits);
            return uiHits.Count != 0;
        }

        public void RequestAttack()
        {
            if (!CanPlay) return;
            var combat = GetComponent<PrototypeCombat>();
            if (combat == null || !combat.TryAttack()) return;
            AttackCount++;
            attackUntil = Time.unscaledTime + 0.25f;
        }

        public void ResumeAfterQuestion()
        {
            Suspend();
            waitingForRelease = true;
            requireFreshPointer = true;
            questionPointerMustMove = true;
            lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            inputResumeFrame = Time.frameCount;
            GameplayEnabled = true;
        }

        public static void PrepareRestart() => restartPending = true;

        public void SetGameplayEnabled(bool enabled)
        {
            GameplayEnabled = enabled;
            if (!enabled) Suspend();
        }

        public static bool AnyPointerPressed()
        {
            if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed
                || Mouse.current.middleButton.isPressed || Mouse.current.backButton.isPressed
                || Mouse.current.forwardButton.isPressed)) return true;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed) return true;
            return false;
        }

        public void Suspend()
        {
            pointerReady = false;
            joystick?.ResetInput();
            attackUntil = 0;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Suspend(); }
        private void OnApplicationPause(bool paused) { if (paused) Suspend(); }
        private void OnDisable() => Suspend();
        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            if (circle != null) Destroy(circle);
            if (circleTexture != null) Destroy(circleTexture);
        }

        private void CreateUI()
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var events = new GameObject("Prototype UI Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem = events.GetComponent<EventSystem>();
            }
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null) inputModule.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
            uiPointer = new PointerEventData(eventSystem);
            var root = new GameObject("Prototype Controls", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.matchWidthOrHeight = 0.5f;
            raycaster = root.GetComponent<GraphicRaycaster>();
            safeArea = Rect("Safe Area", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UpdateSafeArea();
            hud = Rect("Instructions", safeArea, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -48), new Vector2(-24, 72));
            var panel = hud.gameObject.AddComponent<Image>();
            panel.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
            normalInstruction = TouchMode ? "Left thumb: move    Right thumb: attack\nNearest target in range"
                : "Move the mouse over the floor to move\nLeft click: fire at nearest target in range";
            instruction = Label("Instructions Text", hud, normalInstruction, 20);
            circle = CreateCircle();
            var pad = Rect("Left Thumb Joystick", safeArea, Vector2.zero, Vector2.zero,
                new Vector2(120, 120), new Vector2(180, 180));
            var padImage = pad.gameObject.AddComponent<Image>();
            padImage.sprite = circle;
            padImage.color = new Color(0.08f, 0.2f, 0.3f, 0.9f);
            var knob = Rect("Joystick Knob", pad, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(64, 64));
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = new Color(0.2f, 0.85f, 0.9f);
            knobImage.raycastTarget = false;
            joystick = pad.gameObject.AddComponent<PrototypeJoystick>();
            joystick.Owner = this;
            joystick.Knob = knob;
            attackButton = Rect("Right Thumb Attack", safeArea, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-115, 115), new Vector2(150, 150));
            var attackImage = attackButton.gameObject.AddComponent<Image>();
            attackImage.sprite = circle;
            attackImage.color = new Color(0.8f, 0.2f, 0.12f, 0.95f);
            Label("Attack Label", attackButton, "ATTACK", 22);
            attackButton.gameObject.AddComponent<PrototypeAttackButton>().Owner = this;
            pad.gameObject.SetActive(TouchMode);
            attackButton.gameObject.SetActive(TouchMode);
            CreateHealthAndDefeatUI();
        }

        private void CreateHealthAndDefeatUI()
        {
            var healthBar = Rect("Player Health", safeArea, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -105), new Vector2(280, 30));
            healthBar.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.07f, 0.95f);
            var fillRect = Rect("Player Health Fill", healthBar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            healthFill = fillRect.gameObject.AddComponent<Image>();
            healthFill.color = new Color(0.15f, 0.6f, 0.3f);
            healthFill.raycastTarget = false;
            healthLabel = Label("Player Health Text", healthBar, "Health", 20);
            var waveBar = Rect("Wave Status", safeArea, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -140), new Vector2(440, 30));
            waveLabel = Label("Wave Text", waveBar, "Wave 1 / 3", 20);

            defeatPanel = Rect("Defeat Screen", safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            defeatPanel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
            var title = Rect("Defeat Title", defeatPanel, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0, 55), new Vector2(280, 70));
            endTitle = Label("Defeat Text", title, "DEFEATED", 34);
            restartButton = Rect("Restart", defeatPanel, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0, -35), new Vector2(240, 64));
            restartButton.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.5f, 0.4f);
            Label("Restart Text", restartButton, "RESTART", 26);
            restartButton.gameObject.AddComponent<Button>().onClick.AddListener(() => playerHealth?.Restart());
            defeatPanel.gameObject.SetActive(false);
        }

        private void UpdateSafeArea()
        {
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        }

        internal static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static Text Label(string name, RectTransform parent, string text, int fontSize)
        {
            var label = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private Sprite CreateCircle()
        {
            circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (var y = 0; y < 64; y++)
                for (var x = 0; x < 64; x++)
                    circleTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)))));
            circleTexture.Apply();
            return Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
        }
    }
}
