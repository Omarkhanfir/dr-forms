using UnityEngine;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    public sealed class PrototypeTarget : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;
        public int Health { get; private set; }
        private Text label;
        private Image fill;
        private Transform healthDisplay;
        private void Awake()
        {
            Health = maximumHealth;
            var root = new GameObject("Target Health", typeof(Canvas));
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.up * 1.6f;
            root.transform.localScale = Vector3.one * 0.018f;
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
        }
        public void TakeDamage(int damage)
        {
            if (Health <= 0 || damage <= 0) return;
            Health = Mathf.Max(0, Health - damage);
            RefreshHealth();
            if (Health == 0) { gameObject.SetActive(false); Destroy(gameObject); }
        }
        private void RefreshHealth()
        {
            label.text = "Target " + Health + " / " + maximumHealth;
            fill.rectTransform.anchorMax = new Vector2((float)Health / maximumHealth, 1);
        }
    }
}
