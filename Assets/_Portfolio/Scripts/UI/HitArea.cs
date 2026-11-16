using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// Fingers are bigger than icons: gives a small button an invisible, larger touch target. The pad is a child
    /// graphic, so taps on it reach the button's own click handler. With a mouse the pad is only a small margin, so
    /// hover lights the button when the cursor is actually on it.
    /// </summary>
    public static class HitArea
    {
        const float DesktopSize = 64f; // a mouse needs only a small margin around the icon

        static bool Touch => Application.isMobilePlatform || Touchscreen.current != null;

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
            float side = Touch ? size : Mathf.Min(size, DesktopSize);
            rt.sizeDelta = new Vector2(side, side);
        }
    }
}
