using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Portfolio.Slot
{
    /// <summary>
    /// A little Zion who wanders the studio and runs from the visitor. Walk into him to catch him: that's where the
    /// fortune machine's energy comes from. After a catch he pops up somewhere far away and the chase starts over.
    /// Spawned at runtime from the player's own look (same size, accent colour), so the scene doesn't change.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZionRunner : MonoBehaviour
    {
        [SerializeField] float fleeRadius = 4.5f;
        [SerializeField] float catchDistance = 1.5f;
        [SerializeField, Tooltip("Relative to the player's speed: below 1, so he can be caught.")] float speedFactor = 0.7f;
        [SerializeField, Tooltip("Seconds of running before he stops to catch his breath (your chance).")] float stamina = 2.5f;
        [SerializeField] float breather = 1.2f;
        float running;
        [SerializeField] float wanderPause = 2.5f;
        [SerializeField] float caughtPause = 1.5f;

        ClickToMove player;
        NavMeshAgent agent;
        Transform body;
        TMP_Text label;
        Vector3[] navPoints;
        float nextWander, pausedUntil, bobPhase;
        Vector3 bodyRest;

        /// <summary>Creates Zion next to nowhere in particular, dressed like the player but in accent.</summary>
        public static ZionRunner Spawn(ClickToMove player)
        {
            if (!player) return null;
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0) return null;

            var go = new GameObject("Zion (catch me)");
            var agent = go.AddComponent<NavMeshAgent>();
            var playerAgent = player.GetComponent<NavMeshAgent>();
            if (playerAgent) { agent.radius = playerAgent.radius; agent.height = playerAgent.height; agent.speed = playerAgent.speed; agent.angularSpeed = 720f; agent.acceleration = playerAgent.acceleration * 1.5f; }
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            var z = go.AddComponent<ZionRunner>();
            z.player = player;
            z.agent = agent;
            z.navPoints = tri.vertices;
            agent.speed *= z.speedFactor;

            // Look: a copy of the player's body in the accent colour, a little shorter.
            var source = player.GetComponentInChildren<MeshRenderer>();
            if (source)
            {
                var copy = Instantiate(source.gameObject, go.transform);
                foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>()) Destroy(b);
                copy.transform.localPosition = source.transform.localPosition;
                copy.transform.localRotation = source.transform.localRotation;
                copy.transform.localScale = source.transform.localScale * 0.85f;
                var skin = UISkin.Default;
                foreach (var r in copy.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.materials;
                    foreach (var m in mats) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", skin ? skin.accent : new Color(0.89f, 0.38f, 0.12f));
                    r.materials = mats;
                }
                z.body = copy.transform;
                z.bodyRest = copy.transform.localPosition;
            }

            // A small typed name tag that always faces the camera.
            var tag = new GameObject("Name").AddComponent<TextMeshPro>();
            tag.transform.SetParent(go.transform, false);
            tag.transform.localPosition = new Vector3(0, (playerAgent ? playerAgent.height : 2f) + 0.5f, 0);
            tag.text = "Zion";
            tag.fontSize = 3.2f; tag.alignment = TextAlignmentOptions.Center; tag.fontStyle = FontStyles.Bold;
            tag.rectTransform.sizeDelta = new Vector2(4, 1);
            if (UISkin.Default && UISkin.Default.typewriter) tag.font = UISkin.Default.typewriter;
            tag.color = UISkin.Default ? UISkin.Default.accent : Color.red;
            tag.outlineWidth = 0.2f; tag.outlineColor = new Color32(43, 38, 34, 255);
            z.label = tag;

            z.Teleport(far: true);
            return z;
        }

        void Update()
        {
            if (!player || !agent.isOnNavMesh) return;
            var away = transform.position - player.transform.position; away.y = 0;
            float dist = away.magnitude;
            if (Time.time < pausedUntil)
            {
                agent.isStopped = true;
                if (dist <= catchDistance && !SlotMachineUI.IsOpen) Caught(); // still catchable while catching his breath
                return;
            }
            agent.isStopped = false;

            if (dist <= catchDistance && !SlotMachineUI.IsOpen)
            {
                Caught();
                return;
            }
            if (dist < fleeRadius)
            {
                running += Time.deltaTime;
                if (running > stamina)
                {
                    // Out of breath: stands still for a moment, which is when you catch him.
                    running = 0f;
                    pausedUntil = Time.time + breather;
                    agent.ResetPath();
                    if (label) label.text = "Zion <size=70%>(huff)</size>";
                    return;
                }
                if (label) label.text = "Zion";
                // Run directly away; if a wall is in the way, pick the free spot that's farthest from the player.
                var dir = dist > 0.01f ? away / dist : Random.insideUnitSphere;
                dir.y = 0;
                if (!TryGo(transform.position + dir.normalized * 3.5f))
                    TryGo(FarthestNearby());
            }
            else if (Time.time >= nextWander && (!agent.hasPath || agent.remainingDistance < 0.3f))
            {
                running = 0f; // got away: fresh legs for the next chase
                nextWander = Time.time + wanderPause + Random.value * 2f;
                TryGo(transform.position + Random.insideUnitSphere * 4f);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (label && cam) label.transform.rotation = cam.transform.rotation;
            if (!body) return;
            float speed01 = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f));
            if (speed01 > 0.05f) bobPhase += Time.deltaTime * 16f;
            body.localPosition = bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(bobPhase)) * 0.12f * speed01;
        }

        void Caught()
        {
            var data = SlotMachineData.Default;
            Energy.Add(data ? data.energyPerCatch : 2);
            Teleport(far: true);
            running = 0f;
            if (label) label.text = "Zion";
            pausedUntil = Time.time + caughtPause;
        }

        /// <summary>Jumps to a random walkable point, preferably far from the player.</summary>
        void Teleport(bool far)
        {
            Vector3 best = transform.position; float bestDist = -1;
            for (int i = 0; i < 12; i++)
            {
                var p = navPoints[Random.Range(0, navPoints.Length)];
                if (!NavMesh.SamplePosition(p, out var hit, 1f, NavMesh.AllAreas)) continue;
                float d = player ? Vector3.Distance(hit.position, player.transform.position) : 0;
                if (!far) { best = hit.position; break; }
                if (d > bestDist) { bestDist = d; best = hit.position; }
            }
            agent.Warp(best);
            agent.ResetPath();
        }

        Vector3 FarthestNearby()
        {
            Vector3 best = transform.position; float bestDist = -1;
            for (int i = 0; i < 8; i++)
            {
                var p = transform.position + Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * 3f;
                if (!NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                float d = Vector3.Distance(hit.position, player.transform.position);
                if (d > bestDist) { bestDist = d; best = hit.position; }
            }
            return best;
        }

        bool TryGo(Vector3 target)
        {
            if (!NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            agent.SetPath(path);
            return true;
        }
    }
}
