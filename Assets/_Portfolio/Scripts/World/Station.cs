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
        [SerializeField] TextMesh label;
        [SerializeField] float hoverScale = 1.08f;

        Vector3 baseScale;
        bool hovered;

        public Vector3 ApproachPosition => approachPoint ? approachPoint.position : transform.position;

        void Awake()
        {
            if (!visual) visual = transform;
            baseScale = visual.localScale;
            if (label && data) label.text = data.title;
        }

        public void SetHovered(bool value) => hovered = value;

        void LateUpdate()
        {
            var target = hovered ? baseScale * hoverScale : baseScale;
            visual.localScale = Vector3.Lerp(visual.localScale, target, Time.deltaTime * 12f);

            // Billboard the label so it stays readable from the isometric camera.
            var cam = Camera.main;
            if (label && cam) label.transform.rotation = cam.transform.rotation;
        }
    }
}
