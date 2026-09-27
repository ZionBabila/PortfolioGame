using Unity.Cinemachine;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// What the overview camera looks at: the player, with look-ahead so the frame opens toward where the
    /// player is facing (rule of thirds: the player sits on the third line, two thirds of the screen ahead
    /// of them). Also rises with the player's height (stairs, mezzanine) so the upper level comes into view.
    /// </summary>
    public class OverviewTarget : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Vector3 roomCenter;
        [Tooltip("The camera that frames this target; its angle and lens size define 'a third of the screen'.")]
        [SerializeField] CinemachineCamera viewCamera;
        [Tooltip("0 = locked on the room center, 1 = locked on the player (horizontally).")]
        [SerializeField, Range(0f, 1f)] float followPlayer = 1f;
        [Tooltip("How much of the player's height above the floor the view rises by.")]
        [SerializeField, Range(0f, 1.5f)] float followHeight = 1f;
        [Tooltip("How far off-center the player sits, as a fraction of the screen. 1/6 puts them on the third line.")]
        [SerializeField, Range(0f, 0.4f)] float lookAhead = 1f / 6f;
        [Tooltip("Seconds to swing the look-ahead when the player turns.")]
        [SerializeField] float lookAheadSmoothing = 0.9f;
        [Tooltip("How fast the look-ahead direction follows the walking direction (higher = snappier). Low values ignore the quick turns at path corners.")]
        [SerializeField] float directionFollow = 1.5f;
        [Tooltip("Below this speed (m/s) the direction is kept, so stopping or tiny moves don't swing the frame.")]
        [SerializeField] float minSpeed = 1f;

        Vector3 facing = Vector3.forward;
        Vector3 lastPlayerPos;
        bool hasLastPos;
        Vector3 offset;
        Vector3 offsetVelocity;

        public float PlayerHeight => player ? Mathf.Max(0f, player.position.y - roomCenter.y) : 0f;

        void LateUpdate()
        {
            if (!player) return;

            var p = Vector3.Lerp(roomCenter, player.position, followPlayer);
            p.y = roomCenter.y + PlayerHeight * followHeight;

            offset = Vector3.SmoothDamp(offset, DesiredLookAhead(), ref offsetVelocity, lookAheadSmoothing);
            transform.position = p + offset;
        }

        /// <summary>
        /// Offset in the camera's view plane that moves the player 'lookAhead' of the screen away from the
        /// center, opposite to where they face, i.e. opens the frame in front of them.
        /// </summary>
        Vector3 DesiredLookAhead()
        {
            if (!viewCamera || lookAhead <= 0f) return Vector3.zero;

            // Follow the smoothed *walking* direction, not the character's instantaneous facing: the agent spins at
            // every path corner, which would flip the frame from side to side. Standing still keeps the last direction.
            var pos = player.position;
            if (hasLastPos && Time.deltaTime > 0f)
            {
                var velocity = Vector3.ProjectOnPlane(pos - lastPlayerPos, Vector3.up) / Time.deltaTime;
                if (velocity.magnitude > minSpeed)
                    facing = Vector3.Slerp(facing, velocity.normalized, 1f - Mathf.Exp(-directionFollow * Time.deltaTime)).normalized;
            }
            lastPlayerPos = pos;
            hasLastPos = true;

            var rot = viewCamera.transform.rotation;
            var right = rot * Vector3.right;
            var up = rot * Vector3.up;
            var screenDir = new Vector2(Vector3.Dot(facing, right), Vector3.Dot(facing, up));
            if (screenDir.sqrMagnitude < 1e-4f) return Vector3.zero;
            screenDir.Normalize();

            float halfHeight = viewCamera.Lens.OrthographicSize;
            float halfWidth = halfHeight * Aspect.Ratio;
            // Screen is 2*half wide; 'lookAhead' of it (1/6) takes the player from the center to the third line.
            return right * (screenDir.x * 2f * halfWidth * lookAhead)
                 + up * (screenDir.y * 2f * halfHeight * lookAhead);
        }
    }
}
