using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// Full-screen page for one <see cref="ProjectData"/>: an image stage (cover → gallery → exploded view, arrows / swipe /
    /// arrow keys) and a scrolling text column. Landscape: images left, text right. Portrait: images on top, text below.
    /// Built in code on first use, so the scene needs no changes.
    /// </summary>
    public class ProjectViewer : MonoBehaviour
    {
        static readonly Color Ink = new(0.08f, 0.07f, 0.1f);
        static readonly Color Paper = new(0.99f, 0.97f, 0.93f, 1f);
        static readonly Color Muted = new(0.35f, 0.33f, 0.38f);
        static readonly Color Stage = new(0.92f, 0.9f, 0.86f);

        [SerializeField, Range(0.4f, 0.75f)] float landscapeImageWidth = 0.6f;
        [SerializeField, Range(0.3f, 0.7f)] float portraitImageHeight = 0.48f;
        [SerializeField] float animSpeed = 8f;

        static ProjectViewer instance;
        public static bool IsOpen => instance && instance.open;

        TMP_FontAsset font;
        LinkButton linkTemplate;
        CanvasGroup group;
        RectTransform card, media, info, content;
        VerticalLayoutGroup contentLayout;
        ScrollRect scroll;
        Image picture, accentBar;
        TMP_Text slideLabel, caption;
        Button prev, next;
        readonly List<GameObject> spawned = new();
        readonly List<(Sprite sprite, string label, string caption)> slides = new();
        Color accent;
        int index;
        bool open, portrait, laidOut;
        float shown;

        /// <summary>The shared viewer, created on top of <paramref name="canvas"/> the first time it's needed.</summary>
        public static ProjectViewer Get(Transform canvas, TMP_FontAsset font, LinkButton linkTemplate)
        {
            if (instance) return instance;
            var root = UIObj("Project Viewer", canvas);
            Stretch(root);
            instance = root.gameObject.AddComponent<ProjectViewer>();
            instance.font = font ? font : TMP_Settings.defaultFontAsset;
            instance.linkTemplate = linkTemplate;
            instance.Build();
            return instance;
        }

        public static void HideCurrent()
        {
            if (instance) instance.Hide();
        }

        public void Show(ProjectData project)
        {
            if (!project) return;
            accent = project.accent;
            accentBar.color = accent;
            slideLabel.color = accent;

            slides.Clear();
            if (project.cover) slides.Add((project.cover, "", ""));
            foreach (var g in project.gallery)
                if (g != null && g.image) slides.Add((g.image, "", g.caption));
            if (project.explodedView) slides.Add((project.explodedView, "EXPLODED VIEW", project.explodedCaption));
            index = 0;
            prev.gameObject.SetActive(slides.Count > 1);
            next.gameObject.SetActive(slides.Count > 1);
            ShowSlide();

            foreach (var go in spawned) Destroy(go);
            spawned.Clear();

            var meta = Join("  ·  ", project.category, project.year);
            if (meta.Length > 0) AddText(meta.ToUpperInvariant(), 24, FontStyles.Bold, accent);
            AddText(project.title, 56, FontStyles.Bold, Ink);
            if (!string.IsNullOrEmpty(project.tagline)) AddText(project.tagline, 30, FontStyles.Italic, Muted);

            var facts = new List<string>();
            AddFact(facts, "Role", project.role);
            AddFact(facts, "Duration", project.duration);
            AddFact(facts, "Context", project.context);
            AddFact(facts, "Tools", project.tools);
            foreach (var f in project.facts)
                if (f != null) AddFact(facts, f.label, f.value);
            if (facts.Count > 0)
            {
                Spacer(6);
                AddText(string.Join("\n", facts), 27, FontStyles.Normal, Ink).lineSpacing = 18f;
            }

            Section("The challenge", project.challenge);
            Section("The solution", project.solution);
            Section("Process", project.process);

            if (project.links.Count > 0 && linkTemplate)
            {
                Spacer(10);
                foreach (var link in project.links)
                {
                    var b = Instantiate(linkTemplate, content);
                    b.gameObject.SetActive(true);
                    b.Bind(link);
                    b.GetComponent<Image>().color = accent;
                    spawned.Add(b.gameObject);
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild(content);
            scroll.verticalNormalizedPosition = 1f;
            transform.SetAsLastSibling();
            open = true;
            group.blocksRaycasts = true;
        }

        public void Hide()
        {
            open = false;
            group.blocksRaycasts = false;
        }

        public void Step(int delta)
        {
            if (slides.Count < 2) return;
            index = (index + delta + slides.Count) % slides.Count;
            ShowSlide();
        }

        void ShowSlide()
        {
            bool any = slides.Count > 0;
            picture.gameObject.SetActive(any);
            if (!any)
            {
                slideLabel.text = "";
                caption.text = "";
                return;
            }
            var s = slides[index];
            picture.sprite = s.sprite;
            var counter = slides.Count > 1 ? $"{index + 1} / {slides.Count}" : "";
            slideLabel.text = Join("  ·  ", s.label, counter);
            caption.text = s.caption ?? "";
        }

        void Update()
        {
            if (open)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.leftArrowKey.wasPressedThisFrame) Step(-1);
                    if (kb.rightArrowKey.wasPressedThisFrame) Step(1);
                }
            }

            shown = Mathf.MoveTowards(shown, open ? 1f : 0f, Time.unscaledDeltaTime * animSpeed);
            float eased = 1f - Mathf.Pow(1f - shown, 3f);
            group.alpha = eased;
            card.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);

            if (!laidOut || portrait != Aspect.IsPortrait) Layout();
        }

        void Layout()
        {
            laidOut = true;
            portrait = Aspect.IsPortrait;
            if (portrait)
            {
                Stretch(card, 20, 20, 20, 20);
                SetAnchors(media, new Vector2(0, 1 - portraitImageHeight), Vector2.one);
                SetAnchors(info, Vector2.zero, new Vector2(1, 1 - portraitImageHeight));
                contentLayout.padding = new RectOffset(40, 40, 36, 48);
            }
            else
            {
                Stretch(card, 40, 40, 40, 40);
                SetAnchors(media, Vector2.zero, new Vector2(landscapeImageWidth, 1));
                SetAnchors(info, new Vector2(landscapeImageWidth, 0), Vector2.one);
                contentLayout.padding = new RectOffset(44, 100, 44, 48); // right: room for the close button
            }
            LayoutRebuilder.MarkLayoutForRebuild(content);
        }

        // ---------- Building ----------

        void Build()
        {
            var root = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.72f);

            card = UIObj("Card", root);
            card.gameObject.AddComponent<Image>().color = Paper;
            card.gameObject.AddComponent<Outline>().effectColor = Ink;

            // Image stage.
            media = UIObj("Media", card);
            media.gameObject.AddComponent<Image>().color = Stage;
            media.gameObject.AddComponent<SwipeArea>().Swiped += Step;

            picture = UIObj("Picture", media).gameObject.AddComponent<Image>();
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            Stretch(picture.rectTransform, 110, 110, 70, 80);

            slideLabel = Label("Slide", media, 24, FontStyles.Bold, Ink);
            Bar(slideLabel.rectTransform, top: true, 60, 28);
            caption = Label("Caption", media, 26, FontStyles.Italic, Muted);
            Bar(caption.rectTransform, top: false, 76, 28);
            caption.alignment = TextAlignmentOptions.Center;

            prev = RoundButton("Prev", media, "←", new Vector2(0, 0.5f), new Vector2(20, 0));
            prev.onClick.AddListener(() => Step(-1));
            next = RoundButton("Next", media, "→", new Vector2(1, 0.5f), new Vector2(-20, 0));
            next.onClick.AddListener(() => Step(1));

            // Text column.
            info = UIObj("Info", card);
            info.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // catch drags
            info.gameObject.AddComponent<RectMask2D>();
            content = UIObj("Content", info);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 14;
            contentLayout.childControlHeight = true; contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false; contentLayout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = info.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = info;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var bar = UIObj("Accent", card);
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1); bar.sizeDelta = new Vector2(0, 12); bar.anchoredPosition = Vector2.zero;
            accentBar = bar.gameObject.AddComponent<Image>();
            accentBar.raycastTarget = false;

            var close = UIObj("Close", card);
            close.anchorMin = close.anchorMax = close.pivot = Vector2.one;
            close.sizeDelta = new Vector2(80, 80);
            close.anchoredPosition = new Vector2(-12, -20);
            var closeImg = close.gameObject.AddComponent<Image>();
            closeImg.color = new Color(0, 0, 0, 0);
            var closeBtn = close.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(Hide);
            var x = Label("Text", close, 56, FontStyles.Bold, Ink);
            x.text = "×";
            x.alignment = TextAlignmentOptions.Center;
            Stretch(x.rectTransform);

            Layout();
        }

        Button RoundButton(string name, Transform parent, string glyph, Vector2 anchor, Vector2 offset)
        {
            var rt = UIObj(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = new Vector2(76, 76);
            rt.anchoredPosition = offset;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(Paper.r, Paper.g, Paper.b, 0.92f);
            rt.gameObject.AddComponent<Outline>().effectColor = Ink;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var t = Label("Text", rt, 40, FontStyles.Bold, Ink);
            t.text = glyph;
            t.alignment = TextAlignmentOptions.Center;
            Stretch(t.rectTransform);
            return b;
        }

        TMP_Text AddText(string value, float size, FontStyles style, Color color)
        {
            var t = Label("Text", content, size, style, color);
            t.text = value;
            spawned.Add(t.gameObject);
            return t;
        }

        void Section(string heading, string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return;
            Spacer(10);
            AddText(heading, 30, FontStyles.Bold, accent);
            AddText(body, 28, FontStyles.Normal, Ink).lineSpacing = 10f; // TMP line spacing is in em/100
        }

        void Spacer(float height)
        {
            var s = UIObj("Spacer", content);
            s.gameObject.AddComponent<LayoutElement>().minHeight = height;
            spawned.Add(s.gameObject);
        }

        TextMeshProUGUI Label(string name, Transform parent, float size, FontStyles style, Color color)
        {
            var t = UIObj(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        // ---------- Station card ----------

        /// <summary>A clickable project card (thumbnail + title + tagline) for the station panel.</summary>
        public static Button CreateCard(Transform parent, ProjectData project, TMP_FontAsset font, Action onClick)
        {
            var rt = UIObj("Project " + project.name, parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Stage;
            rt.gameObject.AddComponent<Outline>().effectColor = Ink;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());

            var strip = UIObj("Accent", rt);
            strip.anchorMin = Vector2.zero; strip.anchorMax = new Vector2(0, 1);
            strip.pivot = new Vector2(0, 0.5f); strip.sizeDelta = new Vector2(10, 0); strip.anchoredPosition = Vector2.zero;
            var stripImg = strip.gameObject.AddComponent<Image>();
            stripImg.color = project.accent;
            stripImg.raycastTarget = false;

            float textLeft = 30;
            if (project.cover)
            {
                var thumb = UIObj("Thumb", rt).gameObject.AddComponent<Image>();
                thumb.sprite = project.cover;
                thumb.preserveAspect = true;
                thumb.raycastTarget = false;
                var tr = thumb.rectTransform;
                tr.anchorMin = Vector2.zero; tr.anchorMax = new Vector2(0, 1); tr.pivot = new Vector2(0, 0.5f);
                tr.sizeDelta = new Vector2(130, -20); tr.anchoredPosition = new Vector2(22, 0);
                textLeft = 170;
            }

            var title = CardText(rt, font, project.title + "  →", 34, FontStyles.Bold, Ink);
            Stretch(title.rectTransform, textLeft, 20, 18, 75);
            title.alignment = TextAlignmentOptions.BottomLeft;
            var tagline = CardText(rt, font, project.tagline, 24, FontStyles.Italic, Muted);
            Stretch(tagline.rectTransform, textLeft, 20, 80, 12);
            tagline.alignment = TextAlignmentOptions.TopLeft;
            tagline.overflowMode = TextOverflowModes.Ellipsis;
            return b;
        }

        static TMP_Text CardText(Transform parent, TMP_FontAsset font, string value, float size, FontStyles style, Color color)
        {
            var t = UIObj("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.text = value ?? "";
            return t;
        }

        // ---------- Helpers ----------

        static void AddFact(List<string> lines, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add($"<b>{label}</b>   {value}");
        }

        static string Join(string sep, params string[] parts)
        {
            var kept = new List<string>();
            foreach (var p in parts)
                if (!string.IsNullOrWhiteSpace(p)) kept.Add(p);
            return string.Join(sep, kept);
        }

        static RectTransform UIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>A full-width text bar pinned to the top or bottom edge of its parent.</summary>
        static void Bar(RectTransform rt, bool top, float height, float inset)
        {
            float y = top ? 1 : 0;
            rt.anchorMin = new Vector2(0, y); rt.anchorMax = new Vector2(1, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(-2 * inset, height);
            rt.anchoredPosition = new Vector2(0, top ? -14 : 8);
        }
    }
}
