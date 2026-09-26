using System.Runtime.InteropServices;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Opens URLs in a new tab. In a WebGL build, Application.OpenURL runs outside the browser's
    /// click handler and gets blocked as a popup, so we register the open on the next DOM pointerup instead.
    /// </summary>
    public static class WebLinks
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PortfolioOpenOnPointerUp(string url);
        public const bool DeferToPointerUp = true;
#else
        public const bool DeferToPointerUp = false;
#endif

        /// <summary>Call from a pointer-down handler.</summary>
        public static void ArmForPointerUp(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PortfolioOpenOnPointerUp(url);
#endif
        }

        /// <summary>Call from a click handler (non-WebGL platforms and the Editor).</summary>
        public static void OpenNow(string url) => Application.OpenURL(url);
    }
}
