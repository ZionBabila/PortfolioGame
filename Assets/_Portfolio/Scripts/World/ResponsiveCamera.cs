using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Isometric orthographic camera that adapts to the browser's aspect ratio.
    /// Landscape (16:9): wider view, drifts toward the player.
    /// Portrait (9:16): zooms in to a fixed visible width and follows the player.
    /// With <see cref="confineToRoom"/>, the view never shows anything outside the room:
    /// every screen corner must land on the floor or on one of the back walls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ResponsiveCamera : MonoBehaviour
    {
        public static bool IsPortrait => Screen.height > Screen.width;

        [SerializeField] Transform target;
        [SerializeField] Vector3 mapCenter;
        [SerializeField] Vector2 mapHalfExtents = new(14f, 14f);
        [SerializeField] Vector3 viewAngles = new(35f, 45f, 0f);
        [SerializeField] float distance = 60f;

        [Header("Landscape")]
        [SerializeField] float landscapeSize = 11f;
        [SerializeField, Range(0f, 1f)] float landscapeFollow = 0.3f;

        [Header("Portrait")]
        [SerializeField] float portraitVisibleWidth = 15f;
        [SerializeField, Range(0f, 1f)] float portraitFollow = 1f;

        [Header("Room confinement")]
        [SerializeField] bool confineToRoom;
        [Tooltip("Floor rectangle (x, z) the view must stay inside.")]
        [SerializeField] Vector2 roomMin;
        [SerializeField] Vector2 roomMax;
        [Tooltip("Visible floor rectangle (x, z): the room plus any non-walkable apron beyond the open sides.")]
        [SerializeField] Vector2 viewFloorMin;
        [SerializeField] Vector2 viewFloorMax;
        [SerializeField] float floorY;
        [Tooltip("Height of the back walls; the upper screen corners may land on them.")]
        [SerializeField] float wallHeight = 6f;
        [SerializeField] float edgeMargin = 0.05f;
        [Tooltip("Fraction of the widest fitting view to use. Below 1 leaves room to pan after the player.")]
        [SerializeField, Range(0.5f, 1f)] float panRoom = 0.85f;

        [SerializeField] float smoothTime = 0.35f;

        Camera cam;
        Vector3 anchor;          // best-fitting focus for the current aspect (room confinement)
        float anchorAspect = -1f;
        float anchorSize = float.MaxValue; // widest view that fits at the anchor
        Vector3 focus;
        Vector3 focusVelocity;
        float sizeVelocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            anchor = mapCenter;
            UpdateAnchor();
            cam.orthographicSize = FitSize(DesiredSize());
            focus = Confine(DesiredFocus(), cam.orthographicSize);
            Apply();
        }

        void LateUpdate()
        {
            UpdateAnchor();
            float size = FitSize(DesiredSize());
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, smoothTime);
            var desired = Confine(DesiredFocus(), cam.orthographicSize);
            focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity, smoothTime);
            focus = Confine(focus, cam.orthographicSize); // smoothing must never reveal the outside either
            Apply();
        }

        float DesiredSize() => IsPortrait
            ? portraitVisibleWidth / (2f * Mathf.Max(cam.aspect, 0.1f))
            : landscapeSize;

        Vector3 DesiredFocus()
        {
            if (!target) return mapCenter;
            float follow = IsPortrait ? portraitFollow : landscapeFollow;
            var f = Vector3.Lerp(mapCenter, target.position, follow);
            f.x = Mathf.Clamp(f.x, mapCenter.x - mapHalfExtents.x, mapCenter.x + mapHalfExtents.x);
            f.z = Mathf.Clamp(f.z, mapCenter.z - mapHalfExtents.y, mapCenter.z + mapHalfExtents.y);
            f.y = mapCenter.y;
            return f;
        }

        void Apply()
        {
            var rot = Quaternion.Euler(viewAngles);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
        }

        // ---------- confinement ----------

        /// <summary>
        /// Seen at an angle the room is a hexagon, and the floor center is not where the screen rectangle fits best.
        /// Search the floor once per aspect ratio (i.e. on resize / rotate) for the focus that allows the widest view.
        /// </summary>
        void UpdateAnchor()
        {
            if (!confineToRoom || Mathf.Approximately(anchorAspect, cam.aspect)) return;
            anchorAspect = cam.aspect;
            float best = 0f;
            anchor = mapCenter;
            for (float x = roomMin.x + 1f; x <= roomMax.x - 1f; x += 0.5f)
            for (float z = roomMin.y + 1f; z <= roomMax.y - 1f; z += 0.5f)
            {
                var c = new Vector3(x, mapCenter.y, z);
                if (!Fits(c, best + 0.01f)) continue;
                float lo = best, hi = 30f;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (Fits(c, mid)) lo = mid; else hi = mid;
                }
                best = lo;
                anchor = c;
            }
            anchorSize = best;
        }

        /// <summary>Largest size ≤ desired whose view fits the room at the anchor.</summary>
        float FitSize(float desired)
        {
            if (!confineToRoom) return desired;
            desired = Mathf.Min(desired, anchorSize * panRoom);
            if (Fits(anchor, desired)) return desired;
            float lo = 0.5f, hi = desired;
            for (int i = 0; i < 14; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(anchor, mid)) lo = mid; else hi = mid;
            }
            return lo;
        }

        /// <summary>Moves the focus from the anchor toward <paramref name="desired"/> as far as the view still fits.</summary>
        Vector3 Confine(Vector3 desired, float size)
        {
            if (!confineToRoom || Fits(desired, size)) return desired;
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 14; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(Vector3.Lerp(anchor, desired, mid), size)) lo = mid; else hi = mid;
            }
            return Vector3.Lerp(anchor, desired, lo);
        }

        bool Fits(Vector3 focusPoint, float size)
        {
            var rot = Quaternion.Euler(viewAngles);
            var fwd = rot * Vector3.forward;
            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            var origin = focusPoint - fwd * distance;
            float w = size * cam.aspect;

            for (int sx = -1; sx <= 1; sx++)
            for (int sy = -1; sy <= 1; sy++)
            {
                if (sx == 0 && sy == 0) continue;
                if (!RayHitsRoom(origin + right * (w * sx) + up * (size * sy), fwd)) return false;
            }
            return true;
        }

        bool RayHitsRoom(Vector3 o, Vector3 d)
        {
            float m = edgeMargin;
            // Floor.
            if (d.y < -1e-5f)
            {
                var p = o + d * ((floorY - o.y) / d.y);
                if (p.x >= viewFloorMin.x + m && p.x <= viewFloorMax.x - m && p.z >= viewFloorMin.y + m && p.z <= viewFloorMax.y - m) return true;
            }
            // Back walls: the two sides the camera is looking toward.
            if (Mathf.Abs(d.x) > 1e-5f)
            {
                float wallX = d.x > 0 ? roomMax.x : roomMin.x;
                float t = (wallX - o.x) / d.x;
                var p = o + d * t;
                if (t > 0 && p.y >= floorY && p.y <= floorY + wallHeight - m && p.z >= roomMin.y && p.z <= roomMax.y) return true;
            }
            if (Mathf.Abs(d.z) > 1e-5f)
            {
                float wallZ = d.z > 0 ? roomMax.y : roomMin.y;
                float t = (wallZ - o.z) / d.z;
                var p = o + d * t;
                if (t > 0 && p.y >= floorY && p.y <= floorY + wallHeight - m && p.x >= roomMin.x && p.x <= roomMax.x) return true;
            }
            return false;
        }
    }
}
