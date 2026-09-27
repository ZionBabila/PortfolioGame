using Unity.Cinemachine;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// A trigger volume that hands the view to its own CinemachineCamera while the player is inside.
    /// The CinemachineBrain blends between cameras, so walking into a zone smoothly moves the camera.
    /// Put zones on the Ignore Raycast layer so they don't swallow clicks on the floor.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        [SerializeField] CinemachineCamera zoneCamera;
        [Tooltip("Must be higher than the follow camera's priority. When zones overlap, the higher one wins.")]
        [SerializeField] int activePriority = 20;

        int inside;

        public CinemachineCamera ZoneCamera => zoneCamera;

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            gameObject.layer = 2; // Ignore Raycast
        }

        void Awake() => SetActive(false);

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            inside++;
            SetActive(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            inside = Mathf.Max(0, inside - 1);
            if (inside == 0) SetActive(false);
        }

        static bool IsPlayer(Collider c) => c.GetComponentInParent<ClickToMove>();

        void SetActive(bool active)
        {
            if (zoneCamera) zoneCamera.Priority = active ? activePriority : 0;
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, inside > 0 ? 0.35f : 0.12f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
