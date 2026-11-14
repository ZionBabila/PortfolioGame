using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// The content card for a station. Docks to the right side in landscape (16:9)
    /// and becomes a bottom sheet in portrait (9:16).
    /// </summary>
    public class StationPanel : MonoBehaviour
    {
        [SerializeField] RectTransform sheet;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text kindLabel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text body;
        [SerializeField] Image accentBar;
        [SerializeField] Image picture;
        [SerializeField] RectTransform linksRoot;
        [SerializeField] LinkButton linkTemplate;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scroll;
        [Tooltip("Workshop Letters art. Empty = Resources/UISkin (built by Portfolio → UI → Import Workshop PSD); none = flat look.")]
        [SerializeField] UISkin skin;

        [Header("Layout")]
        [SerializeField, Range(0.2f, 0.6f)] float landscapeWidth = 0.4f;
        [SerializeField, Range(0.3f, 0.9f)] float portraitHeight = 0.58f;
        [SerializeField] float margin = 24f;
        [SerializeField] float animSpeed = 10f;

        readonly List<LinkButton> spawned = new();
        readonly List<GameObject> projectCards = new();
        bool open;
        float shown;
        StrokeDraw logoDraw, signatureDraw;

        public bool IsOpen => open;

        /// <summary>The station whose letter is open (null when closed). QuickNav lights its icon from this.</summary>
        public static StationData Current { get; private set; }
        public static event System.Action CurrentChanged;

        // The project enters Play mode without a domain reload, so statics must be reset by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            CurrentChanged = null;
        }
        bool Skinned => skin && skin.IsReady;

        void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (linkTemplate) linkTemplate.gameObject.SetActive(false);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (!skin) skin = UISkin.Default;
            if (Skinned) ApplySkin();
        }

        /// <summary>Dresses the scene-built sheet in the PSD art: paper, letterhead loop, close X, signature.</summary>
        void ApplySkin()
        {
            if (sheet.TryGetComponent<Image>(out var paper))
            {
                paper.sprite = skin.Sliced("BG_Paper", 48);
                paper.type = Image.Type.Sliced;
                paper.color = Color.white;
            }
            if (sheet.TryGetComponent<Outline>(out var outline)) outline.enabled = false;
            var drop = sheet.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(skin.ink.r, skin.ink.g, skin.ink.b, 0.22f);
            drop.effectDistance = new Vector2(5, -8);
            if (accentBar) accentBar.gameObject.SetActive(false);

            // Typed-letter voice; the small kind label is the letterhead.
            skin.Type(title, FontStyles.Bold);
            skin.Type(subtitle, FontStyles.Italic);
            skin.Type(body);
            if (body) body.fontSize = Mathf.Min(body.fontSize, 29f); // Courier runs wider than Roboto
            if (title) title.fontSize = Mathf.Min(title.fontSize, 54f);
            skin.Label(kindLabel, 10f);
            if (subtitle) subtitle.color = skin.inkSoft;
            if (title) title.color = skin.ink;
            if (body) body.color = skin.ink;

            if (closeButton && closeButton.TryGetComponent<Image>(out var closeImg))
            {
                foreach (var t in closeButton.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false);
                foreach (var t in closeButton.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false);
                closeImg.color = Color.white;
                closeImg.preserveAspect = true;
                closeButton.transition = Selectable.Transition.None;
                SpriteStates.On(closeImg, skin.Get("Btn_Close_Normal"), skin.Get("Btn_Close_Hover"));
                var cr = (RectTransform)closeButton.transform;
                cr.sizeDelta = new Vector2(44, 44);
                HitArea.Grow(closeButton, 140f); // a finger-sized target around the small X
                cr.anchoredPosition = new Vector2(-26, -30);
            }

            var logo = Decoration("Logo_Stroke", new Vector2(1, 1), new Vector2(-88, -26), Color.white);
            logo.rectTransform.sizeDelta *= 1.6f;
            logoDraw = StrokeDraw.On(logo, 0.9f);
            var sign = Decoration("Signature_Stroke", new Vector2(0, 0), new Vector2(40, 22), Color.white);
            sign.rectTransform.sizeDelta *= 1.3f;
            signatureDraw = StrokeDraw.On(sign, 1.6f);
        }

        Image Decoration(string layer, Vector2 corner, Vector2 offset, Color color)
        {
            var go = new GameObject(layer, typeof(RectTransform), typeof(Image));
            go.layer = sheet.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(sheet, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.sprite = skin.Get(layer);
            img.color = color;
            img.raycastTarget = false;
            img.SetNativeSize();
            return img;
        }

        public void Show(StationData data)
        {
            if (!data) return;
            // Null-safe: a text field left empty (e.g. before converting the scene to TextMeshPro) just stays blank.
            SetText(kindLabel, data.kind.ToString().ToUpperInvariant());
            SetText(title, data.title);
            SetText(subtitle, data.SubtitleNow);
            if (subtitle) subtitle.gameObject.SetActive(!string.IsNullOrEmpty(data.SubtitleNow));
            SetText(body, data.BodyNow);
            if (accentBar) accentBar.color = data.accent;
            if (kindLabel) kindLabel.color = data.accent;

            picture.sprite = data.image;
            picture.gameObject.SetActive(data.image);
            if (data.image) picture.preserveAspect = true;

            foreach (var b in spawned) Destroy(b.gameObject);
            spawned.Clear();
            foreach (var c in projectCards) Destroy(c);
            projectCards.Clear();
            var font = title ? title.font : null;
            if (Skinned)
                BuildSkinnedLinks(data, font);
            else
            {
                for (int i = 0; i < data.projects.Count; i++)
                {
                    var project = data.projects[i];
                    if (!project) continue;
                    var card = ProjectViewer.CreateCard(linksRoot, project, font, () => OpenProject(project, font));
                    card.transform.SetSiblingIndex(projectCards.Count); // cards first, links after
                    projectCards.Add(card.gameObject);
                }
                foreach (var link in data.LinksNow)
                {
                    var b = Instantiate(linkTemplate, linksRoot);
                    b.gameObject.SetActive(true);
                    b.Bind(link);
                    b.GetComponent<Image>().color = data.accent;
                    spawned.Add(b);
                }
            }
            if (logoDraw) logoDraw.Play(0.15f);
            if (signatureDraw) signatureDraw.Play(0.6f);

            if (scroll) scroll.verticalNormalizedPosition = 1f;
            open = true;
            group.blocksRaycasts = true;
            Current = data;
            CurrentChanged?.Invoke();
        }

        /// <summary>Games and projects become tickets in a wrapping grid; every other link becomes a rubber stamp.</summary>
        void BuildSkinnedLinks(StationData data, TMP_FontAsset font)
        {
            bool gameLinks = data.kind == StationKind.Game;
            int ticketCount = 0;
            RectTransform deck = null;

            RectTransform Deck()
            {
                if (deck) return deck;
                var go = new GameObject("Tickets", typeof(RectTransform));
                go.layer = linksRoot.gameObject.layer;
                deck = (RectTransform)go.transform;
                deck.SetParent(linksRoot, false);
                deck.SetAsFirstSibling(); // tickets first, stamps after
                var grid = go.AddComponent<GridLayoutGroup>();
                grid.cellSize = TicketCard.Size;
                grid.spacing = new Vector2(18, 22);
                grid.padding = new RectOffset(4, 4, 8, 12);
                grid.childAlignment = TextAnchor.UpperLeft;
                projectCards.Add(go);
                return deck;
            }

            foreach (var project in data.projects)
            {
                if (!project) continue;
                var p = project;
                TicketCard.ForProject(Deck(), skin, p, ticketCount++, font, () => OpenProject(p, font));
            }

            int stampCount = 0;
            foreach (var link in data.LinksNow)
            {
                if (gameLinks)
                {
                    TicketCard.ForLink(Deck(), skin, link, ticketCount++, font);
                    continue;
                }
                var b = Instantiate(linkTemplate, linksRoot);
                b.gameObject.SetActive(true);
                b.Bind(link);
                StampButton.Apply(b.gameObject, skin, stampCount++);
                spawned.Add(b);
            }
        }

        void OpenProject(ProjectData project, TMP_FontAsset font) =>
            ProjectViewer.Get(GetComponentInParent<Canvas>().rootCanvas.transform, font, linkTemplate).Show(project);

        static void SetText(TMP_Text t, string value)
        {
            if (t) t.text = value;
        }

        public void Hide()
        {
            ProjectViewer.HideCurrent();
            open = false;
            group.blocksRaycasts = false;
            if (Current)
            {
                Current = null;
                CurrentChanged?.Invoke();
            }
        }

        void Update()
        {
            if (Skinned)
            {
                // Motion table: open = up 40 px + 2°→0 with overshoot (~0.45 s); close = lift 24 px and fade (~0.25 s).
                shown = Mathf.MoveTowards(shown, open ? 1f : 0f, Time.unscaledDeltaTime * (open ? 2.2f : 4f));
                group.alpha = UIEase.OutCubic(shown);
                Layout(1f);
                if (open)
                {
                    float back = UIEase.OutBack(shown);
                    sheet.anchoredPosition += new Vector2(0f, -40f * (1f - back));
                    sheet.localRotation = Quaternion.Euler(0, 0, 2f * (1f - back));
                }
                else
                {
                    sheet.anchoredPosition += new Vector2(0f, 24f * (1f - shown));
                    sheet.localRotation = Quaternion.identity;
                }
                return;
            }

            shown = Mathf.MoveTowards(shown, open ? 1f : 0f, Time.unscaledDeltaTime * animSpeed);
            float eased = 1f - Mathf.Pow(1f - shown, 3f);
            group.alpha = eased;
            Layout(eased);
        }

        void Layout(float eased)
        {
            if (Aspect.IsPortrait)
            {
                sheet.anchorMin = new Vector2(0f, 0f);
                sheet.anchorMax = new Vector2(1f, portraitHeight);
                sheet.offsetMin = new Vector2(margin * 0.5f, margin * 0.5f);
                sheet.offsetMax = new Vector2(-margin * 0.5f, 0f);
                sheet.anchoredPosition += new Vector2(0f, -(1f - eased) * 200f);
            }
            else
            {
                sheet.anchorMin = new Vector2(1f - landscapeWidth, 0f);
                sheet.anchorMax = new Vector2(1f, 1f);
                sheet.offsetMin = new Vector2(0f, margin);
                sheet.offsetMax = new Vector2(-margin, -margin);
                sheet.anchoredPosition += new Vector2((1f - eased) * 200f, 0f);
            }
        }
    }
}
