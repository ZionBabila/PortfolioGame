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
            label.fontSize *= 0.6f; // Courier on a tag reads at a smaller size, and the tag mustn't hide the station
            tilt = PaperTag.Dress(label, skin, name);
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
