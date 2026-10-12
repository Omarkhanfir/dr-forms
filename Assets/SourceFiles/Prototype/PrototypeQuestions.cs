using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    // Run before combat so the interval boundary cannot admit another attack or hit.
    [DefaultExecutionOrder(-100)]
    public sealed class PrototypeQuestions : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float intervalSeconds = 10;
        [SerializeField, Min(0)] private int damageUpgrade = 10;
        [SerializeField] private bool equalRewards = true;
        [SerializeField, Min(0)] private int incorrectDamageUpgrade = 5;
        private static readonly List<int> answers = new List<int>();
        private static int earnedDamage;
        private static float activeSeconds;
        private PrototypeControls controls;
        private PrototypeCombat combat;
        private RectTransform panel;
        private Text progress, prompt;
        private readonly List<Text> optionLabels = new List<Text>();
        private readonly List<RectTransform> options = new List<RectTransform>();
        private float previousTimeScale;
        public bool Paused { get; private set; }
        public int AnswerCount => answers.Count;
        public int[] Answers => answers.ToArray();
        public float ActiveSeconds => activeSeconds;
        public Vector2[] OptionCenters => options.ConvertAll(option => RectTransformUtility.WorldToScreenPoint(null, option.position)).ToArray();
        private static readonly string[] prompts = { "What is 2 + 2?", "Which planet do we live on?", "Which shape has three sides?" };
        private static readonly string[][] choices = { new[] { "3", "4", "5" }, new[] { "Mars", "Earth", "Venus" }, new[] { "Circle", "Square", "Triangle" } };
        private static readonly int[] correct = { 1, 1, 2 };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { answers.Clear(); earnedDamage = 0; activeSeconds = 0; }
        public static void NewAssessment() => ResetSession();

        private void Start()
        {
            controls = GetComponent<PrototypeControls>();
            combat = GetComponent<PrototypeCombat>();
            combat.AddDamage(earnedDamage);
            var root = controls.UIRoot;
            var status = PrototypeControls.Rect("Question Progress", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(360, 30));
            progress = PrototypeControls.Label("Question Progress Text", status, "", 20);
            panel = PrototypeControls.Rect("Question Screen", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 1);
            var title = PrototypeControls.Rect("Question Prompt", panel, new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            prompt = PrototypeControls.Label("Prompt", title, "", 26);
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var option = PrototypeControls.Rect("Answer " + i, panel, new Vector2(0.08f, 0.48f - i * 0.14f), new Vector2(0.92f, 0.59f - i * 0.14f), Vector2.zero, Vector2.zero);
                option.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.45f, 0.4f);
                optionLabels.Add(PrototypeControls.Label("Answer Text", option, "", 24));
                option.gameObject.AddComponent<Button>().onClick.AddListener(() => Submit(index));
                options.Add(option);
            }
            panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (controls == null) return;
            progress.text = "Questions " + AnswerCount + " / 3" + (AnswerCount == 3 ? " - Complete" : "");
            if (Paused || AnswerCount >= 3 || !controls.CanPlay) return;
            activeSeconds += Time.deltaTime;
            if (activeSeconds < Mathf.Max(0.1f, intervalSeconds)) return;
            Paused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            controls.SetGameplayEnabled(false);
            prompt.text = "Question " + (AnswerCount + 1) + " / 3\n" + prompts[AnswerCount];
            for (var i = 0; i < 3; i++) optionLabels[i].text = choices[AnswerCount][i];
            panel.gameObject.SetActive(true);
        }

        private void Submit(int answer)
        {
            if (!Paused || AnswerCount >= 3 || answer < 0 || answer >= 3) return;
            var reward = equalRewards || answer == correct[AnswerCount] ? damageUpgrade : incorrectDamageUpgrade;
            answers.Add(answer);
            earnedDamage += Mathf.Max(0, reward);
            combat.AddDamage(reward);
            activeSeconds = 0;
            Paused = false; // Close the submission gate before restoring gameplay.
            panel.gameObject.SetActive(false);
            controls.ResumeAfterQuestion();
            Time.timeScale = previousTimeScale;
        }

        private void OnDestroy() { if (Paused) Time.timeScale = previousTimeScale; }
    }
}
