using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// A rubber-stamp CTA (motion table: Stamp). Rests slightly crooked; on hover it straightens, grows to 1.06 and
    /// swaps to Stamp_Inked; on press it squashes to 0.92, and on release it thumps down and settles.
    /// </summary>
    public class StampButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Image frame;
        TMP_Text label;
        Sprite normal, inked;
        Color accent, card;
        float restTilt, tilt, scale = 1f;
        bool over, down;

        /// <summary>Turns an existing button (e.g. a LinkButton) into a stamp.</summary>
        public static StampButton Apply(GameObject target, UISkin skin, int index)
        {
            var s = target.GetComponent<StampButton>();
            if (!s) s = target.AddComponent<StampButton>();
            s.frame = target.GetComponent<Image>();
            s.label = target.GetComponentInChildren<TMP_Text>(true);
            s.normal = skin.Sliced("Stamp_Normal", 24);
            s.inked = skin.Sliced("Stamp_Inked", 24);
            s.accent = skin.accent;
            s.card = skin.card;
            s.restTilt = (index % 2 == 0 ? -1f : 1f) * (1.2f + (index * 0.37f) % 1f);
            s.tilt = s.restTilt;
            if (s.frame)
            {
                s.frame.type = Image.Type.Sliced;
                s.frame.color = Color.white;
            }
            if (target.TryGetComponent<Button>(out var b)) b.transition = Selectable.Transition.None;
            s.Refresh();
            return s;
        }

        public void OnPointerEnter(PointerEventData e) { over = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { over = false; down = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { down = true; Refresh(); }

        public void OnPointerUp(PointerEventData e)
        {
            if (down) scale = 0.86f; // the thump: overshoot below the press, then spring back up
            down = false;
            Refresh();
        }

        void OnDisable()
        {
            over = down = false;
            tilt = restTilt;
            scale = 1f;
            transform.localRotation = Quaternion.Euler(0, 0, restTilt);
            transform.localScale = Vector3.one;
            Refresh();
        }

        void Refresh()
        {
            bool lit = over || down;
            if (frame && normal) frame.sprite = lit && inked ? inked : normal;
            if (label) label.color = lit ? card : accent;
        }

        void Update()
        {
            bool lit = over || down;
            float targetScale = down ? 0.92f : over ? 1.06f : 1f;
            scale = UIEase.Damp(scale, targetScale, down ? 30f : 14f);
            tilt = UIEase.Damp(tilt, lit ? 0f : restTilt, 12f);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.localRotation = Quaternion.Euler(0, 0, tilt);
        }
    }
}
