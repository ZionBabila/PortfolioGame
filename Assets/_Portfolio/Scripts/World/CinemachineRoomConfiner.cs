using Unity.Cinemachine;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Keeps an orthographic Cinemachine view inside a cutaway room: every screen corner must land on the
    /// visible floor or on one of the two back walls, so the outside never shows.
    /// (CinemachineConfiner2D can't do this: it bakes its polygon in world XY, so it only works for cameras
    /// looking down the Z axis, not for an isometric view.)
    ///
    /// Per aspect ratio and view angle it searches for the widest view that fits (the "anchor"); the
    /// lens is capped to that, and the camera is pulled from where the body placed it back toward the
    /// anchor just far enough to fit.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Portfolio Room Confiner")]
    [ExecuteAlways]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    public class CinemachineRoomConfiner : CinemachineExtension
    {
        [Tooltip("Cap the lens size to the widest view that fits inside the walls. Off: the zoom is exactly what the lens says.")]
        public bool LimitZoom = true;
        [Tooltip("Pull the camera back inside when its view would show past the walls. Off: the camera goes wherever its body puts it.")]
        public bool KeepInsideWalls = true;

        [Tooltip("Walkable room rectangle (x, z). The back walls stand on its far edges.")]
        public Vector2 RoomMin;
        public Vector2 RoomMax;
        [Tooltip("Visible floor rectangle (x, z): the room plus the non-walkable apron beyond the open sides.")]
        public Vector2 ViewFloorMin;
        public Vector2 ViewFloorMax;
        public float FloorY;
        public float WallHeight = 6f;
        [Tooltip("Fraction of the widest fitting view the lens may use. Below 1 leaves room to pan.")]
        [Range(0.5f, 1f)] public float PanRoom = 0.85f;
        public float EdgeMargin = 0.05f;

        const float ProbeDistance = 50f;

        float cachedAspect = -1f;
        Quaternion cachedRotation;
        Vector3 anchorFocus;
        float maxSize = float.MaxValue;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body || !state.Lens.Orthographic) return;

            var rot = state.RawOrientation;
            float aspect = state.Lens.Aspect > 0 ? state.Lens.Aspect : 16f / 9f;
            if (!Mathf.Approximately(aspect, cachedAspect) || Quaternion.Angle(rot, cachedRotation) > 0.01f)
                UpdateAnchor(rot, aspect);

            float size = state.Lens.OrthographicSize;
            if (LimitZoom)
            {
                size = Mathf.Min(size, maxSize * PanRoom);
                state.Lens.OrthographicSize = size;
            }
            if (!KeepInsideWalls) return;

            // Safety net: targets that pre-confine themselves (OverviewTarget) rarely trigger this.
            var pos = state.RawPosition;
            var confined = ClampCameraPosition(pos, rot, size, aspect);
            state.PositionCorrection += confined - pos;
        }

        /// <summary>The largest lens size the view can have and still fit inside the room at this angle/aspect.</summary>
        public float MaxFitSize(Quaternion rot, float aspect)
        {
            if (!Mathf.Approximately(aspect, cachedAspect) || Quaternion.Angle(rot, cachedRotation) > 0.01f)
                UpdateAnchor(rot, aspect);
            return maxSize * PanRoom;
        }

        /// <summary>
        /// Nearest point to <paramref name="focus"/> (the point the camera centers on) whose view fits inside
        /// the room. Lets a follow target confine itself *before* the camera's damping, so the camera eases into
        /// the wall limit instead of being corrected abruptly after it.
        /// </summary>
        public Vector3 ConfineFocus(Vector3 focus, Quaternion rot, float size, float aspect)
        {
            if (!KeepInsideWalls) return focus;
            size = Mathf.Min(size, MaxFitSize(rot, aspect));
            var fwd = rot * Vector3.forward;
            var cam = focus - fwd * ProbeDistance;
            return ClampCameraPosition(cam, rot, size, aspect) + fwd * ProbeDistance;
        }

        /// <summary>
        /// Clamps across the view one screen axis at a time (right, then up), each a 1-D search from a position
        /// that fits. Unlike pulling back along a line toward a fixed point, the result changes smoothly as the
        /// target moves, so the camera slides along the walls instead of jumping.
        /// </summary>
        Vector3 ClampCameraPosition(Vector3 pos, Quaternion rot, float size, float aspect)
        {
            if (!Mathf.Approximately(aspect, cachedAspect) || Quaternion.Angle(rot, cachedRotation) > 0.01f)
                UpdateAnchor(rot, aspect);
            var fwd = rot * Vector3.forward;
            var anchorPos = anchorFocus - fwd * ProbeDistance;
            // Only the position across the view matters for an orthographic camera; keep the body's depth.
            var desired = OnViewPlane(pos, anchorPos, fwd);
            if (Fits(desired, rot, size, aspect)) return pos;

            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            var p = anchorPos;
            p += right * Reach(p, right, Vector3.Dot(desired - p, right), rot, size, aspect);
            p += up * Reach(p, up, Vector3.Dot(desired - p, up), rot, size, aspect);
            p += right * Reach(p, right, Vector3.Dot(desired - p, right), rot, size, aspect); // settle into corners
            return p + fwd * Vector3.Dot(pos - p, fwd);
        }

        /// <summary>How far (up to <paramref name="distance"/>) one can move from a fitting position along dir and still fit.</summary>
        float Reach(Vector3 from, Vector3 dir, float distance, Quaternion rot, float size, float aspect)
        {
            if (Mathf.Abs(distance) < 1e-4f || Fits(from + dir * distance, rot, size, aspect)) return distance;
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 14; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(from + dir * (distance * mid), rot, size, aspect)) lo = mid; else hi = mid;
            }
            return distance * lo;
        }

        /// <summary>Moves p along the view direction onto the plane through reference (perpendicular to fwd).</summary>
        static Vector3 OnViewPlane(Vector3 p, Vector3 reference, Vector3 fwd) => p - fwd * Vector3.Dot(p - reference, fwd);

        void UpdateAnchor(Quaternion rot, float aspect)
        {
            cachedAspect = aspect;
            cachedRotation = rot;
            var fwd = rot * Vector3.forward;
            float best = 0f;
            anchorFocus = new Vector3((RoomMin.x + RoomMax.x) * 0.5f, FloorY, (RoomMin.y + RoomMax.y) * 0.5f);
            for (float x = RoomMin.x + 1f; x <= RoomMax.x - 1f; x += 0.5f)
            for (float z = RoomMin.y + 1f; z <= RoomMax.y - 1f; z += 0.5f)
            {
                var f = new Vector3(x, FloorY, z);
                var p = f - fwd * ProbeDistance;
                if (!Fits(p, rot, best + 0.01f, aspect)) continue;
                float lo = best, hi = 30f;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (Fits(p, rot, mid, aspect)) lo = mid; else hi = mid;
                }
                best = lo;
                anchorFocus = f;
            }
            maxSize = best > 0f ? best : float.MaxValue;
        }

        bool Fits(Vector3 camPos, Quaternion rot, float size, float aspect)
        {
            var fwd = rot * Vector3.forward;
            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            float w = size * aspect;
            for (int sx = -1; sx <= 1; sx++)
            for (int sy = -1; sy <= 1; sy++)
            {
                if (sx == 0 && sy == 0) continue;
                if (!RayHitsRoom(camPos + right * (w * sx) + up * (size * sy), fwd)) return false;
            }
            return true;
        }

        bool RayHitsRoom(Vector3 o, Vector3 d)
        {
            float m = EdgeMargin;
            if (d.y < -1e-5f)
            {
                var p = o + d * ((FloorY - o.y) / d.y);
                if (p.x >= ViewFloorMin.x + m && p.x <= ViewFloorMax.x - m && p.z >= ViewFloorMin.y + m && p.z <= ViewFloorMax.y - m)
                    return true;
            }
            // The back walls run the full length of the visible floor (they continue past the cutaway).
            if (Mathf.Abs(d.x) > 1e-5f)
            {
                float wallX = d.x > 0 ? RoomMax.x : RoomMin.x;
                float t = (wallX - o.x) / d.x;
                var p = o + d * t;
                if (t > 0 && p.y >= FloorY && p.y <= FloorY + WallHeight - m && p.z >= ViewFloorMin.y && p.z <= ViewFloorMax.y) return true;
            }
            if (Mathf.Abs(d.z) > 1e-5f)
            {
                float wallZ = d.z > 0 ? RoomMax.y : RoomMin.y;
                float t = (wallZ - o.z) / d.z;
                var p = o + d * t;
                if (t > 0 && p.y >= FloorY && p.y <= FloorY + WallHeight - m && p.x >= ViewFloorMin.x && p.x <= ViewFloorMax.x) return true;
            }
            return false;
        }
    }
}
