using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>Swaps an Image between the PSD's sibling state layers (_Normal / _Hover / _Pressed).</summary>
    [RequireComponent(typeof(Image))]
    public class SpriteStates : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Sprite normal, hover, pressed;
        Image image;
        bool over, down;

        public static SpriteStates On(Image target, Sprite normal, Sprite hover, Sprite pressed = null)
        {
            var s = target.GetComponent<SpriteStates>();
            if (!s) s = target.gameObject.AddComponent<SpriteStates>();
            s.normal = normal; s.hover = hover; s.pressed = pressed;
            s.Refresh();
            return s;
        }

        public void OnPointerEnter(PointerEventData e) { over = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { over = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { down = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { down = false; Refresh(); }

        void OnDisable() { over = down = false; Refresh(); }

        void Refresh()
        {
            if (!image) image = GetComponent<Image>();
            var s = down && pressed ? pressed : (over || down) && hover ? hover : normal;
            if (s) image.sprite = s;
        }
    }
}
