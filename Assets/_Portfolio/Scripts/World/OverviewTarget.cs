using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// What the overview camera looks at: mostly the room, drifting a little toward the player, and
    /// rising with the player's height (stairs, mezzanine) so the upper level comes into view.
    /// </summary>
    public class OverviewTarget : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Vector3 roomCenter;
        [Tooltip("0 = locked on the room center, 1 = locked on the player (horizontally).")]
        [SerializeField, Range(0f, 1f)] float followPlayer = 0.35f;
        [Tooltip("How much of the player's height above the floor the view rises by.")]
        [SerializeField, Range(0f, 1.5f)] float followHeight = 1f;

        public float PlayerHeight => player ? Mathf.Max(0f, player.position.y - roomCenter.y) : 0f;

        void LateUpdate()
        {
            if (!player) return;
            var p = Vector3.Lerp(roomCenter, player.position, followPlayer);
            p.y = roomCenter.y + PlayerHeight * followHeight;
            transform.position = p;
        }
    }
}
