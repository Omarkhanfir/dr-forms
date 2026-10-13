using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    // Content and reward policy stay independent of the wave configuration.
    public sealed class PrototypeQuestions : MonoBehaviour
    {
        [System.Serializable]
        private sealed class Question
        {
            public string prompt;
            public string[] options;
            public int correct;
            public Question(string text, string[] choices, int answer) { prompt = text; options = choices; correct = answer; }
        }
        [SerializeField] private Question[] content = {
            new Question("What is 2 + 2?", new[] { "3", "4", "5" }, 1),
            new Question("Which planet do we live on?", new[] { "Mars", "Earth", "Venus" }, 1),
            new Question("Which shape has three sides?", new[] { "Circle", "Square", "Triangle" }, 2)
        };
        [SerializeField, Min(0)] private float inputGuardSeconds = 0.5f;
        [SerializeField, Min(0)] private int damageUpgrade = 10;
        [SerializeField] private bool equalRewards = true;
        [SerializeField, Min(0)] private int incorrectDamageUpgrade = 5;
        [SerializeField] private bool showCorrectnessFeedback;
        private enum ScreenState { Closed, Question }
        private ScreenState state;
        private static readonly List<int> answers = new List<int>();
        private static int earnedDamage;
        private PrototypeControls controls;
        private PrototypeCombat combat;
        private PrototypeWaves waves;
        private RectTransform panel, contentRect;
        private Text progress, prompt;
        private PrototypeDeliberateButton submit;
        private string rewardFeedback = "";
        private readonly List<PrototypeDeliberateButton> options = new List<PrototypeDeliberateButton>();
        private readonly List<Text> labels = new List<Text>();
        private float previousTimeScale, neutralAt = -1;
        private int epoch, generation;
        private bool ready;
        private int selected = -1;
        public bool Paused => state != ScreenState.Closed;
        public int Selected => selected;
        public bool InputReady => ready && controls.HasFocus && epoch == PrototypeBrowserInput.Epoch;
        public int Generation => generation;
        public int AnswerCount => answers.Count;
        public int[] Answers => answers.ToArray();
        public float ActiveSeconds => 0; // Retained read-only snapshot compatibility; no timed trigger.
        public Vector2[] OptionCenters => options.ConvertAll(option => Center(option.transform)).ToArray();
        public Vector2 SubmitCenter => submit != null ? Center(submit.transform) : Vector2.zero;
        private static Vector2 Center(Transform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.position);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { answers.Clear(); earnedDamage = 0; }
        public static void NewAssessment() { ResetSession(); PrototypeWaves.NewRun(); }

        private void Start()
        {
            controls = GetComponent<PrototypeControls>();
            combat = GetComponent<PrototypeCombat>();
            waves = GetComponent<PrototypeWaves>();
            combat.AddDamage(earnedDamage);
            var root = controls.UIRoot;
            var status = PrototypeControls.Rect("Question Progress", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -123), new Vector2(360, 30));
            progress = PrototypeControls.Label("Question Progress Text", status, "", 20);
            panel = PrototypeControls.Rect("Question Screen", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 1);
            var viewport = PrototypeControls.Rect("Question Viewport", panel, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            contentRect = PrototypeControls.Rect("Scrollable Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            contentRect.pivot = new Vector2(0.5f, 1);
            var layout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14; layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            contentRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = contentRect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var title = PrototypeControls.Rect("Question Prompt", contentRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            prompt = PrototypeControls.Label("Prompt", title, "", 26);
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Overflow;
            title.gameObject.AddComponent<LayoutElement>().minHeight = 100;
            // Text supplies preferred height through its own layout on the prompt container below.
            prompt.rectTransform.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var option = MakeButton("Answer " + i, contentRect, () => Select(index));
                option.gameObject.AddComponent<LayoutElement>().minHeight = 72;
                options.Add(option);
                labels.Add(PrototypeControls.Label("Answer Text", (RectTransform)option.transform, "", 24));
            }
            var footer = PrototypeControls.Rect("Actions", panel, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero);
            var hint = PrototypeControls.Rect("Question Help", panel, new Vector2(0.05f, 0.205f), new Vector2(0.95f, 0.24f), Vector2.zero, Vector2.zero);
            PrototypeControls.Label("Help", hint, "Scroll for more. Choose an option, then Submit Answer.", 14);
            submit = MakeButton("Submit Answer", footer, Submit);
            PrototypeControls.Label("Submit Text", (RectTransform)submit.transform, "Submit Answer", 24);
            panel.gameObject.SetActive(false);
            if (content == null || content.Length != 3 || waves == null || waves.WaveCount != content.Length
                || System.Array.Exists(content, q => q == null || string.IsNullOrWhiteSpace(q.prompt) || q.options == null
                    || q.options.Length != options.Count || q.correct < 0 || q.correct >= q.options.Length
                    || System.Array.Exists(q.options, string.IsNullOrWhiteSpace)))
            {
                Debug.LogError("Prototype requires exactly three waves and one valid three-option question per wave.");
                controls.SetGameplayEnabled(false);
                prompt.text = "Invalid prototype configuration: require three waves and three questions with three options each.";
                foreach (var option in options) option.gameObject.SetActive(false);
                submit.gameObject.SetActive(false); panel.gameObject.SetActive(true);
                enabled = false;
            }
        }

        private PrototypeDeliberateButton MakeButton(string name, RectTransform parent, UnityEngine.Events.UnityAction action)
        {
            var rect = PrototypeControls.Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.1f, 0.45f, 0.4f);
            var button = rect.gameObject.AddComponent<PrototypeDeliberateButton>();
            button.Owner = this; button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, 0.9f, 0.4f); colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f); button.colors = colors;
            button.onClick.AddListener(action);
            return button;
        }

        private void Guard()
        {
            ready = false; neutralAt = -1; generation++; epoch = PrototypeBrowserInput.Epoch;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        internal void Reveal(RectTransform item)
        {
            if (item.parent != contentRect) return;
            Canvas.ForceUpdateCanvases();
            var bottom = -item.anchoredPosition.y + item.rect.height * item.pivot.y;
            var top = bottom - item.rect.height;
            var viewportHeight = ((RectTransform)contentRect.parent).rect.height;
            var offset = contentRect.anchoredPosition.y;
            if (top < offset) offset = top;
            else if (bottom > offset + viewportHeight) offset = bottom - viewportHeight;
            contentRect.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, Mathf.Max(0, contentRect.rect.height - viewportHeight)));
        }

        private void Update()
        {
            if (controls == null) return;
            progress.gameObject.SetActive(controls.Started && !controls.ManualPaused);
            progress.text = "Questions " + AnswerCount + " / " + content.Length + rewardFeedback;
            if (!Paused) return;
            if (!controls.HasFocus || epoch != PrototypeBrowserInput.Epoch) { Guard(); return; }
            if (ready) return;
            if (PrototypeControls.AnyPointerPressed() || SubmitKeyHeld()) { neutralAt = -1; return; }
            if (neutralAt < 0) neutralAt = Time.unscaledTime;
            if (Time.unscaledTime - neutralAt < inputGuardSeconds) return;
            ready = true;
            EventSystem.current?.SetSelectedGameObject(options[0].gameObject);
        }

        private static bool SubmitKeyHeld() => Keyboard.current != null &&
            (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed || Keyboard.current.spaceKey.isPressed);

        public void OpenForWave(int wave)
        {
            if (Paused || wave != AnswerCount + 1 || wave > content.Length || GetComponent<PrototypePlayerHealth>().IsDefeated) return;
            state = ScreenState.Question; selected = -1; rewardFeedback = "";
            previousTimeScale = Time.timeScale; Time.timeScale = 0;
            controls.SetGameplayEnabled(false); Guard();
            prompt.text = "Question " + wave + " / " + content.Length + "\n" + content[AnswerCount].prompt;
            foreach (var option in options) { option.gameObject.SetActive(true); option.GetComponent<Image>().color = new Color(0.1f, 0.45f, 0.4f); }
            for (var i = 0; i < options.Count; i++) labels[i].text = content[AnswerCount].options[i];
            submit.gameObject.SetActive(true); submit.interactable = false;
            contentRect.anchoredPosition = Vector2.zero;
            panel.gameObject.SetActive(true);
            ResizeText();
        }

        private void LateUpdate() { if (Paused) ResizeText(); }
        private void ResizeText()
        {
            // Recompute wrapped heights on rotation/resizing as well as when content changes.
            var width = Mathf.Max(80, contentRect.rect.width - 32);
            var settings = prompt.GetGenerationSettings(new Vector2(width, 0));
            prompt.transform.parent.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(100,
                prompt.cachedTextGeneratorForLayout.GetPreferredHeight(prompt.text, settings) / prompt.pixelsPerUnit + 24);
            for (var i = 0; i < labels.Count; i++)
                options[i].GetComponent<LayoutElement>().preferredHeight = Mathf.Max(72,
                    labels[i].cachedTextGeneratorForLayout.GetPreferredHeight(labels[i].text, labels[i].GetGenerationSettings(new Vector2(width, 0))) / labels[i].pixelsPerUnit + 24);
        }

        private void Select(int index)
        {
            if (state != ScreenState.Question || !InputReady) return;
            selected = index;
            for (var i = 0; i < options.Count; i++)
            {
                labels[i].text = (i == selected ? "[Selected] " : "") + content[AnswerCount].options[i];
                options[i].GetComponent<Image>().color = i == selected ? new Color(0.15f, 0.65f, 0.55f) : new Color(0.1f, 0.45f, 0.4f);
            }
            submit.interactable = true;
            // Invalidate any simultaneous second pointer/key gesture already in progress.
            generation++;
        }

        private void Submit()
        {
            if (state != ScreenState.Question || !InputReady || selected < 0 || AnswerCount != waves.WaveNumber - 1) return;
            var isCorrect = selected == content[AnswerCount].correct;
            var reward = Mathf.Max(0, equalRewards || isCorrect ? damageUpgrade : incorrectDamageUpgrade);
            state = ScreenState.Closed; // Close the transaction gate before applying anything.
            answers.Add(selected); earnedDamage += reward; combat.AddDamage(reward);
            rewardFeedback = " | Damage +" + reward
                + (showCorrectnessFeedback ? (isCorrect ? " (Correct)" : " (Incorrect)") : "");
            Guard(); panel.gameObject.SetActive(false);
            controls.Suspend(); Time.timeScale = previousTimeScale;
            PrototypeAudio.Play(2);
            waves.CompleteAnsweredWave();
        }
        private void OnDestroy() { if (Paused) Time.timeScale = previousTimeScale; }
    }

    // Button clicks require a fresh down in this screen generation. Keyboard submits
    // require a fresh key edge; UI module repeat/held events cannot activate them.
    public sealed class PrototypeDeliberateButton : Button
    {
        public PrototypeQuestions Owner;
        private readonly Dictionary<int, int> presses = new Dictionary<int, int>();
        public override void OnPointerDown(PointerEventData data)
        {
            presses.Remove(data.pointerId);
            if (Owner.InputReady && IsInteractable() && data.button == PointerEventData.InputButton.Left)
                presses[data.pointerId] = Owner.Generation;
            base.OnPointerDown(data);
        }
        public override void OnPointerClick(PointerEventData data)
        {
            var valid = presses.TryGetValue(data.pointerId, out var version) && version == Owner.Generation;
            presses.Remove(data.pointerId);
            if (valid && Owner.InputReady && data.clickCount == 1) base.OnPointerClick(data);
        }
        public override void OnSubmit(BaseEventData data)
        {
            var keys = Keyboard.current;
            if (Owner.InputReady && keys != null && (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame))
                base.OnSubmit(data);
        }
        public override void OnSelect(BaseEventData data)
        {
            base.OnSelect(data);
            Owner?.Reveal((RectTransform)transform);
        }
        protected override void OnDisable() { presses.Clear(); base.OnDisable(); }
    }
}
