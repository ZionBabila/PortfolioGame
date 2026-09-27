using UnityEngine;

namespace Portfolio
{
    /// <summary>The two layouts the portfolio supports: landscape (16:9 desktop) and portrait (9:16 phone).</summary>
    public static class Aspect
    {
        public static bool IsPortrait => Screen.height > Screen.width;
        public static float Ratio => Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
    }
}
