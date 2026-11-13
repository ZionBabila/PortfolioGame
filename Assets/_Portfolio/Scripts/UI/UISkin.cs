using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// The "Workshop Letters" look: every sprite from UI_Workshop.psd (by layer name) plus the color tokens.
    /// Filled by Portfolio → UI → Import Workshop PSD. Lives in Resources so the panels pick it up without scene edits;
    /// with no skin asset the UI keeps its old flat look.
    /// </summary>
    [CreateAssetMenu(menuName = "Portfolio/UI Skin", fileName = "UISkin")]
    public class UISkin : ScriptableObject
    {
        public List<Sprite> sprites = new();

        [Header("Tokens (Docs/UI_PSD_Spec.md)")]
        public Color paper = new Color32(239, 233, 220, 255);
        public Color paperEdge = new Color32(217, 208, 189, 255);
        public Color card = new Color32(248, 244, 234, 255);
        public Color ink = new Color32(43, 38, 34, 255);
        public Color inkSoft = new Color32(109, 100, 90, 255);
        public Color accent = new Color32(226, 97, 31, 255);

        static UISkin cached;
        Dictionary<string, Sprite> byName;
        readonly Dictionary<string, Sprite> slicedCache = new();

        /// <summary>The skin in Resources/UISkin, or null if it hasn't been built yet.</summary>
        public static UISkin Default
        {
            get
            {
                if (!cached) cached = Resources.Load<UISkin>("UISkin");
                return cached;
            }
        }

        public bool IsReady => sprites.Count > 0;

        public void SetSprites(List<Sprite> list)
        {
            sprites = list;
            byName = null;
            slicedCache.Clear();
        }

        public Sprite Get(string layerName)
        {
            if (byName == null)
            {
                byName = new Dictionary<string, Sprite>();
                foreach (var s in sprites)
                    if (s) byName[s.name] = s;
            }
            byName.TryGetValue(layerName, out var sprite);
            return sprite;
        }

        /// <summary>
        /// The layer's sprite with a 9-slice border (in PSD pixels, left/bottom/right/top), made at runtime so the PSD
        /// import needs no Sprite Editor work.
        /// </summary>
        public Sprite Sliced(string layerName, Vector4 border)
        {
            var key = layerName + border;
            if (slicedCache.TryGetValue(key, out var s) && s) return s;
            var src = Get(layerName);
            if (!src) return null;
            s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), src.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            s.name = layerName + "_Sliced";
            slicedCache[key] = s;
            return s;
        }

        public Sprite Sliced(string layerName, float border) => Sliced(layerName, Vector4.one * border);

        void OnValidate() => byName = null;
    }
}
