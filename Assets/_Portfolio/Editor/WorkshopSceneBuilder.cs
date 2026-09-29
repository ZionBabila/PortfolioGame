using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Workshop scene tooling. The scene is yours to edit by hand; two entry points:
    ///  * Portfolio → Sync Workshop From Blender (safe, use this): after re-exporting Workshop.fbx, gives new meshes
    ///    toon materials + colliders, turns new ST_&lt;Name&gt; empties into Stations and re-bakes the NavMesh.
    ///    Nothing you changed in the scene is touched.
    ///  * Portfolio → Advanced → Rebuild Workshop Scene From Scratch: regenerates the whole scene (overwrites it).
    /// Every empty named ST_&lt;Name&gt; becomes a clickable Station; ST_&lt;Name&gt;_Approach is where the player stops.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const string WorkshopScenePath = Root + "/Scenes/Workshop.unity";
        const string WorkshopModelPath = Root + "/Art/Models/Workshop.blend"; // Unity imports it directly (runs Blender in the background)

        static readonly Dictionary<string, string> StationFiles = new()
        {
            ["About"] = "01_About",
            ["Games"] = "02_Games",
            ["Projects"] = "03_Projects",
            ["Career"] = "04_Career",
            ["Contact"] = "05_Links",
        };

        [MenuItem("Portfolio/Advanced/Rebuild Workshop Scene From Scratch (overwrites scene)")]
        public static void BuildWorkshop() => BuildWorkshop(confirm: true);

        public static void BuildWorkshop(bool confirm)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopModelPath);
            if (!model)
            {
                Debug.LogError($"[Portfolio] Missing {WorkshopModelPath}. Export it from Blender first (Art/Blender/build_workshop.py).");
                return;
            }
            if (confirm && File.Exists(WorkshopScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Build Workshop Scene?",
                    "This regenerates Workshop.unity from the Blender export. Changes made directly in that scene will be lost.\n\nStation content (.asset files) is kept.",
                    "Rebuild", "Cancel"))
                return;

            EnsureFolder(StationsDir);
            EnsureFolder(MaterialsDir);
            foreach (var seed in Seeds) LoadOrCreateStation(seed);
            ConfigureAgentType();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var workshop = (GameObject)PrefabUtility.InstantiatePrefab(model);
            workshop.name = "Workshop";

            ApplyToonAndColliders(workshop);
            var stations = SetupStations(workshop);

            BakeWorkshopNavMesh(workshop);

            // Look toward the back-left corner (the two walls), whatever axis convention the export used.
            var about = stations.First(s => s.name == "ST_About").transform.position;
            var projects = stations.First(s => s.name == "ST_Projects");
            var toBack = Vector3.ProjectOnPlane(about - projects.transform.position, Vector3.up).normalized;
            float yaw = Mathf.Atan2(toBack.x, toBack.z) * Mathf.Rad2Deg;

            // Sun comes in from behind the back walls, so the window grids throw light patches across the floor.
            BuildLighting();
            var sun = Object.FindAnyObjectByType<Light>();
            sun.transform.rotation = Quaternion.Euler(38f, yaw + 180f - 22f, 0f);
            sun.shadowStrength = 0.75f;
            sun.shadows = LightShadows.Hard;   // crisp window-grid shadows suit the toon look better than soft ones
            sun.shadowBias = 0.15f;
            sun.shadowNormalBias = 0.8f;
            sun.intensity = 1.35f;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.66f);
            ConfigureRendering();

            var mats = new Dictionary<string, Material>
            {
                ["player"] = ToonMat("Player", new Color(0.98f, 0.92f, 0.85f), 0.05f),
                ["roof"] = ToonMat("Roof", new Color(0.9f, 0.4f, 0.35f), 0.05f),
            };
            var player = BuildPlayer(mats);
            var aboutStation = stations.First(s => s.name == "ST_About");
            player.transform.position = Vector3.Lerp(projects.ApproachPosition, aboutStation.ApproachPosition, 0.5f); // middle of the room
            var agent = player.GetComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.3f;
            // Camera zones are triggers: the player needs a collider + kinematic rigidbody to fire them.
            // Ignore Raycast keeps the player (and the zones) from catching floor clicks.
            player.layer = 2;
            var body = player.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.65f, 0f);
            body.height = 1.3f;
            body.radius = 0.3f;
            var rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var marker = Prim(PrimitiveType.Cylinder, "ClickMarker", null, Vector3.zero, new Vector3(0.5f, 0.01f, 0.5f),
                ToonMat("Marker", new Color(1f, 0.95f, 0.5f), 0f));
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            var cam = BuildCamera();
            cam.backgroundColor = new Color(0.86f, 0.83f, 0.78f);
            var floor = workshop.GetComponentsInChildren<Renderer>().First(r => r.name == "Floor").bounds;
            var walls = workshop.GetComponentsInChildren<Renderer>().First(r => r.name == "Walls").bounds;
            var view = floor;
            var apron = workshop.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "FloorApron");
            if (apron) view.Encapsulate(apron.bounds);
            // Top-down with a slight tilt (Coin Master style): look straight at the window wall, 55� down.
            // yaw points at the back-left corner; snap it to the nearest wall direction.
            var viewRotation = Quaternion.Euler(55f, Mathf.Round((yaw + 45f) / 90f) * 90f, 0f);
            BuildCinemachineRig(player.transform, stations, viewRotation, floor, walls, view);

            BuildUI(out var panel, out var nav);
            var mover = player.GetComponent<ClickToMove>();
            SetRefs(mover, ("cam", cam), ("clickMarker", marker.transform), ("body", player.transform.Find("Body")));
            SetValues(mover, ("clickMask", Physics.DefaultRaycastLayers)); // skips Ignore Raycast: player + camera zones
            var game = new GameObject("PortfolioGame").AddComponent<PortfolioGame>();
            SetRefs(game, ("player", mover), ("panel", panel), ("quickNav", nav));

            EditorSceneManager.SaveScene(scene, WorkshopScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(WorkshopScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Portfolio] Workshop scene built with {stations.Count} stations: {WorkshopScenePath}");
        }

        static void ApplyToonAndColliders(GameObject workshop)
        {
            var shader = Shader.Find("Portfolio/ToonOutline");
            foreach (var r in workshop.GetComponentsInChildren<MeshRenderer>(true))
            {
                // Only the model's meshes: station labels (TextMesh / TextMeshPro) also have a MeshRenderer,
                // but their font material must stay as it is, and they have no mesh to collide with.
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh || r.GetComponent<TextMesh>() || r.GetComponent<TMP_Text>()) continue;

                bool smooth = PortfolioArtTools.HasSmoothedNormals(r);
                // Derive each slot from the imported material *by name*, not from the current override by index:
                // the importer may reorder a mesh's materials when you add or change slots in Blender.
                var source = PrefabUtility.GetCorrespondingObjectFromSource(r);
                var imported = source ? source.sharedMaterials : r.sharedMaterials;
                var mats = new Material[imported.Length];
                for (int i = 0; i < imported.Length; i++)
                {
                    var m = imported[i];
                    if (!m || m.shader == shader || m.shader.name == "Portfolio/ToonGlass") mats[i] = m;
                    else if (m.name == "M_Glass") mats[i] = GlassMaterial();
                    else mats[i] = PortfolioArtTools.ToonFor(m, shader, smooth, OutlineWidthFor(m.name));
                }
                if (!r.sharedMaterials.SequenceEqual(mats)) r.sharedMaterials = mats;

                if (r.name == "FloorApron") continue; // visual only: the camera may show it, the player may not walk on it
                if (r.GetComponent<Collider>()) continue; // keep colliders you added or tuned by hand
                var mc = r.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        /// <summary>
        /// Safe update after re-exporting Workshop.fbx from Blender: only adds what's missing
        /// (toon materials on new meshes, colliders, new stations) and re-bakes the NavMesh.
        /// </summary>
        [MenuItem("Portfolio/Sync Workshop From Blender")]
        public static void SyncWorkshop()
        {
            var workshop = GameObject.Find("Workshop");
            if (!workshop || !PrefabUtility.IsPartOfPrefabInstance(workshop))
            {
                EditorUtility.DisplayDialog("Sync Workshop", "Open the Workshop scene first (no \"Workshop\" model instance found).", "OK");
                return;
            }
            Undo.RegisterFullObjectHierarchyUndo(workshop, "Sync Workshop From Blender");
            int before = workshop.GetComponentsInChildren<Station>(true).Length;

            ApplyToonAndColliders(workshop);
            var stations = SetupStations(workshop);
            BakeWorkshopNavMesh(workshop);

            EditorSceneManager.MarkSceneDirty(workshop.scene);
            int added = stations.Count - before;
            Debug.Log($"[Portfolio] Workshop synced: {stations.Count} stations ({added} new), NavMesh re-baked." +
                      (added > 0 ? " New stations have no camera zone yet: Portfolio → Camera → Add Camera Zone." : ""));
        }

        static void BakeWorkshopNavMesh(GameObject workshop)
        {
            var surface = workshop.GetComponent<NavMeshSurface>();
            if (!surface)
            {
                surface = workshop.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            }
            // Re-bake into the same asset the surface already uses (e.g. one baked from its Inspector).
            var existing = surface.navMeshData ? AssetDatabase.GetAssetPath(surface.navMeshData) : null;
            surface.BuildNavMesh();
            var navPath = string.IsNullOrEmpty(existing) ? Root + "/Scenes/Workshop_NavMesh.asset" : existing;
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);
        }

        static Material GlassMaterial()
        {
            var path = MaterialsDir + "/Glass.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            mat = new Material(Shader.Find("Portfolio/ToonGlass"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>
        /// Render quality for both URP assets (the Web build uses "Mobile"): full resolution, 4x MSAA, and a shadow
        /// map sharp enough for the window-grid shadows. Also available as Portfolio → Apply Render Quality.
        /// </summary>
        [MenuItem("Portfolio/Apply Render Quality")]
        public static void ConfigureRendering()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" }))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(asset);
                so.FindProperty("m_RenderScale").floatValue = 1f;    // template Mobile asset renders at 0.8 and upscales
                so.FindProperty("m_MSAA").intValue = 4;              // smooth edges on the outlines
                so.FindProperty("m_ShadowDistance").floatValue = 45f;
                so.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
                so.FindProperty("m_ShadowCascadeCount").intValue = 1;
                so.FindProperty("m_SoftShadowsSupported").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static float OutlineWidthFor(string material) => material switch
        {
            "M_Glass" => 0f,          // panes sit inside the steel frame; the frame carries the line
            "M_Concrete" => 0.02f,
            "M_ConcreteEdge" => 0f,
            "M_Bulb" => 0f,
            _ => 0.025f,
        };

        static List<Station> SetupStations(GameObject workshop)
        {
            // Labels must stay below the wall tops: the camera never shows anything above them.
            var wallsRenderer = workshop.GetComponentsInChildren<Renderer>().First(r => r.name == "Walls");
            float labelCeiling = wallsRenderer.bounds.max.y - 0.9f;
            var roots = workshop.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("ST_") && !t.name.EndsWith("_Approach"))
                .ToList();

            var stations = new List<Station>();
            foreach (var t in roots)
            {
                var existing = t.GetComponent<Station>();
                if (existing) { stations.Add(existing); continue; } // already set up (and maybe hand-tuned)

                var key = t.name.Substring(3);
                StationData data = null;
                if (StationFiles.TryGetValue(key, out var file))
                    data = AssetDatabase.LoadAssetAtPath<StationData>($"{StationsDir}/{file}.asset");
                if (!data) Debug.LogWarning($"[Portfolio] No StationData mapped for {t.name}");

                var renderers = t.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                var box = t.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = t.InverseTransformPoint(bounds.center);
                var size = t.InverseTransformVector(bounds.size);
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

                var label = new GameObject("Label").AddComponent<TextMeshPro>();
                label.transform.SetParent(t, false);
                var labelPos = new Vector3(bounds.center.x, bounds.max.y + 0.6f, bounds.center.z);
                var approachPoint = t.Find(t.name + "_Approach");
                if (labelPos.y > labelCeiling && approachPoint)
                {
                    // Too tall (e.g. the shelf on the mezzanine): hang the label in front of the station instead.
                    var front = Vector3.ProjectOnPlane(approachPoint.position - bounds.center, Vector3.up).normalized;
                    labelPos = bounds.center + front * (bounds.extents.magnitude * 0.5f);
                    labelPos.y = labelCeiling;
                }
                label.transform.position = labelPos;
                if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
                label.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
                label.fontSize = 5f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = Ink;
                label.text = data ? data.title : key;

                var station = t.gameObject.AddComponent<Station>();
                station.data = data;
                station.approachPoint = t.Find(t.name + "_Approach");
                SetRefs(station, ("visual", renderers[0].transform), ("label", label));
                stations.Add(station);
            }
            return stations;
        }

        /// <summary>Humanoid agent sized for furniture: must not step onto 0.75 m desks, but climbs 0.2 m stair risers.</summary>
        static void ConfigureAgentType()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_Settings");
            if (list == null || list.arraySize == 0) return;
            var s = list.GetArrayElementAtIndex(0);
            s.FindPropertyRelative("agentRadius").floatValue = 0.3f;
            s.FindPropertyRelative("agentHeight").floatValue = 1.6f;
            s.FindPropertyRelative("agentClimb").floatValue = 0.25f;
            s.FindPropertyRelative("agentSlope").floatValue = 45f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValues(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogError($"[Portfolio] {target.GetType().Name} has no field '{field}'"); continue; }
                switch (value)
                {
                    case float f: p.floatValue = f; break;
                    case Vector2 v2: p.vector2Value = v2; break;
                    case Vector3 v3: p.vector3Value = v3; break;
                    case bool b: p.boolValue = b; break;
                    case int i: p.intValue = i; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
