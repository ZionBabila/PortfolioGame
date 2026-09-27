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
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [ExecuteAlways]
    public class AspectLens : MonoBehaviour
    {
        [Header("Landscape (16:9)")]
        [SerializeField] float landscapeSize = 4.2f;
        [SerializeField] Vector2 landscapeScreenPosition;

        [Header("Portrait (9:16)")]
        [Tooltip("World units visible across the screen width.")]
        [SerializeField] float portraitVisibleWidth = 6.5f;
        [SerializeField] Vector2 portraitScreenPosition;

        CinemachineCamera vcam;
        CinemachinePositionComposer composer;
        float lastRatio = -1f;

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
            float ratio = Aspect.Ratio;
            if (Mathf.Approximately(ratio, lastRatio)) return;
            lastRatio = ratio;

            bool portrait = ratio < 1f;
            var lens = vcam.Lens;
            lens.OrthographicSize = portrait ? portraitVisibleWidth / (2f * ratio) : landscapeSize;
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
