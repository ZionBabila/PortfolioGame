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

        [Header("Layout")]
        [SerializeField, Range(0.2f, 0.6f)] float landscapeWidth = 0.4f;
        [SerializeField, Range(0.3f, 0.9f)] float portraitHeight = 0.58f;
        [SerializeField] float margin = 24f;
        [SerializeField] float animSpeed = 10f;

        readonly List<LinkButton> spawned = new();
        readonly List<GameObject> projectCards = new();
        bool open;
        float shown;

        public bool IsOpen => open;

        void Awake()
        {
            if (closeButton) closeButton.onClick.AddListener(Hide);
            if (linkTemplate) linkTemplate.gameObject.SetActive(false);
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void Show(StationData data)
        {
            if (!data) return;
            // Null-safe: a text field left empty (e.g. before converting the scene to TextMeshPro) just stays blank.
            SetText(kindLabel, data.kind.ToString().ToUpperInvariant());
            SetText(title, data.title);
            SetText(subtitle, data.subtitle);
            if (subtitle) subtitle.gameObject.SetActive(!string.IsNullOrEmpty(data.subtitle));
            SetText(body, data.body);
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
            for (int i = 0; i < data.projects.Count; i++)
            {
                var project = data.projects[i];
                if (!project) continue;
                var card = ProjectViewer.CreateCard(linksRoot, project, font, () =>
                    ProjectViewer.Get(GetComponentInParent<Canvas>().rootCanvas.transform, font, linkTemplate).Show(project));
                card.transform.SetSiblingIndex(projectCards.Count); // cards first, links after
                projectCards.Add(card.gameObject);
            }
            foreach (var link in data.links)
            {
                var b = Instantiate(linkTemplate, linksRoot);
                b.gameObject.SetActive(true);
                b.Bind(link);
                b.GetComponent<Image>().color = data.accent;
                spawned.Add(b);
            }

            if (scroll) scroll.verticalNormalizedPosition = 1f;
            open = true;
            group.blocksRaycasts = true;
        }

        static void SetText(TMP_Text t, string value)
        {
            if (t) t.text = value;
        }

        public void Hide()
        {
            ProjectViewer.HideCurrent();
            open = false;
            group.blocksRaycasts = false;
        }

        void Update()
        {
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
