using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Portfolio → Art → Sync Toon Colors From Blender: the toon materials in Materials/Converted keep their own color
    /// (so Inspector tweaks survive a re-import). After recoloring in Blender, run this to copy each Blender material's
    /// base color into its "&lt;name&gt;_Toon" material, warm-tint the textured ones, and set the Workshop Letters ink
    /// outline and shade. Each run is recorded in the console.
    /// </summary>
    /// <summary>Runs <see cref="ToonColorSync.Sync"/> by itself when Blender writes a new Workshop_palette.json.</summary>
    class ToonPaletteWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Any(p => p.EndsWith("/Workshop_palette.json"))) return;
            EditorApplication.delayCall += ToonColorSync.Sync; // after the import finishes
        }
    }

    public static class ToonColorSync
    {
        const string ModelPath = "Assets/_Portfolio/Art/Models/Workshop.blend";
        const string ToonDir = "Assets/_Portfolio/Materials/Converted";

        static readonly Color Ink = new Color32(43, 38, 34, 255);       // #2B2622, the UI ink
        static readonly Color Shade = new Color32(199, 186, 190, 255);  // warm, slightly violet shadow side

        // Textured materials keep their texture; a light tint pulls them into the paper palette.
        static readonly Dictionary<string, Color> TextureTint = new()
        {
            ["plasterWhiteWall"] = new Color32(255, 246, 232, 255),
            ["Poliigon_PlasterPainted_7664_1K"] = new Color32(255, 246, 232, 255),
            ["Material"] = new Color32(246, 236, 220, 255),                       // concrete floor
            ["Poliigon_ConcreteFloorPoured_7656_2K"] = new Color32(246, 236, 220, 255),
            ["polyWood"] = new Color32(255, 240, 222, 255),
            ["GalleryWood"] = new Color32(255, 240, 222, 255),
        };

        // Old toon materials with no Blender source any more.
        static readonly Dictionary<string, Color> Leftovers = new()
        {
            ["M_WallWhite"] = new Color32(241, 235, 223, 255),
            ["M_Concrete"] = new Color32(181, 170, 154, 255),
            ["M_ConcreteEdge"] = new Color32(143, 133, 119, 255),
            ["Glass"] = new Color32(157, 195, 201, 255),
        };

        const string PalettePath = "Assets/_Portfolio/Art/Models/Workshop_palette.json";
        const string ExtractedDir = "Assets/_Portfolio/Art/Models/materials";

        /// <summary>
        /// Reads Workshop_palette.json, written from Blender next to the .blend:
        /// { "materials": { "M_Steel": { "hex": "2B2622", "textured": false }, ... } }.
        /// The model's extracted materials don't follow Blender after the first import, so this file is the link.
        /// </summary>
        static Dictionary<string, Color> ReadPalette()
        {
            var result = new Dictionary<string, Color>();
            var file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), PalettePath);
            if (!System.IO.File.Exists(file)) return result;
            var json = System.IO.File.ReadAllText(file);
            var rx = new System.Text.RegularExpressions.Regex(
                "\"([^\"]+)\"\\s*:\\s*\\{\\s*\"hex\"\\s*:\\s*\"([0-9A-Fa-f]{6})\"\\s*,\\s*\"textured\"\\s*:\\s*(true|false)");
            foreach (System.Text.RegularExpressions.Match m in rx.Matches(json))
                if (m.Groups[3].Value == "false" && ColorUtility.TryParseHtmlString("#" + m.Groups[2].Value, out var c))
                    result[m.Groups[1].Value] = c;
            return result;
        }

        [MenuItem("Portfolio/Art/Sync Toon Colors From Blender")]
        public static void Sync()
        {
            var sources = ReadPalette();
            if (sources.Count == 0) { Debug.LogError($"[Portfolio] No colors in {PalettePath}. Export it from Blender first."); return; }

            // Keep the model's own (extracted) materials in step too, so the imported model matches in the Inspector.
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { ExtractedDir }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m && sources.TryGetValue(m.name, out var c) && m.HasProperty("_BaseColor") && !(m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")))
                {
                    m.SetColor("_BaseColor", c);
                    EditorUtility.SetDirty(m);
                }
            }
            var toons = AssetDatabase.FindAssets("t:Material", new[] { ToonDir })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).ToList();
            var log = new List<string>();

            foreach (var toon in toons)
            {
                var key = toon.name.EndsWith("_Toon") ? toon.name.Substring(0, toon.name.Length - 5) : toon.name;
                Undo.RecordObject(toon, "Sync Toon Colors");
                if (toon.HasProperty("_OutlineColor")) toon.SetColor("_OutlineColor", Ink);
                if (toon.HasProperty("_ShadeColor")) toon.SetColor("_ShadeColor", Shade);

                bool textured = toon.HasProperty("_BaseMap") && toon.GetTexture("_BaseMap");
                if (sources.TryGetValue(key, out var flat))
                {
                    // Flat in Blender (e.g. the floor, switched from a photo texture to a palette color): drop the texture.
                    if (textured) toon.SetTexture("_BaseMap", null);
                    toon.SetColor("_BaseColor", flat);
                    log.Add($"{key}: #{ColorUtility.ToHtmlStringRGB(flat)}{(textured ? " (texture removed)" : "")}");
                }
                else if (textured)
                {
                    var tint = TextureTint.TryGetValue(key, out var t) ? t : Color.white;
                    toon.SetColor("_BaseColor", tint);
                    log.Add($"{key}: texture tint");
                }
                else if (sources.TryGetValue(key, out var c))
                {
                    toon.SetColor("_BaseColor", c);
                    log.Add($"{key}: #{ColorUtility.ToHtmlStringRGB(c)}");
                }
                else if (Leftovers.TryGetValue(key, out var lc))
                {
                    toon.SetColor("_BaseColor", lc);
                    log.Add($"{key}: palette (no Blender source)");
                }
                else log.Add($"{key}: unchanged (no match)");
                EditorUtility.SetDirty(toon);
            }

            // Dana (HR) is built in Unity, not Blender: dress her in the same palette.
            foreach (var (matName, hex) in new[] { ("NPC_Blazer", "4F7A8A"), ("NPC_Tie", "E2611F"), ("NPC_Shirt", "F1EBDF"), ("NPC_Glasses", "2B2622"), ("NPC_Clipboard", "B98A5E") })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Portfolio/Materials/{matName}.mat");
                if (!m || !ColorUtility.TryParseHtmlString("#" + hex, out var c)) continue;
                Undo.RecordObject(m, "Sync Toon Colors");
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", Ink);
                if (m.HasProperty("_ShadeColor")) m.SetColor("_ShadeColor", Shade);
                EditorUtility.SetDirty(m);
                log.Add($"{matName}: #{hex} (character)");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Portfolio] Synced {toons.Count} toon materials from Blender:\n" + string.Join("\n", log));
        }
    }
}
