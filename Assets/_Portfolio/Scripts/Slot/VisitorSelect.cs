using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Slot
{
    /// <summary>
    /// "Discover what you need to know: who are you?" Shown on the first visit (and from the HUD's "Visiting as" stamp):
    /// the visitor picks HR, Team Lead, Head of Department or Just visiting. The fortune machine tells each of them
    /// different things, and the player's figure is dressed for the part.
    /// </summary>
    public class VisitorSelect : MonoBehaviour
    {
        static readonly Dictionary<VisitorRole, Color> RoleColor = new()
        {
            [VisitorRole.Neutral] = new Color32(239, 233, 220, 255),        // paper
            [VisitorRole.HR] = new Color32(79, 122, 138, 255),              // muted blue
            [VisitorRole.TeamLead] = new Color32(126, 154, 110, 255),       // sage
            [VisitorRole.HeadOfDepartment] = new Color32(58, 53, 51, 255),  // ink suit
        };

        static VisitorSelect instance;
        public static bool IsOpen => instance && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        ClickToMove player;
        UISkin skin;
        TMP_FontAsset typeFont, labelFont;
        CanvasGroup group;
        TMP_Text hudText, youTag;
        bool open, laidOutPortrait, laidOut;
        RectTransform letterRect;
        GridLayoutGroup roleGrid;
        TMP_Text titleText;

        public static void Install(Transform canvas, ClickToMove player)
        {
            if (!canvas || instance) return;
            var go = new GameObject("Visitor Select", typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance = go.AddComponent<VisitorSelect>();
            instance.player = player;
            instance.Build();
            instance.DressPlayer();
            if (!Visitor.Chosen) instance.Show();
        }

        void Show() { open = true; group.blocksRaycasts = true; }
        void Hide() { open = false; group.blocksRaycasts = false; }

        void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, open ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            if (!laidOut || laidOutPortrait != Aspect.IsPortrait) Layout();
        }

        /// <summary>Phones (9:16): the letter takes the full width and the four roles stack as big, thumb-sized cards.</summary>
        void Layout()
        {
            laidOut = true;
            laidOutPortrait = Aspect.IsPortrait;
            bool p = laidOutPortrait;
            letterRect.anchorMin = p ? new Vector2(0.04f, 0.06f) : new Vector2(0.18f, 0.1f);
            letterRect.anchorMax = p ? new Vector2(0.96f, 0.94f) : new Vector2(0.82f, 0.9f);
            letterRect.offsetMin = letterRect.offsetMax = Vector2.zero;
            roleGrid.cellSize = p ? new Vector2(860, 210) : new Vector2(460, 150);
            roleGrid.spacing = p ? new Vector2(0, 30) : new Vector2(28, 24);
            roleGrid.constraint = p ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
            roleGrid.constraintCount = 1;
            titleText.fontSize = p ? 66 : 54;
        }

        void Pick(VisitorRole role)
        {
            Visitor.Choose(role);
            DressPlayer();
            Hide();
        }

        /// <summary>Tints the player's figure for the role and puts a "You · HR" tag above it.</summary>
        void DressPlayer()
        {
            var role = Visitor.Current;
            if (hudText) hudText.text = Visitor.Chosen ? $"Visiting as <b>{Visitor.Title(role)}</b>" : "Who are you?";
            if (!player) return;
            foreach (var r in player.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.GetComponent<TMP_Text>()) continue;
                var mats = r.materials;
                foreach (var m in mats) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", RoleColor[role]);
                r.materials = mats;
            }
            if (!youTag)
            {
                youTag = new GameObject("You Tag").AddComponent<TextMeshPro>();
                youTag.transform.SetParent(player.transform, false);
                var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                youTag.transform.localPosition = new Vector3(0, (agent ? agent.height : 2f) + 0.55f, 0);
                youTag.fontSize = 3f; youTag.alignment = TextAlignmentOptions.Center; youTag.fontStyle = FontStyles.Bold;
                youTag.rectTransform.sizeDelta = new Vector2(6, 1);
                if (typeFont) youTag.font = typeFont;
                youTag.color = skin ? skin.paper : Color.white;
                youTag.outlineWidth = 0.2f; youTag.outlineColor = new Color32(43, 38, 34, 255);
                youTag.gameObject.AddComponent<FaceCamera>();
            }
            youTag.text = Visitor.Chosen && role != VisitorRole.Neutral ? $"You · {Visitor.Title(role)}" : "You";
        }

        // ---------- Build ----------

        void Build()
        {
            skin = UISkin.Default;
            if (skin && !skin.IsReady) skin = null;
            typeFont = skin && skin.typewriter ? skin.typewriter : TMP_Settings.defaultFontAsset;
            labelFont = skin && skin.label ? skin.label : typeFont;
            var ink = skin ? skin.ink : new Color(0.17f, 0.15f, 0.13f);
            var paper = skin ? skin.paper : new Color(0.94f, 0.91f, 0.86f);
            var accent = skin ? skin.accent : new Color(0.89f, 0.38f, 0.12f);
            var soft = skin ? skin.inkSoft : new Color(0.43f, 0.39f, 0.35f);
            var root = (RectTransform)transform;

            // HUD: "Visiting as HR" stamp, bottom-left above Find me / Music (the station letter owns the right side).
            var hb = new GameObject("Visiting As", typeof(RectTransform), typeof(Image), typeof(Button));
            hb.layer = gameObject.layer;
            var hrt = (RectTransform)hb.transform; hrt.SetParent(root, false);
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0, 0); hrt.anchoredPosition = new Vector2(24, 192); hrt.sizeDelta = new Vector2(360, 70);
            hb.GetComponent<Button>().onClick.AddListener(Show);
            hudText = Text("Text", hrt, 22, FontStyles.Normal, accent, TextAlignmentOptions.Center, labelFont);
            Stretch(hudText.rectTransform, 12, 12, 0, 0); hudText.textWrappingMode = TextWrappingModes.NoWrap;
            if (skin) StampButton.Apply(hb, skin, 2, paperBacking: true); else hb.GetComponent<Image>().color = paper;
            hudText.transform.SetAsLastSibling();

            // The welcome letter, above everything.
            var overlay = UIObj("Overlay", root); Stretch(overlay);
            var layer = overlay.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 120;
            overlay.gameObject.AddComponent<GraphicRaycaster>();
            group = overlay.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            overlay.gameObject.AddComponent<Image>().color = new Color(ink.r, ink.g, ink.b, 0.8f);

            var letter = UIObj("Letter", overlay);
            letterRect = letter;
            letter.anchorMin = new Vector2(0.18f, 0.1f); letter.anchorMax = new Vector2(0.82f, 0.9f);
            letter.offsetMin = letter.offsetMax = Vector2.zero;
            var limg = letter.gameObject.AddComponent<Image>();
            if (skin) { limg.sprite = skin.Sliced("BG_Paper", 48); limg.type = Image.Type.Sliced; } else limg.color = paper;
            var ls = letter.gameObject.AddComponent<Shadow>(); ls.effectColor = new Color(0, 0, 0, 0.35f); ls.effectDistance = new Vector2(8, -12);

            var col = UIObj("Column", letter); Stretch(col, 70, 70, 50, 50);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14; v.childControlWidth = v.childControlHeight = true; v.childForceExpandHeight = false;

            var kicker = Text("Kicker", col, 20, FontStyles.Normal, accent, TextAlignmentOptions.Left, labelFont);
            kicker.text = "WELCOME TO ZION BABILA'S STUDIO"; kicker.characterSpacing = 12;
            var title = Text("Title", col, 54, FontStyles.Bold, ink, TextAlignmentOptions.Left, typeFont);
            title.text = "Discover what you need to know.";
            titleText = title;
            var sub = Text("Sub", col, 30, FontStyles.Italic, soft, TextAlignmentOptions.Left, typeFont);
            sub.text = "Tell me who you are, and the fortune machine will reveal what matters to you.";

            var grid = UIObj("Roles", col);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            roleGrid = g;
            g.cellSize = new Vector2(460, 150); g.spacing = new Vector2(28, 24); g.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            int i = 0;
            foreach (VisitorRole role in System.Enum.GetValues(typeof(VisitorRole)))
            {
                if (role == VisitorRole.Neutral) continue;
                RoleCard(grid, role, i++, ink, soft);
            }
            RoleCard(grid, VisitorRole.Neutral, i, ink, soft);
        }

        void RoleCard(Transform parent, VisitorRole role, int index, Color ink, Color soft)
        {
            var go = new GameObject(Visitor.Title(role), typeof(RectTransform), typeof(Image), typeof(Button));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            var r = role;
            go.GetComponent<Button>().onClick.AddListener(() => Pick(r));
            var name = Text("Name", rt, 34, FontStyles.Bold, ink, TextAlignmentOptions.TopLeft, labelFont);
            Stretch(name.rectTransform, 34, 24, 22, 70);
            name.text = Visitor.Title(role);
            var blurb = Text("Blurb", rt, 22, FontStyles.Italic, soft, TextAlignmentOptions.TopLeft, typeFont);
            Stretch(blurb.rectTransform, 34, 24, 78, 14);
            blurb.text = Visitor.Blurb(role);
            if (skin) StampButton.Apply(go, skin, index, paperBacking: true); else go.GetComponent<Image>().color = new Color(0.97f, 0.96f, 0.92f);
            name.transform.SetAsLastSibling(); blurb.transform.SetAsLastSibling();
            // a colour chip for the figure you'll be
            var chip = UIObj("Chip", rt); chip.anchorMin = chip.anchorMax = new Vector2(1, 1); chip.pivot = new Vector2(1, 1);
            chip.sizeDelta = new Vector2(34, 34); chip.anchoredPosition = new Vector2(-26, -26);
            var ci = chip.gameObject.AddComponent<Image>(); ci.color = RoleColor[role]; ci.raycastTarget = false;
            chip.gameObject.AddComponent<Outline>().effectColor = ink;
        }

        TextMeshProUGUI Text(string n, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align, TMP_FontAsset face)
        {
            var t = UIObj(n, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (face) t.font = face;
            t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false;
            return t;
        }

        static RectTransform UIObj(string n, Transform parent)
        {
            var go = new GameObject(n, typeof(RectTransform)); go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false); return rt;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }
    }

    /// <summary>Keeps a world-space tag facing the camera.</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate() { var c = Camera.main; if (c) transform.rotation = c.transform.rotation; }
    }
}
