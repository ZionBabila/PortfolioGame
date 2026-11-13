using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// "Draws on" a *_Stroke sprite by animating an Image fill. A fill sweep only approximates following the path,
    /// which reads well for these short single-line drawings.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class StrokeDraw : MonoBehaviour
    {
        public float duration = 1.2f;
        public Image.FillMethod method = Image.FillMethod.Horizontal;

        Image image;
        Coroutine running;

        Image Img
        {
            get
            {
                if (!image) image = GetComponent<Image>();
                return image;
            }
        }

        public static StrokeDraw On(Image target, float duration, Image.FillMethod method = Image.FillMethod.Horizontal)
        {
            var d = target.GetComponent<StrokeDraw>();
            if (!d) d = target.gameObject.AddComponent<StrokeDraw>();
            d.duration = duration;
            d.method = method;
            return d;
        }

        public void Play(float delay = 0f)
        {
            var img = Img;
            img.type = Image.Type.Filled;
            img.fillMethod = method;
            img.fillOrigin = 0;
            img.fillClockwise = true;
            img.fillAmount = 0f;
            if (!isActiveAndEnabled) { img.fillAmount = 1f; return; }
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run(delay));
        }

        IEnumerator Run(float delay)
        {
            float t = -delay;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                Img.fillAmount = UIEase.InOutSine(t / duration);
                yield return null;
            }
            Img.fillAmount = 1f;
            running = null;
        }

        void OnDisable()
        {
            if (image) image.fillAmount = 1f;
            running = null;
        }
    }
}
