using TMPro;
using UnityEngine;

namespace Portfolio
{
    /// <summary>A clickable building on the map. The player walks to <see cref="approachPoint"/> and the station's content opens.</summary>
    [RequireComponent(typeof(Collider))]
    public class Station : MonoBehaviour
    {
        public StationData data;
        public Transform approachPoint;
        [SerializeField] Transform visual;
        [SerializeField] TMP_Text label;
        [SerializeField] float hoverScale = 1.08f;

        Vector3 baseScale;
        bool hovered;
        UISkin skin;
        float tilt;

        public Vector3 ApproachPosition => approachPoint ? approachPoint.position : transform.position;

        void Awake()
        {
            if (!visual) visual = transform;
            baseScale = visual.localScale;
            if (label && data) label.text = data.title;
            skin = UISkin.Default;
            if (label && skin && skin.IsReady) DressLabel();
            else skin = null;
        }

        /// <summary>The floating name becomes a paper tag: typed title, wavy accent underline, a slight tilt.</summary>
        void DressLabel()
        {
            skin.Type(label, FontStyles.Bold);
            label.color = skin.ink;
            label.fontSize *= 0.6f; // Courier on a tag reads at a smaller size, and the tag mustn't hide the station
            label.ForceMeshUpdate();
            var b = label.textBounds;
            var textRenderer = label.GetComponent<Renderer>();
            int order = textRenderer ? textRenderer.sortingOrder : 0;

            // In label space the camera looks along +Z, so +Z is "behind the text".
            var paper = Part("Tag", skin.Sliced("BG_Paper", 48), new Vector3(b.center.x, b.center.y, 0.06f), order - 3);
            paper.drawMode = SpriteDrawMode.Sliced;
            paper.size = new Vector2(b.size.x + 0.7f, b.size.y + 0.45f);

            var shade = Part("Tag Shadow", paper.sprite, new Vector3(b.center.x + 0.07f, b.center.y - 0.1f, 0.09f), order - 4);
            shade.drawMode = SpriteDrawMode.Sliced;
            shade.size = paper.size;
            shade.color = new Color(skin.ink.r, skin.ink.g, skin.ink.b, 0.28f);

            var line = Part("Underline", skin.Get("Underline_Stroke"), new Vector3(b.center.x, b.min.y - 0.1f, 0.03f), order - 1);
            if (line.sprite) line.transform.localScale = new Vector3(b.size.x / line.sprite.bounds.size.x, 1.2f, 1f);

            tilt = ((name.GetHashCode() & 7) - 3.5f) * 0.6f; // a stable, small, per-station tilt
        }

        SpriteRenderer Part(string partName, Sprite sprite, Vector3 localPos, int order)
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

        public void SetHovered(bool value) => hovered = value;

        void LateUpdate()
        {
            var target = hovered ? baseScale * hoverScale : baseScale;
            visual.localScale = Vector3.Lerp(visual.localScale, target, Time.deltaTime * 12f);

            // Billboard the label so it stays readable from the isometric camera.
            var cam = Camera.main;
            if (label && cam) label.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, hovered ? 0f : tilt);
            if (skin && label) label.color = Color.Lerp(label.color, hovered ? skin.accent : skin.ink, Time.deltaTime * 12f);
        }
    }
}
