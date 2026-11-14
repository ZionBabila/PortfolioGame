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
        static Dictionary<string, Color> ReadPalette() => ReadPalette(out _);

        /// <summary>
        /// Flat colors by material name, plus textured entries: { "texture": "silver_oak_veneer_02_diff_2k",
        /// "tint": "C8A57C" } — Blender stores the tint (multiplier that lands the texture on its palette color)
        /// as the material's "unity_tint" property.
        /// </summary>
        static Dictionary<string, Color> ReadPalette(out Dictionary<string, (string texture, Color tint)> textured)
        {
            var result = new Dictionary<string, Color>();
            textured = new Dictionary<string, (string, Color)>();
            var file = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), PalettePath);
            if (!System.IO.File.Exists(file)) return result;
            var json = System.IO.File.ReadAllText(file);
            string Field(string body, string key)
            {
                var f = System.Text.RegularExpressions.Regex.Match(body, "\"" + key + "\"\\s*:\\s*(\"([^\"]*)\"|true|false)");
                return !f.Success ? null : f.Groups[2].Success && f.Groups[1].Value.StartsWith("\"") ? f.Groups[2].Value : f.Groups[1].Value;
            }
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\\{([^{}]*)\\}"))
            {
                var name = m.Groups[1].Value; var body = m.Groups[2].Value;
                if (Field(body, "textured") == "true")
                {
                    var tex = Field(body, "texture");
                    var tint = Color.white;
                    var tintHex = Field(body, "tint");
                    if (!string.IsNullOrEmpty(tintHex)) ColorUtility.TryParseHtmlString("#" + tintHex, out tint);
                    if (!string.IsNullOrEmpty(tex)) textured[name] = (tex, tint);
                }
                else if (ColorUtility.TryParseHtmlString("#" + Field(body, "hex"), out var c))
                    result[name] = c;
            }
            return result;
        }

        static Texture2D FindTexture(string fileName)
        {
            var guid = AssetDatabase.FindAssets(fileName + " t:Texture2D", new[] { "Assets/_Portfolio/Art/Textures" })
                .FirstOrDefault(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == fileName);
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
        }

        static void ApplyTexture(Material m, Texture2D tex, Color tint)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        }

        [MenuItem("Portfolio/Art/Sync Toon Colors From Blender")]
        public static void Sync()
        {
            var sources = ReadPalette(out var texturedSources);
            if (sources.Count == 0) { Debug.LogError($"[Portfolio] No colors in {PalettePath}. Export it from Blender first."); return; }
            var textures = new Dictionary<string, Texture2D>();
            foreach (var kv in texturedSources)
            {
                var t = FindTexture(kv.Value.texture);
                if (t) textures[kv.Key] = t;
                else Debug.LogWarning($"[Portfolio] Texture '{kv.Value.texture}' for {kv.Key} isn't under Art/Textures.");
            }

            // Keep the model's own (extracted) materials in step too, so the imported model matches in the Inspector.
            foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { ExtractedDir }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m && textures.TryGetValue(m.name, out var tx))
                {
                    ApplyTexture(m, tx, texturedSources[m.name].tint);
                    EditorUtility.SetDirty(m);
                }
                else if (m && sources.TryGetValue(m.name, out var c) && m.HasProperty("_BaseColor"))
                {
                    // Flat in Blender = flat here too. Leaving the texture on the source would let the Workshop
                    // sync (PortfolioArtTools.ToonFor) copy it back onto the toon material.
                    if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")) m.SetTexture("_BaseMap", null);
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
                if (textures.TryGetValue(key, out var tx))
                {
                    // Textured in Blender: the texture gives the grain, the tint keeps it on its palette color.
                    var tint = texturedSources[key].tint;
                    ApplyTexture(toon, tx, tint);
                    log.Add($"{key}: {tx.name} x #{ColorUtility.ToHtmlStringRGB(tint)}");
                }
                else if (sources.TryGetValue(key, out var flat))
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
