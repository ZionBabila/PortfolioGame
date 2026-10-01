using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Portfolio
{
    /// <summary>
    /// An HR recruiter who wanders the workshop and, every so often, chases the visitor down to ask an HR question
    /// (see <see cref="HRQuestionSet"/>). Slower than the player, so you can always escape; never interrupts an open
    /// station; after each chat it walks away and leaves you alone for <see cref="cooldown"/> seconds.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class HRRecruiter : MonoBehaviour
    {
        [SerializeField] ClickToMove player;
        [SerializeField] HRDialog dialog;
        [SerializeField] HRQuestionSet questions;
        [SerializeField] StationPanel stationPanel;
        [SerializeField] Transform body;
        [SerializeField] TMP_Text label;

        [Header("Behaviour")]
        [Tooltip("Seconds after the game starts before the first chase.")]
        [SerializeField] float firstChaseDelay = 12f;
        [Tooltip("Seconds of peace after each chat.")]
        [SerializeField] float cooldown = 40f;
        [Tooltip("The player walks at 5, so anything lower can be outrun.")]
        [SerializeField] float chaseSpeed = 3.4f;
        [SerializeField] float wanderSpeed = 1.3f;
        [SerializeField] float wanderRadius = 4f;
        [SerializeField] float catchDistance = 1.25f;
        [Tooltip("Gives up (and tries again later) if the chase takes longer than this.")]
        [SerializeField] float giveUpAfter = 20f;

        [Header("Look")]
        [SerializeField] float bobHeight = 0.1f;
        [SerializeField] float bobSpeed = 12f;

        enum State { Wander, Chase, Talk, Retreat }

        NavMeshAgent agent;
        State state;
        float stateStarted;
        float nextChaseAt;
        float nextRepath;
        float pauseUntil = -1f;
        int[] deck;
        int deckPos;
        bool metBefore;
        Vector3 bodyRest;
        float bobPhase;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (body) bodyRest = body.localPosition;
        }

        void Start()
        {
            nextChaseAt = Time.time + firstChaseDelay;
            SetState(State.Wander);
        }

        void Update()
        {
            if (!player || !dialog || !questions || questions.questions.Count == 0) return;
            float dist = FlatDistance(transform.position, player.transform.position);

            switch (state)
            {
                case State.Wander:
                case State.Retreat:
                    if (Arrived())
                    {
                        if (state == State.Retreat) SetState(State.Wander);
                        // Stand around for a moment, then stroll somewhere nearby.
                        if (pauseUntil < 0f) pauseUntil = Time.time + Random.Range(1f, 3f);
                        else if (Time.time >= pauseUntil) { pauseUntil = -1f; WanderStep(); }
                    }
                    if (Time.time >= nextChaseAt && CanApproach()) SetState(State.Chase);
                    break;

                case State.Chase:
                    if (!CanApproach()) { nextChaseAt = Time.time + 6f; SetState(State.Wander); break; }
                    if (dist <= catchDistance && Mathf.Abs(transform.position.y - player.transform.position.y) < 1f)
                    {
                        StartTalk();
                        break;
                    }
                    if (Time.time - stateStarted > giveUpAfter) { nextChaseAt = Time.time + cooldown * 0.5f; SetState(State.Wander); break; }
                    if (Time.time >= nextRepath)
                    {
                        nextRepath = Time.time + 0.3f;
                        agent.SetDestination(player.transform.position);
                    }
                    break;

                case State.Talk:
                    Face(player.transform.position);
                    break;
            }

            if (label) label.text = state == State.Chase ? "HR!" : "HR";
            Animate();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (label && cam) label.transform.rotation = cam.transform.rotation;
        }

        bool CanApproach() =>
            !HRDialog.IsOpen && !ProjectViewer.IsOpen && !(stationPanel && stationPanel.IsOpen) && player.isActiveAndEnabled;

        void SetState(State next)
        {
            state = next;
            stateStarted = Time.time;
            agent.isStopped = false;
            agent.speed = next switch
            {
                State.Chase => chaseSpeed,
                State.Retreat => wanderSpeed * 2f,
                _ => wanderSpeed,
            };
        }

        void StartTalk()
        {
            SetState(State.Talk);
            agent.ResetPath();
            agent.isStopped = true;
            player.Halt();
            player.FaceTowards(transform.position);
            Face(player.transform.position);

            var set = questions;
            string greeting = !metBefore ? Pick(set.intros) : Pick(set.greetings);
            metBefore = true;
            dialog.Show(set, NextQuestion(), greeting, EndTalk);
        }

        void EndTalk()
        {
            nextChaseAt = Time.time + cooldown;
            SetState(State.Retreat);
            // Walk away from the player.
            var away = transform.position - player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            if (!TryGo(transform.position + away.normalized * wanderRadius * 1.5f)) WanderStep();
        }

        HRQuestion NextQuestion()
        {
            int n = questions.questions.Count;
            if (deck == null || deck.Length != n || deckPos >= n)
            {
                deck = new int[n];
                for (int i = 0; i < n; i++) deck[i] = i;
                for (int i = n - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
                deckPos = 0;
            }
            return questions.questions[deck[deckPos++]];
        }

        void WanderStep()
        {
            var offset = Random.insideUnitCircle * wanderRadius;
            TryGo(transform.position + new Vector3(offset.x, 0f, offset.y));
        }

        bool TryGo(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var hit, 2f, NavMesh.AllAreas)) return false;
            return agent.SetDestination(hit.position);
        }

        bool Arrived() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;

        void Face(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }

        void Animate()
        {
            if (!body) return;
            float speed01 = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(chaseSpeed, 0.01f));
            if (speed01 > 0.05f) bobPhase += Time.deltaTime * bobSpeed;
            body.localPosition = bodyRest + Vector3.up * (Mathf.Abs(Mathf.Sin(bobPhase)) * bobHeight * speed01);
        }

        static string Pick(System.Collections.Generic.List<string> list) =>
            list == null || list.Count == 0 ? null : list[Random.Range(0, list.Count)];

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
