using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Turns every .ttf/.otf in Assets/_Portfolio/Art/Fonts into a dynamic TextMeshPro font asset
    /// (glyphs are added on demand, so Hebrew works too), gives each a fallback for symbols the font lacks,
    /// and makes the chosen one the project's default TMP font.
    /// </summary>
    public static class PortfolioFonts
    {
        const string FontsDir = "Assets/_Portfolio/Art/Fonts";
        const string DefaultFontFile = "Roboto.ttf";
        // Hebrew isn't in Roboto; Heebo is its Hebrew companion (same design), so it fills in Hebrew text.
        const string HebrewFontFile = "Heebo.ttf";

        [MenuItem("Portfolio/Fonts/Create TMP Font Assets From Art Fonts")]
        public static void CreateFontAssets()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Fonts", "Stop Play mode first.", "OK");
                return;
            }
            if (TMP_Settings.instance == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh();
            }
            // The TMP default (LiberationSans SDF) becomes the fallback for glyphs our fonts don't have (e.g. →).
            var fallback = TMP_Settings.defaultFontAsset;

            TMP_FontAsset chosenDefault = null, hebrew = null;
            var fontPaths = Directory.GetFiles(FontsDir)
                .Where(p => p.EndsWith(".ttf") || p.EndsWith(".otf"))
                .Select(p => p.Replace('\\', '/'));

            foreach (var fontPath in fontPaths)
            {
                var assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (!fontAsset)
                {
                    var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                    fontAsset = TMP_FontAsset.CreateFontAsset(font); // dynamic atlas, SDF
                    fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                    AssetDatabase.CreateAsset(fontAsset, assetPath);
                    // The atlas texture and material live inside the font asset.
                    fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                    fontAsset.material.name = fontAsset.name + " Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                fontAsset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>(); // null on new assets
                if (fallback && fallback != fontAsset && !fontAsset.fallbackFontAssetTable.Contains(fallback))
                    fontAsset.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(fontAsset);

                if (Path.GetFileName(fontPath) == DefaultFontFile) chosenDefault = fontAsset;
                if (Path.GetFileName(fontPath) == HebrewFontFile) hebrew = fontAsset;
                Debug.Log($"[Portfolio] TMP font asset ready: {assetPath}");
            }

            if (chosenDefault && hebrew && !chosenDefault.fallbackFontAssetTable.Contains(hebrew))
            {
                chosenDefault.fallbackFontAssetTable.Insert(0, hebrew); // Hebrew first, then symbols
                EditorUtility.SetDirty(chosenDefault);
            }

            if (chosenDefault)
            {
                var settings = new SerializedObject(TMP_Settings.instance);
                settings.FindProperty("m_defaultFontAsset").objectReferenceValue = chosenDefault;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[Portfolio] Default TMP font: {chosenDefault.name} (used by new text; existing text keeps its font until you change it).");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
