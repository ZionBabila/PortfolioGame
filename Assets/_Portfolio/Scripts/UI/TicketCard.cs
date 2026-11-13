using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// An admit-one ticket for a game or project, built from the PSD's Ticket layers (motion table: Ticket).
    /// Dealt in with a stagger and its own tilt; hover straightens, lifts, redraws the art in accent and grows the
    /// PLAY stamp; click presses the stamp and tears the ticket away up-right.
    /// </summary>
    public class TicketCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
    {
        public static readonly Vector2 Size = new(150, 170); // BG_Card is 300×340 at 2×

        RectTransform body;
        CanvasGroup bodyGroup;
        Image shadow, art, play;
        StrokeDraw artDraw;
        Sprite playNormal, playPressed;
        Color ink, accent;
        string url;
        Action onClick;

        float restTilt, tilt, lift, playScale = 1f, shadowGrow;
        float dealDelay, dealP, tearP = -1f;
        bool over;

        // ---------- Building ----------

        /// <summary>A ticket that opens <paramref name="link"/> (a playable game).</summary>
        public static TicketCard ForLink(Transform parent, UISkin skin, StationLink link, int index, TMP_FontAsset font)
        {
            var t = Create(parent, skin, link.label, "PLAY IN BROWSER", ArtFor(link.label), TintFor(link.label), index, font);
            t.url = link.url;
            return t;
        }

        /// <summary>A ticket that runs <paramref name="onClick"/> after it tears away (a project page).</summary>
        public static TicketCard ForProject(Transform parent, UISkin skin, ProjectData project, int index, TMP_FontAsset font, Action onClick)
        {
            var meta = string.IsNullOrEmpty(project.category) ? "PROJECT" : project.category.ToUpperInvariant();
            var t = Create(parent, skin, project.title, meta, "Art_Project_Stroke", new Color32(227, 232, 242, 255), index, font);
            if (project.cover)
            {
                t.art.sprite = project.cover; // a real cover beats the generic line drawing
                t.art.color = Color.white;
                t.artDraw.enabled = false;
            }
            t.onClick = onClick;
            return t;
        }

        static TicketCard Create(Transform parent, UISkin skin, string title, string meta, string artLayer, Color tint, int index, TMP_FontAsset font)
        {
            var root = UIObj("Ticket " + title, parent);
            root.sizeDelta = Size;
            var hit = root.gameObject.AddComponent<Image>(); // fixed hit area so the lift doesn't make hover flicker
            hit.color = Color.clear;

            var t = root.gameObject.AddComponent<TicketCard>();
            t.ink = skin.ink;
            t.accent = skin.accent;

            t.shadow = Img("Shadow", root, skin.Get("Ticket_Shadow"));
            Stretch(t.shadow.rectTransform, -8, -8, -2, -16);

            t.body = UIObj("Body", root);
            Stretch(t.body);
            t.bodyGroup = t.body.gameObject.AddComponent<CanvasGroup>();

            Stretch(Img("BG_Card", t.body, skin.Get("BG_Card")).rectTransform);

            var area = Img("Art_Area", t.body, skin.Get("Art_Area")).rectTransform;
            area.GetComponent<Image>().color = tint;
            area.anchorMin = new Vector2(0, 1 - 85f / 170f); area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            t.art = Img("Art", area, skin.Get(artLayer));
            t.art.color = t.ink;
            t.art.preserveAspect = true;
            Stretch(t.art.rectTransform, 30, 30, 12, 14);
            t.artDraw = StrokeDraw.On(t.art, 1.1f);

            var titleText = Text("Title", t.body, font, title, 17, FontStyles.Bold, skin.ink);
            Stretch(titleText.rectTransform, 12, 10, 88, 58);
            titleText.alignment = TextAlignmentOptions.BottomLeft;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            var metaText = Text("Meta", t.body, font, meta, 10, FontStyles.Normal, skin.inkSoft);
            Stretch(metaText.rectTransform, 12, 10, 114, 42);
            metaText.alignment = TextAlignmentOptions.TopLeft;
            metaText.characterSpacing = 8;

            var perf = Img("Perforation", t.body, skin.Get("Perforation")).rectTransform;
            perf.anchorMin = perf.anchorMax = new Vector2(0.5f, 0);
            perf.anchoredPosition = new Vector2(0, 38);
            perf.GetComponent<Image>().SetNativeSize();

            var admit = Text("Admit", t.body, font, "ADMIT ONE", 9, FontStyles.Bold, skin.inkSoft);
            admit.rectTransform.anchorMin = Vector2.zero; admit.rectTransform.anchorMax = new Vector2(0.5f, 0);
            admit.rectTransform.pivot = new Vector2(0, 0);
            admit.rectTransform.sizeDelta = new Vector2(0, 30);
            admit.rectTransform.anchoredPosition = new Vector2(12, 4);
            admit.alignment = TextAlignmentOptions.MidlineLeft;
            admit.characterSpacing = 6;

            t.playNormal = skin.Get("Ticket_Play_Normal");
            t.playPressed = skin.Get("Ticket_Play_Pressed");
            t.play = Img("Btn_Play", t.body, t.playNormal);
            var pr = t.play.rectTransform;
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1, 0);
            t.play.SetNativeSize();
            pr.anchoredPosition = new Vector2(-10, 5);

            // Each ticket gets its own small, repeatable tilt and a staggered deal-in.
            t.restTilt = ((index * 7 + 3) % 5 - 2) * 1.1f;
            t.tilt = t.restTilt;
            t.dealDelay = index * 0.09f;
            t.dealP = 0f;
            t.artDraw.Play(t.dealDelay + 0.25f);
            t.Apply();
            return t;
        }

        static string ArtFor(string label)
        {
            var k = Key(label);
            if (k.Contains("shellbound")) return "Art_ShellBound_Stroke";
            if (k.Contains("maskadu")) return "Art_Maskadu_Stroke";
            if (k.Contains("whereismymind")) return "Art_WhereIsMyMind_Stroke";
            if (k.Contains("chatbaker")) return "Art_ChatBaker_Stroke";
            if (k.Contains("kaa")) return "Art_KaasJourney_Stroke";
            return "Art_Project_Stroke";
        }

        static Color TintFor(string label)
        {
            var k = Key(label);
            if (k.Contains("shellbound")) return new Color32(223, 233, 230, 255);
            if (k.Contains("maskadu")) return new Color32(241, 224, 207, 255);
            if (k.Contains("whereismymind")) return new Color32(230, 226, 239, 255);
            if (k.Contains("chatbaker")) return new Color32(243, 230, 201, 255);
            if (k.Contains("kaa")) return new Color32(223, 232, 213, 255);
            return new Color32(227, 232, 242, 255);
        }

        static string Key(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in s ?? "")
                if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        // ---------- Interaction ----------

        public void OnPointerEnter(PointerEventData e)
        {
            if (tearP >= 0f) return;
            over = true;
            if (artDraw.enabled) { art.color = accent; artDraw.Play(); }
        }

        public void OnPointerExit(PointerEventData e)
        {
            over = false;
            if (artDraw.enabled) art.color = ink;
            if (tearP < 0f) play.sprite = playNormal;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (tearP >= 0f) return;
            play.sprite = playPressed ? playPressed : playNormal;
            // WebGL popup blockers only allow opening a tab from inside the browser's own click handler.
            if (WebLinks.DeferToPointerUp && !string.IsNullOrEmpty(url)) WebLinks.ArmForPointerUp(url);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (tearP >= 0f || e.dragging) { play.sprite = playNormal; return; }
            if (!WebLinks.DeferToPointerUp && !string.IsNullOrEmpty(url)) WebLinks.OpenNow(url);
            tearP = 0f;
            over = false;
        }

        // ---------- Motion ----------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (dealDelay > 0f) dealDelay -= dt;
            else if (dealP < 1f) dealP = Mathf.Min(1f, dealP + dt / 0.45f);

            if (tearP >= 0f)
            {
                tearP += dt / 0.35f;
                if (tearP >= 1f)
                {
                    tearP = -1f;
                    onClick?.Invoke();
                    // The panel stays open (links open in a new tab), so deal the ticket back in.
                    play.sprite = playNormal;
                    dealP = 0f;
                    dealDelay = 0.4f;
                }
            }

            tilt = UIEase.Damp(tilt, over ? 0f : restTilt, 14f);
            lift = UIEase.Damp(lift, over ? 6f : 0f, 16f);
            playScale = UIEase.Damp(playScale, over ? 1.12f : 1f, 16f);
            shadowGrow = UIEase.Damp(shadowGrow, over ? 1f : 0f, 12f);
            Apply();
        }

        void Apply()
        {
            float back = UIEase.OutBack(dealP);
            float tear = tearP >= 0f ? UIEase.InCubic(tearP) : 0f;

            var pos = new Vector2(0f, lift - 40f * (1f - back)) + new Vector2(110f, 90f) * tear;
            body.anchoredPosition = pos;
            body.localRotation = Quaternion.Euler(0, 0, tilt + 9f * (1f - back) - 16f * tear);
            bodyGroup.alpha = UIEase.OutCubic(dealP) * (1f - tear);

            play.rectTransform.localScale = Vector3.one * playScale;

            var sr = shadow.rectTransform;
            sr.localScale = Vector3.one * (1f + 0.05f * shadowGrow);
            sr.localRotation = body.localRotation;
            shadow.color = new Color(1, 1, 1, (0.75f + 0.25f * shadowGrow) * bodyGroup.alpha);
        }

        // ---------- Helpers ----------

        static RectTransform UIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Image Img(string name, Transform parent, Sprite sprite)
        {
            var img = UIObj(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (!sprite) img.color = Color.clear;
            return img;
        }

        static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size, FontStyles style, Color color)
        {
            var t = UIObj(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.text = value ?? "";
            t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
