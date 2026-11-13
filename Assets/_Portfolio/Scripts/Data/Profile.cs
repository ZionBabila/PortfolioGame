using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Which "pitch" this visit gets, picked by the link you send:
    ///   .../?for=techart  → Tech Artist / game developer
    ///   .../?for=design   → Industrial designer / maker
    ///   no parameter      → the general version (both).
    /// Stations swap their text and links through <see cref="StationData.variants"/>.
    /// In the Editor, Portfolio → Profile picks one for Play mode.
    /// </summary>
    public static class Profile
    {
        public const string TechArt = "techart";
        public const string Design = "design";
        public const string EditorPrefKey = "Portfolio.Profile";

        static string current;
        static bool read;

        public static string Current
        {
            get
            {
                if (!read) { current = Read(); read = true; }
                return current;
            }
        }

        public static bool Is(string profile) => !string.IsNullOrEmpty(profile) && Normalize(profile) == Current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => read = false; // Play mode runs without a domain reload

        static string Read()
        {
#if UNITY_EDITOR
            var chosen = UnityEditor.EditorPrefs.GetString(EditorPrefKey, "");
            if (!string.IsNullOrEmpty(chosen)) return Normalize(chosen);
#endif
            return Normalize(FromUrl(Application.absoluteURL));
        }

        /// <summary>The value of "for" in the page's query string or hash (?for=x, #for=x, #x).</summary>
        static string FromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            int q = url.IndexOfAny(new[] { '?', '#' });
            if (q < 0) return "";
            foreach (var part in url.Substring(q + 1).Split('&', '#', '?'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && (kv[0] == "for" || kv[0] == "p")) return System.Uri.UnescapeDataString(kv[1]);
                if (kv.Length == 1 && Normalize(kv[0]) != "") return kv[0];
            }
            return "";
        }

        public static string Normalize(string raw)
        {
            switch ((raw ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace("_", "").Replace(" ", ""))
            {
                case "techart": case "techartist": case "tech": case "ta": case "game": case "games": case "gamedev":
                    return TechArt;
                case "design": case "industrial": case "industrialdesign": case "industrialdesigner": case "id": case "product":
                    return Design;
                default:
                    return "";
            }
        }
    }
}
