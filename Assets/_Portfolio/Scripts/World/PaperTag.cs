using TMPro;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// The floating name tag look shared by the stations and the fortune machine: typed bold title on a scrap of
    /// paper, with a soft shadow and a wavy accent underline. Returns a small stable tilt for the billboard.
    /// </summary>
    public static class PaperTag
    {
        public static float Dress(TMP_Text label, UISkin skin, string seed)
        {
            if (!label || !skin || !skin.IsReady) return 0f;
            skin.Type(label, FontStyles.Bold);
            label.color = skin.ink;
            label.outlineWidth = 0f;
            label.ForceMeshUpdate();
            var b = label.textBounds;
            var textRenderer = label.GetComponent<Renderer>();
            int order = textRenderer ? textRenderer.sortingOrder : 0;

            // In label space the camera looks along +Z, so +Z is "behind the text".
            var paper = Part(label, "Tag", skin.Sliced("BG_Paper", 48), new Vector3(b.center.x, b.center.y, 0.06f), order - 3);
            paper.drawMode = SpriteDrawMode.Sliced;
            paper.size = new Vector2(b.size.x + 0.7f, b.size.y + 0.45f);

            var shade = Part(label, "Tag Shadow", paper.sprite, new Vector3(b.center.x + 0.07f, b.center.y - 0.1f, 0.09f), order - 4);
            shade.drawMode = SpriteDrawMode.Sliced;
            shade.size = paper.size;
            shade.color = new Color(skin.ink.r, skin.ink.g, skin.ink.b, 0.28f);

            var line = Part(label, "Underline", skin.Get("Underline_Stroke"), new Vector3(b.center.x, b.min.y - 0.1f, 0.03f), order - 1);
            if (line.sprite) line.transform.localScale = new Vector3(b.size.x / line.sprite.bounds.size.x, 1.2f, 1f);

            return (((seed ?? "").GetHashCode() & 7) - 3.5f) * 0.6f; // a stable, small, per-tag tilt
        }

        static SpriteRenderer Part(TMP_Text label, string partName, Sprite sprite, Vector3 localPos, int order)
        {
            var go = new GameObject(partName);
            go.layer = label.gameObject.layer;
            go.transform.SetParent(label.transform, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            return sr;
        }
    }
}
