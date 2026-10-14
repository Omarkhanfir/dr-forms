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
        private RectTransform paper;
        private RectTransform viewport, footer, hint;
        private VerticalLayoutGroup contentLayout;
        private Image paperImage;
        private Shadow paperShadow, outerShadow;
        private Text help, submitLabel;
        private Sprite manuscriptSprite;
        private float openedAt;
        private const float OpeningSeconds = .28f;
        private static readonly Color Ink = new Color(.18f,.17f,.18f);
        private static readonly Color Burgundy = new Color(.45f,.10f,.18f);
        private static readonly Color AnswerGrey = new Color(.83f,.82f,.79f);
        private Text progress, prompt, questionStep;
        private PrototypeDeliberateButton submit;
        private string rewardFeedback = "";
        private readonly List<PrototypeDeliberateButton> options = new List<PrototypeDeliberateButton>();
        private readonly List<Text> labels = new List<Text>();
        private readonly List<Image> choiceMarks = new List<Image>();
        private float selectionAt;
        private float previousTimeScale, neutralAt = -1;
        private int epoch, generation;
        private bool ready, layoutDirty = true;
        private float lastLayoutWidth = -1;
        private float lastLayoutHeight = -1, lastPixelRatio = -1;
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern float PrototypeQuestions_CanvasPixelRatio();
#endif
        private static float CanvasPixelRatio {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return Mathf.Max(.1f, PrototypeQuestions_CanvasPixelRatio());
#else
                return 1;
#endif
            }
        }
        private int displayedAnswers = -1;
        private string displayedReward;
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
        [System.Serializable] public struct LayoutRect {public float x,y,width,height;}
        [System.Serializable] public struct LayoutMetrics {
            public LayoutRect paper, viewport, submit;
            public LayoutRect[] buttons;
            public float[] textRequiredHeight, textAreaHeight;
            public float pixelRatio, promptRequiredHeight, promptAreaHeight;
            public float bodyFontSize, answerFontSize;
        }
        public LayoutMetrics LayoutVerification {
            get {
                if(paper==null || labels.Count==0)return default;
                float ratio=CanvasPixelRatio, cssScale=controls.UIRoot.GetComponentInParent<Canvas>().scaleFactor/ratio;
                return new LayoutMetrics {
                    paper=CssRect(paper,ratio),viewport=CssRect(viewport,ratio),submit=CssRect((RectTransform)submit.transform,ratio),pixelRatio=ratio,
                    promptRequiredHeight=WrappedHeight(prompt,prompt.rectTransform.rect.width)*cssScale,promptAreaHeight=prompt.rectTransform.rect.height*cssScale,
                    bodyFontSize=prompt.fontSize*cssScale,answerFontSize=labels[0].fontSize*cssScale,
                    buttons=options.ConvertAll(b=>CssRect((RectTransform)b.transform,ratio)).ToArray(),
                    textRequiredHeight=labels.ConvertAll(t=>WrappedHeight(t,t.rectTransform.rect.width)*cssScale).ToArray(),
                    textAreaHeight=labels.ConvertAll(t=>t.rectTransform.rect.height*cssScale).ToArray()
                };
            }
        }
        private static LayoutRect CssRect(RectTransform rect,float ratio){
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return new LayoutRect {x=a.x/ratio,y=(Screen.height-b.y)/ratio,width=(b.x-a.x)/ratio,height=(b.y-a.y)/ratio};
        }
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
            // Development-only long-content fixture exercises the production scrolling UI.
            if (Debug.isDebugBuild && Application.absoluteURL.Contains("longQuestions=1"))
                foreach (var q in content) {
                    q.prompt += " Café, déjà vu, français, español: é è ê à ç ñ ü. Imagine you are guiding a friend through the laboratory. Take your time to consider every choice. You can scroll to read all the text, change your choice, and then use the separate Submit Answer button when you are ready. There is no hurry; the creatures and the game clock wait while this question is open.";
                    for (int i=0;i<q.options.Length;i++) q.options[i] += " - This is a longer example answer with extra explanation so that we can check wrapping, scrolling, and selection on a small phone. Read the whole choice before deciding; selecting it does not submit it. You may change your choice before using the fixed Submit Answer action.";
                }
            var root = controls.UIRoot;
            var status = PrototypeControls.Rect("Question Progress", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-95, -105), new Vector2(180, 24));
            var progressBack = status.gameObject.AddComponent<Image>(); progressBack.sprite = CourtyardArt.Panel; progressBack.type = Image.Type.Sliced; progressBack.color = new Color(1,1,1,.88f); progressBack.raycastTarget = false;
            progress = PrototypeControls.Label("Question Progress Text", status, "", 14);
            progress.resizeTextForBestFit = true; progress.resizeTextMinSize = 10; progress.resizeTextMaxSize = 14;
            panel = PrototypeControls.Rect("Question Screen", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(.035f,.07f,.075f,1);
            paper = PrototypeControls.Rect("Laboratory Manuscript", panel, new Vector2(.025f,.025f), new Vector2(.975f,.975f), Vector2.zero, Vector2.zero);
            var paperTexture = Resources.Load<Texture2D>("Courtyard/Manuscript");
            paperImage = paper.gameObject.AddComponent<Image>();
            paperImage.sprite = manuscriptSprite = Sprite.Create(paperTexture,new Rect(0,0,paperTexture.width,paperTexture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(36,36,36,36));
            paperImage.type = Image.Type.Sliced;
            paperShadow = paper.gameObject.AddComponent<Shadow>();
            outerShadow = paper.gameObject.AddComponent<Shadow>();
            var step = PrototypeControls.Rect("Fixed Question Progress", paper, new Vector2(.06f,.83f), new Vector2(.94f,.95f), Vector2.zero, Vector2.zero);
            questionStep = PrototypeControls.Label("Question Number", step, "", 28);
            questionStep.font = Resources.Load<Font>("Courtyard/ManuscriptHeading"); questionStep.color = Burgundy;
            questionStep.verticalOverflow = VerticalWrapMode.Overflow;
            viewport = PrototypeControls.Rect("Question Viewport", paper, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<Image>().color = new Color(1,1,1,.015f);
            viewport.gameObject.AddComponent<RectMask2D>();
            contentRect = PrototypeControls.Rect("Scrollable Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            contentRect.pivot = new Vector2(0.5f, 1);
            var layout = contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            contentRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = paper.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = contentRect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var title = PrototypeControls.Rect("Question Prompt", contentRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            prompt = PrototypeControls.Label("Prompt", title, "", 26);
            prompt.color = Ink; prompt.alignment = TextAnchor.MiddleCenter;
            prompt.font = Resources.Load<Font>("Courtyard/ManuscriptBody");
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Overflow;
            title.gameObject.AddComponent<LayoutElement>().minHeight = 54;
            // Text supplies preferred height through its own layout on the prompt container below.
            prompt.rectTransform.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var option = MakeButton("Answer " + i, contentRect, () => Select(index));
                option.gameObject.AddComponent<LayoutElement>().minHeight = 64;
                options.Add(option);
                labels.Add(PrototypeControls.Label("Answer Text", (RectTransform)option.transform, "", 24));
                labels[i].color = Ink; labels[i].alignment = TextAnchor.MiddleLeft;
                labels[i].font = prompt.font;
                labels[i].rectTransform.offsetMin = new Vector2(56,10); labels[i].rectTransform.offsetMax = new Vector2(-18,-10);
                var mark = PrototypeControls.Rect("Choice mark", option.transform, new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(28,0),new Vector2(22,22));
                var markImage = mark.gameObject.AddComponent<Image>(); markImage.color = AnswerGrey; markImage.raycastTarget=false;
                var markRim = mark.gameObject.AddComponent<Outline>(); markRim.effectDistance=new Vector2(2,-2); markRim.effectColor=Ink;
                choiceMarks.Add(markImage);
                // Original geometric check, independent of font glyph support.
                var shortStroke=PrototypeControls.Rect("Check short stroke",mark,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-4,-1),new Vector2(3,7));
                shortStroke.localRotation=Quaternion.Euler(0,0,45);shortStroke.gameObject.AddComponent<Image>().raycastTarget=false;
                var longStroke=PrototypeControls.Rect("Check long stroke",mark,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(2,2),new Vector2(3,14));
                longStroke.localRotation=Quaternion.Euler(0,0,-40);longStroke.gameObject.AddComponent<Image>().raycastTarget=false;
            }
            footer = PrototypeControls.Rect("Actions", paper, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);
            hint = PrototypeControls.Rect("Question Help", paper, new Vector2(0.06f, 0.185f), new Vector2(0.94f, 0.24f), Vector2.zero, Vector2.zero);
            help = PrototypeControls.Label("Help", hint, "Scroll to read. Choose, then submit.", 14); help.color = Ink;
            help.font = prompt.font;
            submit = MakeButton("Submit Answer", footer, Submit);
            submit.GetComponent<Image>().color = Burgundy;
            submitLabel = PrototypeControls.Label("Submit Text", (RectTransform)submit.transform, "Submit Answer", 24);
            submitLabel.font = prompt.font;
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
            var image = rect.gameObject.AddComponent<Image>(); image.color = AnswerGrey;
            image.sprite = CourtyardArt.AnswerPanel; image.type = Image.Type.Sliced;
            var rim = rect.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(.37f,.35f,.34f,.7f); rim.effectDistance = new Vector2(1,-1);
            var button = rect.gameObject.AddComponent<PrototypeDeliberateButton>();
            button.Owner = this; button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f,1.08f,1.08f); colors.pressedColor = new Color(.72f,.70f,.68f);
            colors.selectedColor = new Color(.96f,.96f,.96f); colors.disabledColor = new Color(.7f,.7f,.7f); colors.fadeDuration = .09f; button.colors = colors;
            var focus=PrototypeControls.Rect("Keyboard focus",rect,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-10,-10));
            var focusGraphic=focus.gameObject.AddComponent<Image>();focusGraphic.sprite=CourtyardArt.AnswerFocus;focusGraphic.type=Image.Type.Sliced;
            focusGraphic.color=name=="Submit Answer"?new Color(1,.93f,.76f):Burgundy;focusGraphic.raycastTarget=false;
            button.FocusFrame=focus.gameObject;focus.gameObject.SetActive(false);
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
            if (displayedAnswers != AnswerCount || displayedReward != rewardFeedback) {
                displayedAnswers = AnswerCount; displayedReward = rewardFeedback;
                progress.text = "Questions " + AnswerCount + " / " + content.Length + rewardFeedback; }
            if (!Paused) return;
            if (!controls.HasFocus || epoch != PrototypeBrowserInput.Epoch) { Guard(); return; }
            if (ready) return;
            if (PrototypeControls.AnyPointerPressed() || SubmitKeyHeld()) { neutralAt = -1; return; }
            if (neutralAt < 0) neutralAt = Time.unscaledTime;
            if (Time.unscaledTime - neutralAt < inputGuardSeconds || Time.unscaledTime-openedAt < OpeningSeconds) return;
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
            controls.SetGameplayEnabled(false); PrototypeEnemyBolt.Clear(); Guard();
            openedAt = Time.unscaledTime;
            questionStep.text = "Laboratory Notes\nQuestion " + wave + " / " + content.Length;
            prompt.text = content[AnswerCount].prompt;
            foreach (var option in options) { option.gameObject.SetActive(true); option.GetComponent<Image>().color = AnswerGrey; option.GetComponent<Outline>().effectColor = new Color(.37f,.35f,.34f,.7f); }
            for (var i = 0; i < options.Count; i++) labels[i].text = content[AnswerCount].options[i];
            foreach(var mark in choiceMarks){mark.color=AnswerGrey;foreach(Transform stroke in mark.transform)stroke.gameObject.SetActive(false);}
            submit.gameObject.SetActive(true); submit.interactable = false;
            contentRect.anchoredPosition = Vector2.zero;
            panel.gameObject.SetActive(true);
            layoutDirty = true; ResizeText();
        }

        private void LateUpdate() {
            if (!Paused) return;
            // Unscaled presentation only; the opaque screen continues to intercept attack gestures.
            var t = Mathf.Clamp01((Time.unscaledTime-openedAt)/OpeningSeconds); var ease = 1-Mathf.Pow(1-t,3);
            paper.localScale = new Vector3(1,Mathf.Lerp(.93f,1,ease),1);
            paper.anchoredPosition = new Vector2(0,Mathf.Lerp(-12,0,ease));
            if(selected>=0){var pulse=Mathf.Clamp01((Time.unscaledTime-selectionAt)/.16f);choiceMarks[selected].transform.localScale=Vector3.one*(1+.12f*Mathf.Sin(pulse*Mathf.PI));}
            ResizeText();
        }
        private void ResizeText()
        {
            var root = controls.UIRoot;
            var ratio = CanvasPixelRatio;
            if (!layoutDirty && Mathf.Abs(root.rect.width-lastLayoutWidth)<.5f
                && Mathf.Abs(root.rect.height-lastLayoutHeight)<.5f && Mathf.Abs(ratio-lastPixelRatio)<.01f) return;
            layoutDirty=false; lastLayoutWidth=root.rect.width; lastLayoutHeight=root.rect.height; lastPixelRatio=ratio;
            // Convert CSS pixels to canvas units, including the Web player's density cap.
            // This keeps text and touch targets readable without enlarging desktop rows.
            var unit = ratio / root.GetComponentInParent<Canvas>().scaleFactor;
            var available = root.rect.size / unit;
            bool phone = Mathf.Min(available.x,available.y)<600;
            // Enlarge the reading UI, independently of the gameplay canvas. Compact
            // landscape keeps usable space for the scroller and the fixed Submit.
            float readingScale=Mathf.Min(1.5f,Mathf.Clamp(available.x/260f,1.12f,1.5f));
            if(available.y<450)readingScale=Mathf.Min(readingScale,1.25f);
            float margin=phone?10:24, padding=phone?18:30, spacing=phone?10:12;
            float header=phone?42:50, gap=phone?10:12, helpHeight=22, buttonHeight=44*readingScale;
            prompt.fontSize=Mathf.Max(1,Mathf.RoundToInt(17*readingScale*unit));
            questionStep.fontSize=Mathf.Max(1,Mathf.RoundToInt(22*readingScale*unit));
            help.fontSize=Mathf.Max(1,Mathf.RoundToInt(12*readingScale*unit));
            submitLabel.fontSize=Mathf.Max(1,Mathf.RoundToInt(16*readingScale*unit));
            foreach(var label in labels)label.fontSize=Mathf.Max(1,Mathf.RoundToInt(16*readingScale*unit));
            float maxWidth=Mathf.Min(680,available.x-2*margin)*unit;
            float desired=NaturalWidth(prompt)+2*padding*unit;
            foreach(var label in labels)desired=Mathf.Max(desired,NaturalWidth(label)+(48+2*padding)*unit);
            float width=Mathf.Min(maxWidth,Mathf.Max(480*unit,desired));
            float contentWidth=width-2*padding*unit;
            header=Mathf.Max(header,WrappedHeight(questionStep,contentWidth)/unit+4);
            contentLayout.spacing=spacing*unit; contentLayout.padding=new RectOffset();
            var promptLayout=prompt.transform.parent.GetComponent<LayoutElement>();
            promptLayout.minHeight=0; promptLayout.preferredWidth=contentWidth;
            promptLayout.preferredHeight=WrappedHeight(prompt,contentWidth)+4*unit;
            float bodyHeight=promptLayout.preferredHeight+spacing*unit*options.Count;
            for(var i=0;i<labels.Count;i++){
                float optionWidth=Mathf.Min(contentWidth,Mathf.Max(210*unit,NaturalWidth(labels[i])+60*readingScale*unit));
                var item=options[i].GetComponent<LayoutElement>();
                item.minHeight=buttonHeight*unit; item.preferredWidth=optionWidth; item.flexibleWidth=0;
                item.preferredHeight=Mathf.Max(item.minHeight,WrappedHeight(labels[i],optionWidth-48*readingScale*unit)+16*readingScale*unit);
                bodyHeight+=item.preferredHeight;
                labels[i].rectTransform.offsetMin=new Vector2(36,8)*readingScale*unit;
                labels[i].rectTransform.offsetMax=new Vector2(-12,-8)*readingScale*unit;
                var mark=(RectTransform)choiceMarks[i].transform;
                mark.sizeDelta=Vector2.one*14*readingScale*unit; mark.anchoredPosition=new Vector2(19,0)*readingScale*unit;
                choiceMarks[i].GetComponent<Outline>().effectDistance=new Vector2(1,-1)*unit;
                float markUnit=14f/22*readingScale*unit;
                var shortStroke=(RectTransform)mark.GetChild(0);shortStroke.anchoredPosition=new Vector2(-4,-1)*markUnit;shortStroke.sizeDelta=new Vector2(3,7)*markUnit;
                var longStroke=(RectTransform)mark.GetChild(1);longStroke.anchoredPosition=new Vector2(2,2)*markUnit;longStroke.sizeDelta=new Vector2(3,14)*markUnit;
            }
            float footerHeight=(helpHeight+8+buttonHeight+gap)*unit;
            float chrome=(2*padding+header+gap)*unit+footerHeight;
            float height=Mathf.Min(Mathf.Min(740,available.y-2*margin)*unit,bodyHeight+chrome);
            paper.anchorMin=paper.anchorMax=Vector2.one*.5f;paper.sizeDelta=new Vector2(width,height);
            paperImage.pixelsPerUnitMultiplier=36/((phone?10:16)*unit);
            paperShadow.effectColor=new Color(0,0,0,phone?.22f:.28f);paperShadow.effectDistance=new Vector2(0,phone?-4:-8)*unit;
            outerShadow.enabled=!phone;outerShadow.effectColor=new Color(0,0,0,.12f);outerShadow.effectDistance=new Vector2(3,-12)*unit;
            var heading=(RectTransform)questionStep.transform.parent;
            TopRect(heading,padding*unit,header*unit,padding*unit);
            TopRect(viewport,(padding+header+gap)*unit,Mathf.Max(44*unit,height-chrome),padding*unit);
            footer.anchorMin=footer.anchorMax=new Vector2(.5f,0);footer.pivot=new Vector2(.5f,0);
            footer.anchoredPosition=new Vector2(0,padding)*unit;
            footer.sizeDelta=new Vector2(Mathf.Min(contentWidth,Mathf.Max(210*unit,NaturalWidth(submitLabel)+40*unit)),buttonHeight*unit);
            hint.anchorMin=new Vector2(0,0);hint.anchorMax=new Vector2(1,0);hint.pivot=new Vector2(.5f,0);
            hint.anchoredPosition=new Vector2(0,(padding+buttonHeight+8)*unit);hint.sizeDelta=new Vector2(-2*padding*unit,helpHeight*unit);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private static void TopRect(RectTransform rect,float top,float height,float inset){
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-top);rect.sizeDelta=new Vector2(-2*inset,height);
        }
        private static float NaturalWidth(Text text){
            var settings=text.GetGenerationSettings(Vector2.zero);settings.horizontalOverflow=HorizontalWrapMode.Overflow;
            return text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text,settings)/text.pixelsPerUnit;
        }
        private static float WrappedHeight(Text text,float width)=>text.cachedTextGeneratorForLayout.GetPreferredHeight(
            text.text,text.GetGenerationSettings(new Vector2(Mathf.Max(1,width),0)))/text.pixelsPerUnit;

        private void Select(int index)
        {
            if (state != ScreenState.Question || !InputReady) return;
            selected = index; selectionAt=Time.unscaledTime; layoutDirty = true;
            for (var i = 0; i < options.Count; i++)
            {
                labels[i].text = content[AnswerCount].options[i];
                choiceMarks[i].color=i==selected?Burgundy:AnswerGrey;choiceMarks[i].transform.localScale=Vector3.one;
                foreach(Transform stroke in choiceMarks[i].transform)stroke.gameObject.SetActive(i==selected);
                options[i].GetComponent<Image>().color = i == selected ? new Color(.89f,.82f,.80f) : AnswerGrey;
                options[i].GetComponent<Outline>().effectColor = i == selected ? Burgundy : new Color(.37f,.35f,.34f,.7f);
                options[i].GetComponent<Outline>().effectDistance = i == selected ? new Vector2(2,-2) : new Vector2(1,-1);
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
            controls.NotifyReward(reward);
            waves.CompleteAnsweredWave();
        }
        private void OnDestroy() { if (Paused) Time.timeScale = previousTimeScale; if(manuscriptSprite!=null)Destroy(manuscriptSprite); }
    }

    // Button clicks require a fresh down in this screen generation. Keyboard submits
    // require a fresh key edge; UI module repeat/held events cannot activate them.
    public sealed class PrototypeDeliberateButton : Button
    {
        public PrototypeQuestions Owner;
        public GameObject FocusFrame;
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
            if(FocusFrame!=null)FocusFrame.SetActive(true);
            Owner?.Reveal((RectTransform)transform);
        }
        public override void OnDeselect(BaseEventData data){base.OnDeselect(data);if(FocusFrame!=null)FocusFrame.SetActive(false);}
        protected override void OnDisable() { presses.Clear(); base.OnDisable(); }
    }
}
