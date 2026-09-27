using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// One-key switching of the Game view between the two layouts the portfolio supports.
    ///   Ctrl+Alt+1  landscape 16:9
    ///   Ctrl+Alt+2  portrait 9:16
    ///   Ctrl+Alt+0  toggle between them
    /// The aspect presets are added to the Game view's size list on first use.
    /// Unity has no public API for this, so it goes through the GameView's internal types.
    /// </summary>
    public static class GameViewAspect
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // Fixed resolutions, not free aspect ratios: the Game view renders at full 1080p and scales down to fit the
        // window, instead of rendering at the (small) window size and looking blurry.
        [MenuItem("Portfolio/View/Landscape 16∶9 (1920×1080) %&1", priority = 0)]
        public static void Landscape() => Select(1920, 1080, "Portfolio 16:9 1080p");

        [MenuItem("Portfolio/View/Portrait 9∶16 (1080×1920) %&2", priority = 1)]
        public static void Portrait() => Select(1080, 1920, "Portfolio 9:16 1080p");

        [MenuItem("Portfolio/View/Toggle Landscape ⇄ Portrait %&0", priority = 20)]
        public static void Toggle()
        {
            var gv = GetGameView(false);
            bool portrait = gv && gv.position.height > 0 && CurrentIsPortrait(gv);
            if (portrait) Landscape(); else Portrait();
        }

        static bool CurrentIsPortrait(EditorWindow gv)
        {
            // targetSize is the rendered size of the game view (after the aspect constraint).
            var prop = gv.GetType().GetProperty("targetSize", Any);
            if (prop?.GetValue(gv) is Vector2 size) return size.y > size.x;
            return gv.position.height > gv.position.width;
        }

        static void Select(int w, int h, string label)
        {
            var sizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizeType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var sizeKind = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
            if (sizesType == null || sizeType == null || sizeKind == null)
            {
                Debug.LogWarning("[Portfolio] Game view internals changed in this Unity version; pick the aspect in the Game view toolbar.");
                return;
            }

            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType", Any).GetValue(instance);
            var group = sizesType.GetMethod("GetGroup", Any).Invoke(instance, new[] { groupType });
            var groupT = group.GetType();

            int total = (int)groupT.GetMethod("GetTotalCount", Any).Invoke(group, null);
            int index = -1;
            for (int i = 0; i < total; i++)
            {
                var s = groupT.GetMethod("GetGameViewSize", Any).Invoke(group, new object[] { i });
                bool isFixed = sizeType.GetProperty("sizeType", Any).GetValue(s).ToString() == "FixedResolution";
                int sw = (int)sizeType.GetProperty("width", Any).GetValue(s);
                int sh = (int)sizeType.GetProperty("height", Any).GetValue(s);
                if (isFixed && sw == w && sh == h) { index = i; break; }
            }

            if (index < 0)
            {
                var kind = Enum.Parse(sizeKind, "FixedResolution");
                var size = Activator.CreateInstance(sizeType, kind, w, h, label);
                groupT.GetMethod("AddCustomSize", Any).Invoke(group, new[] { size });
                sizesType.GetMethod("SaveToHDD", Any)?.Invoke(instance, null);
                index = total;
            }

            var gv = GetGameView(true);
            var selected = gv.GetType().GetProperty("selectedSizeIndex", Any);
            if (selected != null && selected.CanWrite) selected.SetValue(gv, index);
            else gv.GetType().GetMethod("SizeSelectionCallback", Any)?.Invoke(gv, new object[] { index, null });
            // Full-resolution rendering even when a free aspect ratio is picked by hand.
            gv.GetType().GetProperty("lowResolutionForAspectRatios", Any)?.SetValue(gv, false);
            gv.Repaint();
        }

        static EditorWindow GetGameView(bool focus)
        {
            var type = Type.GetType("UnityEditor.GameView,UnityEditor");
            return focus ? EditorWindow.GetWindow(type) : EditorWindow.GetWindow(type, false, null, false);
        }
    }
}
