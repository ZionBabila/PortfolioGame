using UnityEditor;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Portfolio → Profile: which pitch Play mode shows (the web build reads ?for=techart / ?for=design instead).
    /// </summary>
    public static class ProfileMenu
    {
        const string Root = "Portfolio/Profile/";

        [MenuItem(Root + "General (both)", false, 0)] static void General() => Set("");
        [MenuItem(Root + "Tech Artist  (?for=techart)", false, 1)] static void TechArt() => Set(Profile.TechArt);
        [MenuItem(Root + "Industrial Designer  (?for=design)", false, 2)] static void Design() => Set(Profile.Design);

        [MenuItem(Root + "General (both)", true)] static bool GeneralCheck() => Check("");
        [MenuItem(Root + "Tech Artist  (?for=techart)", true)] static bool TechArtCheck() => Check(Profile.TechArt);
        [MenuItem(Root + "Industrial Designer  (?for=design)", true)] static bool DesignCheck() => Check(Profile.Design);

        static void Set(string value)
        {
            EditorPrefs.SetString(Profile.EditorPrefKey, value);
            UnityEngine.Debug.Log($"[Portfolio] Play mode profile: {(value == "" ? "general" : value)} (takes effect on the next Play).");
        }

        static bool Check(string value)
        {
            Menu.SetChecked(Root + (value == "" ? "General (both)" : value == Profile.TechArt ? "Tech Artist  (?for=techart)" : "Industrial Designer  (?for=design)"),
                EditorPrefs.GetString(Profile.EditorPrefKey, "") == value);
            return true;
        }
    }
}
