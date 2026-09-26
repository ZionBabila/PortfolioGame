using System.Collections.Generic;
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
        [SerializeField] Text kindLabel;
        [SerializeField] Text title;
        [SerializeField] Text subtitle;
        [SerializeField] Text body;
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
            kindLabel.text = data.kind.ToString().ToUpperInvariant();
            title.text = data.title;
            subtitle.text = data.subtitle;
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(data.subtitle));
            body.text = data.body;
            accentBar.color = data.accent;
            kindLabel.color = data.accent;

            picture.sprite = data.image;
            picture.gameObject.SetActive(data.image);
            if (data.image) picture.preserveAspect = true;

            foreach (var b in spawned) Destroy(b.gameObject);
            spawned.Clear();
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

        public void Hide()
        {
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
            if (ResponsiveCamera.IsPortrait)
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
