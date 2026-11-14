using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// Fingers are bigger than icons: gives a small button an invisible, larger touch target. The pad is a child
    /// graphic, so taps on it reach the button's own click handler.
    /// </summary>
    public static class HitArea
    {
        public static void Grow(Component button, float size = 130f)
        {
            if (!button) return;
            var t = button.transform;
            var existing = t.Find("Hit Area");
            var rt = existing ? (RectTransform)existing : new GameObject("Hit Area", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            if (!existing)
            {
                rt.gameObject.layer = button.gameObject.layer;
                rt.SetParent(t, false);
                rt.SetAsFirstSibling();
                rt.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            }
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
        }
    }
}
