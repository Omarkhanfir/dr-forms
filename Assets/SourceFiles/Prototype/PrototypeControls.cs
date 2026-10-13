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
        public bool InputReady => HasFocus && epoch == PrototypeBrowserInput.Epoch
            && !waitingForRelease && Time.frameCount > inputResumeFrame;
        public const string BuildId = "COURTYARD-ART-1";
        public bool Started { get; private set; }
        public bool ManualPaused { get; private set; }
        public bool CanPlay => GameplayEnabled && Started && !ManualPaused && InputReady;
        public Vector2 PlayCenter => Center(playButton);
        public Vector2 PauseCenter => Center(pauseButton);
        public Vector2 MuteCenter => Center(muteButton);
        public Vector2 ResumeCenter => Center(resumeButton);
        public Vector2 MenuRestartCenter => Center(menuRestartButton);
        private static Vector2 Center(RectTransform rect) => rect != null ? RectTransformUtility.WorldToScreenPoint(null, rect.position) : Vector2.zero;
        private RectTransform menu, playButton, pauseButton, muteButton, resumeButton, menuRestartButton;
        private Text summary, muteLabel, menuMuteLabel, graceLabel, rewardLabel;
        private float rewardUntil;
        private RectTransform menuSound;
        public Vector2 MenuSoundCenter => Center(menuSound);
        private int lastHealth = -1, lastWave = -1, lastBreak = -1, summaryDamage = -1;
        private bool wasDefeated;
        private PrototypeQuestions questions;
        private static bool returnToPlay;
        private float menuScale = 1;
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
            questions = GetComponent<PrototypeQuestions>();
            PrototypeAudio.Initialize();
            Started = returnToPlay;
            returnToPlay = false;
            waitingForRelease = restartPending;
            requireFreshPointer = restartPending;
            restartPending = false;
            lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            CreateUI();
            CreateMenu();
            gameObject.AddComponent<PrototypeCharacterVisual>();
            gameObject.AddComponent<PrototypeGarden>();
            if (!Started) { menuScale = Time.timeScale; Time.timeScale = 0; GameplayEnabled = false; }
            menu.gameObject.SetActive(!Started);
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
                // Focus owns only its own pause. A question/end/configuration
                // pause that is still active must never be lifted by recovery.
                Time.timeScale = GameplayEnabled && Started && !ManualPaused ? scaleBeforeFocusLoss : 0;
                playerHealth?.GrantGrace();
                focusClockPaused = false;
            }
            if (HasFocus && epoch == PrototypeBrowserInput.Epoch && waitingForRelease && !AnyPointerPressed())
            {
                waitingForRelease = false;
                // Ignore the reload frame's release/click before accepting fresh input.
                inputResumeFrame = Time.frameCount;
                if (Started && GameplayEnabled && !ManualPaused) playerHealth?.GrantGrace();
            }
            if (playerHealth != null)
            {
                if (lastHealth != playerHealth.Health) { lastHealth = playerHealth.Health;
                    healthLabel.text = "Health " + playerHealth.Health + " / " + playerHealth.MaximumHealth; }
                healthFill.rectTransform.anchorMax = new Vector2((float)playerHealth.Health / playerHealth.MaximumHealth, 1);
                defeatPanel.gameObject.SetActive(playerHealth.IsDefeated || (waves != null && waves.Victory));
                endTitle.text = playerHealth.IsDefeated ? "TRY AGAIN" : "QUEST COMPLETE";
                if (summaryDamage != GetComponent<PrototypeCombat>().Damage || wasDefeated != playerHealth.IsDefeated) {
                summaryDamage = GetComponent<PrototypeCombat>().Damage; wasDefeated = playerHealth.IsDefeated;
                summary.text = playerHealth.IsDefeated ? "Your answers and wand upgrades are safe.\nRetry the current room." : "Three rooms cleared | Three answers saved\nWand power " + GetComponent<PrototypeCombat>().Damage + " | Ready for a new adventure?"; }
                graceLabel.text = playerHealth.GraceActive && CanPlay ? "SHIELD | Safe for a moment" : "";
                pauseButton.gameObject.SetActive(Started && GameplayEnabled && !ManualPaused);
                muteLabel.text = PrototypeAudio.Muted ? "Sound: off" : "Sound: on";
                menuMuteLabel.text = muteLabel.text;
                if (!Started || ManualPaused) menu.SetAsLastSibling();
                restartButton.GetComponentInChildren<Text>().text = playerHealth.IsDefeated ? "Retry" : "New Run";
                if (waves != null && (lastWave != waves.WaveNumber || lastBreak != (waves.ExitUnlocked ? 1 : 0))) {
                lastWave = waves.WaveNumber; lastBreak = waves.ExitUnlocked ? 1 : 0;
                waveLabel.text = "Room " + waves.WaveNumber + " / " + waves.WaveCount
                    + (waves.ExitUnlocked ? "\nExit open: forward" : "\nExit locked"); }
            }
            if (!HasFocus || epoch != PrototypeBrowserInput.Epoch)
            {
                Suspend();
                waitingForRelease = true;
                requireFreshPointer = true;
                lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                epoch = PrototypeBrowserInput.Epoch;
            }
            rewardLabel.gameObject.SetActive(Time.unscaledTime < rewardUntil && Started && GameplayEnabled && !ManualPaused);
            if (lastSafeArea != Screen.safeArea) UpdateSafeArea();
            if (showedAttack == AttackActive) return;
            showedAttack = AttackActive;
            instruction.text = normalInstruction;
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
            playerHealth?.GrantGrace();
        }

        public static void PrepareRestart() { restartPending = true; returnToPlay = true; }

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
                new Vector2(0, -18), new Vector2(-24, 28));
            var panel = hud.gameObject.AddComponent<Image>();
            panel.sprite = CourtyardArt.Panel; panel.type = Image.Type.Sliced; panel.color = new Color(1,1,1,.70f);
            normalInstruction = TouchMode ? "MOSSLIGHT | Move + cast" : "MOSSLIGHT COURTYARD | Mouse to move | Click to cast";
            instruction = Label("Instructions Text", hud, normalInstruction, 14);
            circle = CreateCircle();
            var pad = Rect("Left Thumb Joystick", safeArea, Vector2.zero, Vector2.zero,
                new Vector2(104, 104), new Vector2(136, 136));
            var padImage = pad.gameObject.AddComponent<Image>();
            padImage.sprite = CourtyardArt.Touch;
            padImage.color = Color.white;
            var knob = Rect("Joystick Knob", pad, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(48, 48));
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = new Color(.68f,.82f,.68f,.75f);
            knobImage.raycastTarget = false;
            joystick = pad.gameObject.AddComponent<PrototypeJoystick>();
            joystick.Owner = this;
            joystick.Knob = knob;
            attackButton = Rect("Right Thumb Attack", safeArea, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-102, 104), new Vector2(118, 118));
            var attackImage = attackButton.gameObject.AddComponent<Image>();
            attackImage.sprite = CourtyardArt.Touch;
            attackImage.color = Color.white;
            Label("Attack Label", attackButton, "CAST", 20);
            attackButton.gameObject.AddComponent<PrototypeAttackButton>().Owner = this;
            pad.gameObject.SetActive(TouchMode);
            attackButton.gameObject.SetActive(TouchMode);
            CreateHealthAndDefeatUI();
        }

        private void CreateHealthAndDefeatUI()
        {
            var healthBar = Rect("Player Health", safeArea, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -62), new Vector2(212, 23));
            healthBar.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.07f, 0.95f);
            var fillRect = Rect("Player Health Fill", healthBar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            healthFill = fillRect.gameObject.AddComponent<Image>();
            healthFill.color = new Color(.32f,.68f,.52f);
            healthFill.raycastTarget = false;
            healthLabel = Label("Player Health Text", healthBar, "Health", 14);
            var waveBar = Rect("Wave Status", safeArea, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(95, -115), new Vector2(170, 44));
            var roomBack = waveBar.gameObject.AddComponent<Image>(); roomBack.sprite = CourtyardArt.Panel; roomBack.type = Image.Type.Sliced; roomBack.color = new Color(1,1,1,.88f); roomBack.raycastTarget = false;
            waveLabel = Label("Wave Text", waveBar, "Room 1 / 3", 16);

            defeatPanel = Rect("Defeat Screen", safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            defeatPanel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
            var title = Rect("Defeat Title", defeatPanel, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0, 95), new Vector2(400, 60));
            endTitle = Label("Defeat Text", title, "DEFEATED", 34);
            restartButton = Rect("Restart", defeatPanel, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0, -35), new Vector2(240, 64));
            restartButton.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.5f, 0.4f);
            Label("Restart Text", restartButton, "RESTART", 26);
            var restart = restartButton.gameObject.AddComponent<PrototypeRestartButton>();
            restart.Owner = this;
            restart.onClick.AddListener(() => playerHealth?.Restart());
            var info = Rect("Completion Summary", defeatPanel, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 30), new Vector2(420, 65));
            summary = Label("Summary", info, "", 18);
            summary.resizeTextForBestFit = true; summary.resizeTextMinSize = 12; summary.resizeTextMaxSize = 18;
            defeatPanel.gameObject.SetActive(false);
        }

        public void NotifyReward(int reward) { rewardLabel.text = "WAND UPGRADED  +" + reward + " power"; rewardUntil = Time.unscaledTime + 2.5f; }
        private void CreateMenu()
        {
            var reward = Rect("Upgrade Notice", safeArea, new Vector2(.15f,.65f), new Vector2(.85f,.65f), Vector2.zero, new Vector2(0,40));
            rewardLabel = Label("Wand Upgrade", reward, "", 24); rewardLabel.color = new Color(1,.88f,.42f);
            rewardLabel.resizeTextForBestFit = true; rewardLabel.resizeTextMinSize = 14; rewardLabel.resizeTextMaxSize = 24;
            var shield = Rect("Grace Status", safeArea, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(270, 18));
            graceLabel = Label("Shield", shield, "", 12);
            pauseButton = MenuButton("Pause", safeArea, new Vector2(0, 1), new Vector2(48, -61), new Vector2(80, 36), Pause);
            muteButton = MenuButton("Sound: on", safeArea, new Vector2(1, 1), new Vector2(-52, -61), new Vector2(90, 36), () => PrototypeAudio.ToggleMute());
            muteLabel = muteButton.GetComponentInChildren<Text>();
            menu = Rect("Adventure Menu", safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            menu.gameObject.AddComponent<Image>().color = new Color(.045f,.09f,.11f,1f);
            var accent = Rect("Sunwood Crest", menu, new Vector2(0.42f, 0.90f), new Vector2(0.58f, 0.94f), Vector2.zero, Vector2.zero);
            accent.gameObject.AddComponent<Image>().color = new Color(.95f,.75f,.3f);
            var heading = Rect("Menu Heading", menu, new Vector2(0.08f, 0.71f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            var title = Label("Garden Quest", heading, "MOSSLIGHT COURTYARD\nAn adventure in learning", 32);
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 20; title.resizeTextMaxSize = 32;
            var help = Rect("Menu Help", menu, new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
            var text = Label("Controls", help, (TouchMode ? "Left thumb: move\nRight thumb: tap Attack to cast" : "Move your mouse over the garden to walk\nClick to cast at the nearest creature") + "\nThree rooms. Submit your answer, then walk through the open exit.", 21);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 21;
            playButton = MenuButton("Play", menu, new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(230, 58), Begin);
            resumeButton = MenuButton("Resume", menu, new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(230, 58), Resume);
            menuRestartButton = MenuButton("Restart | New Run", menu, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(230, 48), () => playerHealth.RestartNewRun());
            resumeButton.gameObject.SetActive(false); menuRestartButton.gameObject.SetActive(false);
            menuSound = MenuButton("Sound: on", menu, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(180, 36), () => PrototypeAudio.ToggleMute());
            menuMuteLabel = menuSound.GetComponentInChildren<Text>();
            var id = Rect("Build Identifier", menu, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.09f), Vector2.zero, Vector2.zero);
            Label("Build", id, BuildId, 12);
        }

        private RectTransform MenuButton(string title, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(title, parent, anchor, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = CourtyardArt.Panel; image.type = Image.Type.Sliced; image.color = Color.white;
            Label(title + " Text", rect, title, 18);
            var button = rect.gameObject.AddComponent<PrototypeRestartButton>(); button.Owner = this; button.targetGraphic = image;
            button.onClick.AddListener(action);
            return rect;
        }
        private void Begin()
        {
            if (Started || !InputReady) return;
            Started = true; menu.gameObject.SetActive(false);
            Time.timeScale = menuScale > 0 ? menuScale : 1;
            ResumeAfterQuestion(); PrototypeAudio.Play(2);
        }
        private void Pause()
        {
            if (!CanPlay || questions != null && questions.Paused) return;
            menuScale = Time.timeScale; ManualPaused = true; SetGameplayEnabled(false); Time.timeScale = 0;
            menu.gameObject.SetActive(true); menu.SetAsLastSibling();
            playButton.gameObject.SetActive(false); resumeButton.gameObject.SetActive(true); menuRestartButton.gameObject.SetActive(true);
        }
        private void Resume()
        {
            if (!ManualPaused || !InputReady) return;
            ManualPaused = false; menu.gameObject.SetActive(false);
            Time.timeScale = menuScale > 0 ? menuScale : 1; ResumeAfterQuestion();
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

    // Ending screens retain their pause during focus recovery. The recovery
    // finger cannot click Retry/New Run even if the UI module runs before input.
    public sealed class PrototypeRestartButton : Button
    {
        public PrototypeControls Owner;
        private readonly Dictionary<int, int> presses = new Dictionary<int, int>();
        public override void OnPointerDown(PointerEventData data)
        {
            presses.Remove(data.pointerId);
            if (Owner.InputReady && data.button == PointerEventData.InputButton.Left)
                presses[data.pointerId] = PrototypeBrowserInput.Epoch;
            base.OnPointerDown(data);
        }
        public override void OnPointerClick(PointerEventData data)
        {
            var valid = presses.TryGetValue(data.pointerId, out var epoch) && epoch == PrototypeBrowserInput.Epoch;
            presses.Remove(data.pointerId);
            if (valid && Owner.InputReady && data.clickCount == 1) base.OnPointerClick(data);
        }
        public override void OnSubmit(BaseEventData data)
        {
            var keys = Keyboard.current;
            if (Owner.InputReady && keys != null && (keys.enterKey.wasPressedThisFrame
                || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame)) base.OnSubmit(data);
        }
        protected override void OnDisable() { presses.Clear(); base.OnDisable(); }
    }
}
