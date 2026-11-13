using UnityEngine;

namespace Portfolio
{
    /// <summary>Easing curves for the code-driven UI motion (t in 0..1).</summary>
    public static class UIEase
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        public static float InCubic(float t) { t = Mathf.Clamp01(t); return t * t * t; }
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;

        /// <summary>Overshoots past 1 and settles back (the "with overshoot" in the motion table).</summary>
        public static float OutBack(float t, float s = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        /// <summary>Frame-rate independent approach of <paramref name="current"/> to <paramref name="target"/>.</summary>
        public static float Damp(float current, float target, float sharpness) =>
            Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));
    }
}
