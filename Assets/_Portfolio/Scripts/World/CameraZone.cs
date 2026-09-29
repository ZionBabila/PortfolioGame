using Unity.Cinemachine;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// A trigger volume that hands the view to its own CinemachineCamera while the player is inside.
    /// The CinemachineBrain blends between cameras, so walking into a zone smoothly moves the camera.
    /// If a <see cref="station"/> is set, the zone only takes over when the player is actually visiting that
    /// station (clicked it), not when they merely walk past it.
    /// Put zones on the Ignore Raycast layer so they don't swallow clicks on the floor.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        [SerializeField] CinemachineCamera zoneCamera;
        [Tooltip("Must be higher than the overview camera's priority. When zones overlap, the higher one wins.")]
        [SerializeField] int activePriority = 20;
        [Tooltip("Optional: only activate while the player is heading to / standing at this station.")]
        [SerializeField] Station station;

        int inside;
        ClickToMove player;
        bool active;

        public CinemachineCamera ZoneCamera => zoneCamera;

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast
        }

        void Awake() => SetActive(false);

        void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<ClickToMove>();
            if (!p) return;
            player = p;
            inside++;
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.GetComponentInParent<ClickToMove>()) return;
            inside = Mathf.Max(0, inside - 1);
        }

        void Update()
        {
            // Zones stand aside while the visitor is exploring the map by dragging (until "Find me" / walking).
            bool want = inside > 0 && !CameraPanZoom.Exploring && (!station || (player && player.Destination == station));
            if (want != active) SetActive(want);
        }

        void SetActive(bool value)
        {
            active = value;
            if (zoneCamera) zoneCamera.Priority = value ? activePriority : 0;
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, active ? 0.35f : 0.12f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
