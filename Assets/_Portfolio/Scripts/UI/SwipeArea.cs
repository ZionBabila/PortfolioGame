using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio
{
    /// <summary>Reports a horizontal swipe: +1 = next (swipe left), -1 = previous (swipe right).</summary>
    public class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] float minDistance = 60f;

        public event Action<int> Swiped;

        Vector2 start;

        public void OnBeginDrag(PointerEventData e) => start = e.position;

        public void OnDrag(PointerEventData e) { } // needed, or Unity never starts a drag here

        public void OnEndDrag(PointerEventData e)
        {
            var d = e.position - start;
            if (Mathf.Abs(d.x) > minDistance && Mathf.Abs(d.x) > Mathf.Abs(d.y)) Swiped?.Invoke(d.x < 0 ? 1 : -1);
        }
    }
}
