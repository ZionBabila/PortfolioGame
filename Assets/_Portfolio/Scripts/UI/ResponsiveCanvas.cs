using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>Swaps the CanvasScaler reference resolution between 16:9 and 9:16 so UI stays the same physical size.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvas : MonoBehaviour
    {
        [SerializeField] Vector2 landscapeReference = new(1920, 1080);
        [SerializeField] Vector2 portraitReference = new(1080, 1920);

        CanvasScaler scaler;

        void Awake() => scaler = GetComponent<CanvasScaler>();

        void Update()
        {
            bool portrait = ResponsiveCamera.IsPortrait;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = portrait ? portraitReference : landscapeReference;
            scaler.matchWidthOrHeight = portrait ? 0f : 1f;
        }
    }
}
