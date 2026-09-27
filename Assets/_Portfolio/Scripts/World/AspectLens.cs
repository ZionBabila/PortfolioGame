using Unity.Cinemachine;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Per-orientation framing for a CinemachineCamera.
    /// Landscape keeps a fixed orthographic size; portrait keeps a fixed visible width (so phones see
    /// the same amount of the room across, whatever their exact ratio). Also shifts the composer's
    /// screen position so the subject isn't hidden under the station panel (right side in landscape,
    /// bottom sheet in portrait).
    /// Runs in Play mode only: in the Editor the CinemachineCamera's lens is yours to edit.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class AspectLens : MonoBehaviour
    {
        [Tooltip("Off: leave the lens and composer exactly as set on the CinemachineCamera.")]
        [SerializeField] bool controlLens = true;

        [Header("Landscape (16:9)")]
        [SerializeField] float landscapeSize = 4.2f;
        [SerializeField] Vector2 landscapeScreenPosition;

        [Header("Portrait (9:16)")]
        [Tooltip("World units visible across the screen width.")]
        [SerializeField] float portraitVisibleWidth = 6.5f;
        [SerializeField] Vector2 portraitScreenPosition;

        [Header("Height")]
        [Tooltip("Optional: zoom out as the player climbs (stairs, mezzanine).")]
        [SerializeField] OverviewTarget heightSource;
        [Tooltip("Extra orthographic size per meter the player is above the floor.")]
        [SerializeField] float zoomOutPerMeter = 0.35f;

        CinemachineCamera vcam;
        CinemachinePositionComposer composer;
        float lastRatio = -1f;
        float lastHeight = -1f;

        void OnEnable()
        {
            vcam = GetComponent<CinemachineCamera>();
            composer = GetComponent<CinemachinePositionComposer>();
            lastRatio = -1f;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            if (!controlLens) return;
            float ratio = Aspect.Ratio;
            float height = heightSource ? heightSource.PlayerHeight : 0f;
            if (Mathf.Approximately(ratio, lastRatio) && Mathf.Abs(height - lastHeight) < 0.01f) return;
            lastRatio = ratio;
            lastHeight = height;

            bool portrait = ratio < 1f;
            var lens = vcam.Lens;
            lens.OrthographicSize = (portrait ? portraitVisibleWidth / (2f * ratio) : landscapeSize) + height * zoomOutPerMeter;
            vcam.Lens = lens;

            if (composer)
            {
                var comp = composer.Composition;
                comp.ScreenPosition = portrait ? portraitScreenPosition : landscapeScreenPosition;
                composer.Composition = comp;
            }
        }
    }
}
