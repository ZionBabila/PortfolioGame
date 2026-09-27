using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Converts the open scene's text to TextMeshPro in place (UI Text → TextMeshProUGUI, TextMesh → TextMeshPro),
    /// keeping text, size, style, color, alignment and layout, and re-links the portfolio scripts that point at them.
    /// Everything else in the scene is left alone. Undoable.
    /// </summary>
    public static class TmpSceneConverter
    {
        [MenuItem("Portfolio/Convert Scene Text To TextMeshPro")]
        public static void Convert()
        {
            if (!EnsureTmpResources()) return;
            var font = TMP_Settings.defaultFontAsset;
            int ui = 0, world = 0;

            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                var go = text.gameObject;
                var (value, size, style, color, anchor, raycast) =
                    (text.text, text.fontSize, text.fontStyle, text.color, text.alignment, text.raycastTarget);
                Undo.DestroyObjectImmediate(text);
                var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
                if (font) tmp.font = font;
                tmp.text = value;
                tmp.fontSize = size;
                tmp.fontStyle = Style(style);
                tmp.color = color;
                tmp.alignment = Align(anchor);
                tmp.raycastTarget = raycast;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.text = tmp.text.Replace("✕", "×").Replace("☰  ", ""); // glyphs the default font lacks
                ui++;
            }

            foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
            {
                var go = mesh.gameObject;
                var (value, color) = (mesh.text, mesh.color);
                var renderer = go.GetComponent<MeshRenderer>();
                Undo.DestroyObjectImmediate(mesh);
                if (renderer) Undo.DestroyObjectImmediate(renderer);
                var tmp = Undo.AddComponent<TextMeshPro>(go);
                if (font) tmp.font = font;
                tmp.text = value;
                tmp.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
                tmp.fontSize = 5f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.color = color;
                world++;
            }

            Relink();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[Portfolio] Converted {ui} UI texts and {world} world labels to TextMeshPro. Save the scene (Ctrl+S).");
        }

        /// <summary>The scripts' fields changed from Text/TextMesh to TMP_Text, so their references need re-linking by name.</summary>
        static void Relink()
        {
            foreach (var panel in Object.FindObjectsByType<StationPanel>(FindObjectsInactive.Include))
                Link(panel, panel.transform, ("kindLabel", "Kind"), ("title", "Title"), ("subtitle", "Subtitle"), ("body", "Body"));

            foreach (var button in Object.FindObjectsByType<LinkButton>(FindObjectsInactive.Include))
                Link(button, button.transform, ("label", "Text"));

            foreach (var station in Object.FindObjectsByType<Station>(FindObjectsInactive.Include))
                Link(station, station.transform, ("label", "Label"));
        }

        static void Link(Object owner, Transform root, params (string field, string child)[] map)
        {
            var so = new SerializedObject(owner);
            foreach (var (field, child) in map)
            {
                var target = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == child);
                var prop = so.FindProperty(field);
                if (target && prop != null && !prop.objectReferenceValue) prop.objectReferenceValue = target;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>TMP needs its Essential Resources (default font + settings) in the project; import them once.</summary>
        static bool EnsureTmpResources()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return true;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            if (TMP_Settings.instance != null) return true;
            EditorUtility.DisplayDialog("TextMeshPro",
                "TMP Essential Resources were just imported. Run Portfolio → Convert Scene Text To TextMeshPro again.", "OK");
            return false;
        }

        static FontStyles Style(FontStyle s) => s switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal,
        };

        static TextAlignmentOptions Align(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.TopLeft,
        };
    }
}
