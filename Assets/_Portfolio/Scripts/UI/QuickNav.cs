using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// A "stations" menu for visitors who'd rather not walk: lists every station and sends the player there.
    /// With a <see cref="UISkin"/>, the toggle is a stamp on paper and each row gets its station icon from the PSD,
    /// lit in accent while that station's letter is open.
    /// </summary>
    public class QuickNav : MonoBehaviour
    {
        [SerializeField] Button toggle;
        [SerializeField] GameObject list;
        [SerializeField] Button itemTemplate;

        readonly List<Button> items = new();
        readonly List<(StationData data, Image icon, Sprite normal, Sprite active, TMP_Text label)> rows = new();
        UISkin skin;
        Vector3 listScale = Vector3.one;
        float listShown;

        public void Build(IReadOnlyList<Station> stations, ClickToMove player)
        {
            skin = UISkin.Default;
            if (skin && !skin.IsReady) skin = null;

            itemTemplate.gameObject.SetActive(false);
            list.SetActive(false);
            toggle.onClick.AddListener(() => SetOpen(!list.activeSelf));

            if (skin) SkinShell();

            // The pitch decides the order: games first for a tech-art link, design projects first for a design link.
            StationKind? lead = Profile.Current == Profile.TechArt ? StationKind.Game
                : Profile.Current == Profile.Design ? StationKind.Project : null;
            var ordered = lead.HasValue
                ? System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderByDescending(stations, s => s.data && s.data.kind == lead.Value))
                : new List<Station>(stations);

            foreach (var station in ordered)
            {
                var item = Instantiate(itemTemplate, itemTemplate.transform.parent);
                item.gameObject.SetActive(true);
                SetLabel(item, station.data ? station.data.title : station.name);
                var s = station;
                item.onClick.AddListener(() =>
                {
                    SetOpen(false);
                    player.GoTo(s);
                });
                items.Add(item);
                if (skin) SkinRow(item, station.data);
            }

            if (skin)
            {
                StationPanel.CurrentChanged += RefreshActive;
                RefreshActive();
            }
        }

        public void Close() => SetOpen(false);

        void OnDestroy() => StationPanel.CurrentChanged -= RefreshActive;

        void SetOpen(bool value)
        {
            if (value && !list.activeSelf) listShown = 0f; // unfold from the toggle each time
            list.SetActive(value);
        }

        void Update()
        {
            if (!skin || !list.activeSelf || listShown >= 1f) return;
            listShown = Mathf.Min(1f, listShown + Time.unscaledDeltaTime / 0.28f);
            float back = UIEase.OutBack(listShown, 1.2f);
            var rt = (RectTransform)list.transform;
            rt.localScale = new Vector3(listScale.x, listScale.y * Mathf.Max(0.01f, back), 1f);
            rt.localRotation = Quaternion.Euler(0, 0, 1.5f * (1f - back));
        }

        // ---------- Skin ----------

        void SkinShell()
        {
            StampButton.Apply(toggle.gameObject, skin, 0, paperBacking: true);

            if (list.TryGetComponent<Image>(out var bg))
            {
                bg.sprite = skin.Sliced("BG_Paper", 48);
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            foreach (var o in list.GetComponents<Outline>()) o.enabled = false;
            var drop = list.AddComponent<Shadow>();
            drop.effectColor = new Color(skin.ink.r, skin.ink.g, skin.ink.b, 0.22f);
            drop.effectDistance = new Vector2(4, -6);
            ((RectTransform)list.transform).pivot = new Vector2(0.5f, 1f);
            listScale = list.transform.localScale;
        }

        void SkinRow(Button item, StationData data)
        {
            var key = IconKey(data ? data.kind : StationKind.About);
            var normal = skin.Get($"Icon_{key}_Normal");
            var active = skin.Get($"Icon_{key}_Active");

            if (item.TryGetComponent<Image>(out var plate)) plate.color = new Color(1, 1, 1, 0); // keep the hit area
            item.transition = Selectable.Transition.None;

            var label = item.GetComponentInChildren<TMP_Text>(true);
            if (label)
            {
                skin.Label(label, 1f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = skin.ink;
                label.fontStyle = FontStyles.Normal;
                var lr = label.rectTransform;
                lr.offsetMin = new Vector2(76, lr.offsetMin.y);
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.layer = item.gameObject.layer;
            var ir = (RectTransform)iconGo.transform;
            ir.SetParent(item.transform, false);
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0, 0.5f);
            ir.anchoredPosition = new Vector2(14, 0);
            ir.sizeDelta = new Vector2(46, 46);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = normal;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var hover = item.gameObject.AddComponent<NavRowHover>();
            hover.Init(this, rows.Count);
            rows.Add((data, icon, normal, active, label));
        }

        internal void SetHover(int index, bool over)
        {
            var r = rows[index];
            bool lit = over || (r.data && r.data == StationPanel.Current);
            if (r.icon) r.icon.sprite = lit && r.active ? r.active : r.normal;
            if (r.label) r.label.color = lit ? skin.accent : skin.ink;
        }

        void RefreshActive()
        {
            for (int i = 0; i < rows.Count; i++) SetHover(i, false);
        }

        static string IconKey(StationKind kind) => kind switch
        {
            StationKind.Game => "Games",
            StationKind.Project => "Projects",
            StationKind.Career => "Career",
            StationKind.Links => "Contact",
            _ => "About",
        };

        // Works before and after Portfolio → Convert Scene Text To TextMeshPro.
        static void SetLabel(Component item, string text)
        {
            var tmp = item.GetComponentInChildren<TMP_Text>(true);
            if (tmp) { tmp.text = text; return; }
            var legacy = item.GetComponentInChildren<Text>(true);
            if (legacy) legacy.text = text;
        }
    }

    /// <summary>Hover relay for a QuickNav row (the icon redraws in accent).</summary>
    public class NavRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        QuickNav nav;
        int index;

        public void Init(QuickNav owner, int row) { nav = owner; index = row; }
        public void OnPointerEnter(PointerEventData e) => nav.SetHover(index, true);
        public void OnPointerExit(PointerEventData e) => nav.SetHover(index, false);
    }
}
