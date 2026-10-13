using UnityEngine;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    public sealed class PrototypeTarget : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField] private string displayName = "Target";
        internal static readonly System.Collections.Generic.List<PrototypeTarget> Active = new System.Collections.Generic.List<PrototypeTarget>();
        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() => Active.Remove(this);
        public int Health { get; private set; }
        public void SetDisplayName(string value) { displayName = value; if (label != null) RefreshHealth(); }
        private Text label;
        private Image fill;
        private Transform healthDisplay;
        private Renderer body;
        private MaterialPropertyBlock visual;
        private Color normalColor;
        private float hitUntil;
        public bool HitFlashActive => Time.time < hitUntil;
        public float DisplayedHealthFraction { get; private set; } = 1;
        public int DamageApplications { get; private set; }
        private void Awake()
        {
            Health = maximumHealth;
            gameObject.AddComponent<PrototypeCharacterVisual>();
            body = GetComponent<Renderer>();
            normalColor = body.sharedMaterial.GetColor("_BaseColor");
            visual = new MaterialPropertyBlock();
            var root = new GameObject("Target Health", typeof(Canvas));
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * 1.6f;
            root.transform.localScale = Vector3.one * 0.011f;
            healthDisplay = root.transform;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 55);
            var background = new GameObject("Health Bar", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            var rect = background.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 18);
            rect.anchoredPosition = new Vector2(0, -18);
            background.GetComponent<Image>().color = new Color(0.12f, 0.03f, 0.03f);
            var foreground = new GameObject("Remaining Health", typeof(RectTransform), typeof(Image));
            foreground.transform.SetParent(background.transform, false);
            var fillRect = foreground.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill = foreground.GetComponent<Image>();
            fill.color = new Color(0.2f, 0.9f, 0.3f);
            var text = new GameObject("Health Text", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(root.transform, false);
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 32);
            label = text.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 23;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            RefreshHealth();
        }
        private void LateUpdate()
        {
            if (Camera.main != null) healthDisplay.rotation = Camera.main.transform.rotation;
            DisplayedHealthFraction = Mathf.MoveTowards(DisplayedHealthFraction,
                (float)Health / maximumHealth, Time.deltaTime * 1.5f);
            fill.rectTransform.anchorMax = new Vector2(DisplayedHealthFraction, 1);
            visual.SetColor("_BaseColor", HitFlashActive ? new Color(1, 0.95f, 0.65f) : normalColor);
            body.SetPropertyBlock(visual);
        }
        public void TakeDamage(int damage)
        {
            if (Health <= 0 || damage <= 0) return;
            PrototypePresentation.Damage(transform.position + new Vector3(0, 2.2f, .4f), damage);
            DamageApplications++;
            hitUntil = Time.time + 0.12f;
            Health = Mathf.Max(0, Health - damage);
            RefreshHealth();
            PrototypeAudio.Play(1);
            PrototypeCombatEffect.Spawn(transform.position, PrototypeCharacterVisual.Paint(8), new Color(1,.8f,.35f), .5f, .2f, "Wand Impact");
            for(int i=0;i<3;i++) {
                float angle=i*Mathf.PI*2/3;
                PrototypeCombatEffect.Spawn(transform.position+new Vector3(Mathf.Cos(angle)*.35f,.1f,Mathf.Sin(angle)*.35f),
                    PrototypeCharacterVisual.Paint(11),new Color(1,.94f,.6f),.13f,.22f,"Impact Spark");
            }
            if (Health == 0)
            {
                PrototypeCombatEffect.Spawn(transform.position, body.sharedMaterial,
                    new Color(1, 0.7f, 0.2f), 1.4f, 0.3f, "Target Defeat");
                healthDisplay.gameObject.SetActive(false);
                var controller = GetComponent<CharacterController>(); if (controller != null) controller.enabled = false;
                // The visual owns a short unscaled defeat animation, including question pauses.
            }
        }
        private void RefreshHealth()
        {
            label.text = displayName;

        }
    }
}
