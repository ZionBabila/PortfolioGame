using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Portfolio
{
    /// <summary>Point-and-click movement: click the ground to walk there, click a station to walk to it and open it.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ClickToMove : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] LayerMask clickMask = ~0;
        [SerializeField] float arriveDistance = 0.35f;
        [SerializeField] Transform clickMarker;
        [SerializeField] Transform body;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float bobSpeed = 14f;

        public event Action<Station> ArrivedAtStation;
        public event Action StartedMoving;

        NavMeshAgent agent;
        Station pending;

        /// <summary>The station the player is walking to or standing at (null after clicking the floor).</summary>
        public Station Destination { get; private set; }
        Station hovered;
        Vector3 bodyRest;
        float bobPhase;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (!cam) cam = Camera.main;
            if (body) bodyRest = body.localPosition;
            if (clickMarker) clickMarker.gameObject.SetActive(false);
        }

        void Update()
        {
            HandlePointer();

            if (pending && !agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                var station = pending;
                pending = null;
                if (clickMarker) clickMarker.gameObject.SetActive(false);
                ArrivedAtStation?.Invoke(station);
            }

            AnimateBody();
        }

        void HandlePointer()
        {
            var pointer = Pointer.current;
            if (pointer == null || !cam) return;

            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            Station hit = null;
            Vector3 point = default;
            bool hasPoint = false;

            if (!overUI && Physics.Raycast(cam.ScreenPointToRay(pointer.position.ReadValue()), out var info, 500f, clickMask))
            {
                hit = info.collider.GetComponentInParent<Station>();
                point = info.point;
                hasPoint = true;
            }

            // Touch screens have no hover state.
            SetHovered(pointer is Mouse ? hit : null);

            if (!overUI && hasPoint && pointer.press.wasPressedThisFrame)
            {
                if (hit) GoTo(hit);
                else MoveTo(point);
            }
        }

        public void GoTo(Station station)
        {
            pending = station;
            Destination = station;
            SetDestination(station.ApproachPosition);
        }

        public void MoveTo(Vector3 point)
        {
            pending = null;
            Destination = null;
            SetDestination(point);
        }

        void SetDestination(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var navHit, 3f, NavMesh.AllAreas)) return;
            agent.SetDestination(navHit.position);
            if (clickMarker)
            {
                clickMarker.position = navHit.position + Vector3.up * 0.02f;
                clickMarker.gameObject.SetActive(true);
            }
            StartedMoving?.Invoke();
        }

        void SetHovered(Station station)
        {
            if (hovered == station) return;
            if (hovered) hovered.SetHovered(false);
            hovered = station;
            if (hovered) hovered.SetHovered(true);
        }

        void AnimateBody()
        {
            if (!body) return;
            float speed01 = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f));
            if (speed01 > 0.05f) bobPhase += Time.deltaTime * bobSpeed;
            else if (clickMarker && clickMarker.gameObject.activeSelf && !pending) clickMarker.gameObject.SetActive(false);
            float bob = Mathf.Abs(Mathf.Sin(bobPhase)) * bobHeight * speed01;
            body.localPosition = bodyRest + Vector3.up * bob;
        }
    }
}
