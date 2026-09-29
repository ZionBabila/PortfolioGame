using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Portfolio
{
    /// <summary>
    /// Coin Master-style camera control on top of the overview camera:
    ///  * drag (one finger / mouse) pans the map — a short tap still walks (see <see cref="ClickToMove"/>);
    ///  * pinch (two fingers) / mouse wheel zooms, within limits;
    ///  * the view eases back to the player when they start walking, or via <see cref="Recenter"/> ("Find me").
    /// Lives next to <see cref="OverviewTarget"/>, which adds <see cref="Pan"/> to the framed point (and keeps it
    /// inside the walls); <see cref="AspectLens"/> multiplies the lens by <see cref="Zoom"/>.
    /// </summary>
    [DefaultExecutionOrder(-600)] // before OverviewTarget, so this frame's drag is already applied
    public class CameraPanZoom : MonoBehaviour
    {
        [SerializeField] ClickToMove player;
        [SerializeField] Unity.Cinemachine.CinemachineCamera viewCamera;
        [Tooltip("Pixels a press must travel before it counts as a drag instead of a tap.")]
        [SerializeField] float dragThreshold = 12f;
        [SerializeField, Range(0.3f, 1f)] float minZoom = 0.55f;
        [SerializeField, Range(1f, 2f)] float maxZoom = 1.25f;
        [Tooltip("Zoom change per mouse-wheel notch (0.1 = 10%).")]
        [SerializeField, Range(0.02f, 0.3f)] float wheelZoomStep = 0.1f;
        [SerializeField] float recenterTime = 0.45f;

        /// <summary>True from the frame a press becomes a drag until the frame after release; ClickToMove ignores those releases.</summary>
        public static bool DragConsumedPointer { get; private set; }

        public Vector3 Pan { get; set; }

        /// <summary>True while the visitor has dragged the view away from the player; camera zones stand aside meanwhile.</summary>
        public static bool Exploring { get; private set; }
        public float Zoom { get; private set; } = 1f;

        Vector2 pressStart;
        Vector2 lastPointer;
        bool pressing;
        bool dragging;
        bool pressOverUI;
        bool recentering;
        Vector3 recenterVelocity;
        float lastPinchDistance = -1f;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (player) player.StartedMoving += Recenter;
        }

        void OnDisable()
        {
            if (player) player.StartedMoving -= Recenter;
        }

        /// <summary>Ease the view back to the player (keeps the current zoom).</summary>
        public void Recenter() => recentering = true;

        void Update()
        {
            if (DragConsumedPointer && !pressing) DragConsumedPointer = false; // cleared the frame after release

            if (Touch.activeTouches.Count >= 2) Pinch();
            else
            {
                lastPinchDistance = -1f;
                Drag();
            }

            float scroll = WheelDelta();
            if (Mathf.Abs(scroll) > 0.01f && !OverUI())
            {
                // ~100 px per wheel notch (touchpads send small amounts, giving smooth zoom).
                float notches = Mathf.Clamp(scroll / 100f, -3f, 3f);
                SetZoom(Zoom * Mathf.Pow(1f + wheelZoomStep, -notches)); // wheel up = zoom in
            }

            Exploring = dragging || Pan.sqrMagnitude > 0.01f;

            if (recentering)
            {
                Pan = Vector3.SmoothDamp(Pan, Vector3.zero, ref recenterVelocity, recenterTime);
                if (Pan.sqrMagnitude < 0.0004f) { Pan = Vector3.zero; recentering = false; }
            }
        }

        void Drag()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            var pos = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                pressing = true;
                dragging = false;
                pressOverUI = OverUI();
                pressStart = lastPointer = pos;
                return;
            }
            if (!pressing) return;

            if (pointer.press.isPressed)
            {
                if (pressOverUI) return;
                if (!dragging && (pos - pressStart).magnitude > dragThreshold)
                {
                    dragging = true;
                    DragConsumedPointer = true;
                    recentering = false;
                    LeaveStationView();
                }
                if (dragging) PanByScreenDelta(pos - lastPointer);
                lastPointer = pos;
            }
            else
            {
                pressing = false; // release: DragConsumedPointer stays true for this frame so the tap is ignored
                dragging = false;
            }
        }

        void Pinch()
        {
            var a = Touch.activeTouches[0];
            var b = Touch.activeTouches[1];
            float distance = (a.screenPosition - b.screenPosition).magnitude;
            DragConsumedPointer = true; // a pinch is never a tap
            pressing = true;
            if (lastPinchDistance > 0f && distance > 1f)
                SetZoom(Zoom * (lastPinchDistance / distance));
            // Two-finger drag pans too, following the midpoint.
            var mid = (a.screenPosition + b.screenPosition) * 0.5f;
            if (lastPinchDistance > 0f) PanByScreenDelta(mid - lastPointer);
            lastPointer = mid;
            lastPinchDistance = distance;
            recentering = false;
        }

        void PanByScreenDelta(Vector2 delta)
        {
            if (!viewCamera || Screen.height <= 0) return;
            // "Grab the map": the ground under the finger follows the finger.
            float unitsPerPixel = 2f * viewCamera.Lens.OrthographicSize / Screen.height;
            var rot = viewCamera.transform.rotation;
            Pan -= (rot * Vector3.right) * (delta.x * unitsPerPixel) + (rot * Vector3.up) * (delta.y * unitsPerPixel);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern float PortfolioWheelConsume();

        /// <summary>The Input System gets no wheel events in the web build, so PortfolioWheel.jslib listens to the page.</summary>
        static float WheelDelta() => PortfolioWheelConsume();
#else
        static float WheelDelta() => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#endif

        void SetZoom(float value) => Zoom = Mathf.Clamp(value, minZoom, maxZoom);

        void LeaveStationView()
        {
            if (player) player.ClearDestination();
            var panel = FindAnyObjectByType<StationPanel>();
            if (panel) panel.Hide();
        }

        static bool OverUI() => EventSystem.current && EventSystem.current.IsPointerOverGameObject();
    }
}
