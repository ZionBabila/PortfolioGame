using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Generates the World scene from primitives + the StationData assets.
    /// Safe to re-run: it rebuilds the scene but never overwrites existing station content.
    /// Replace the primitive visuals with real low-poly models once the layout feels right.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const string Root = "Assets/_Portfolio";
        const string ScenePath = Root + "/Scenes/World.unity";
        const string StationsDir = Root + "/Content/Stations";
        const string MaterialsDir = Root + "/Materials";

        static readonly Color Ink = new(0.08f, 0.07f, 0.1f);

        struct StationSeed
        {
            public string file, title, subtitle, body;
            public StationKind kind;
            public Color accent;
            public Vector3 position;
            public (string label, string url)[] links;
        }

        static readonly StationSeed[] Seeds =
        {
            new()
            {
                file = "01_About", kind = StationKind.About, title = "About Me", subtitle = "Game developer",
                body = "Short intro: who you are, what you build, what you're looking for.\n\nEdit this in Assets/_Portfolio/Content/Stations/01_About.asset",
                accent = new Color(0.98f, 0.62f, 0.32f), position = new Vector3(0, 0, -7),
                links = new[] { ("Download CV", "https://example.com/cv.pdf") },
            },
            new()
            {
                file = "02_Games", kind = StationKind.Game, title = "Playable Games", subtitle = "Play in the browser",
                body = "WebGL builds of games I made. Each link opens the game in a new tab.\n\nPut builds in WebGames/<name>/ and link them as games/<name>/",
                accent = new Color(0.45f, 0.78f, 0.45f), position = new Vector3(-9, 0, 2),
                links = new[] { ("Game One", "games/game-one/"), ("Game Two", "games/game-two/") },
            },
            new()
            {
                file = "03_Projects", kind = StationKind.Project, title = "Projects", subtitle = "Selected work",
                body = "Highlights of projects: what it is, your role, tech used, result.",
                accent = new Color(0.42f, 0.62f, 0.95f), position = new Vector3(0, 0, 9),
                links = new[] { ("Project repo", "https://github.com/ZionBabila") },
            },
            new()
            {
                file = "04_Career", kind = StationKind.Career, title = "Career Path", subtitle = "Experience & education",
                body = "2024 - now   Role @ Company\n2022 - 2024  Role @ Company\n2019 - 2022  Studies @ School",
                accent = new Color(0.78f, 0.5f, 0.9f), position = new Vector3(9, 0, 2),
                links = new[] { ("LinkedIn", "https://www.linkedin.com/") },
            },
            new()
            {
                file = "05_Links", kind = StationKind.Links, title = "Contact", subtitle = "Let's talk",
                body = "Where to find me.",
                accent = new Color(0.95f, 0.8f, 0.3f), position = new Vector3(8, 0, -8),
                links = new[] { ("GitHub", "https://github.com/ZionBabila"), ("Email", "mailto:you@example.com") },
            },
        };

        [MenuItem("Portfolio/Legacy/Rebuild Island Scene")]
        public static void Build()
        {
            // The scene is hand-dressed with art after the first build — never replace it silently.
            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Rebuild World Scene?",
                    "This REPLACES World.unity with the placeholder layout. Any art you placed in the scene will be lost.\n\nStation content (the .asset files) is kept.",
                    "Replace scene", "Cancel"))
                return;

            EnsureFolder(StationsDir);
            EnsureFolder(MaterialsDir);
            EnsureFolder(Root + "/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = new Dictionary<string, Material>
            {
                ["ground"] = ToonMat("Ground", new Color(0.62f, 0.8f, 0.5f), 0f),
                ["path"] = ToonMat("Path", new Color(0.93f, 0.85f, 0.68f), 0f),
                ["water"] = ToonMat("Water", new Color(0.45f, 0.72f, 0.9f), 0f),
                ["trunk"] = ToonMat("Trunk", new Color(0.55f, 0.38f, 0.26f), 0.05f),
                ["leaves"] = ToonMat("Leaves", new Color(0.35f, 0.65f, 0.4f), 0.07f),
                ["player"] = ToonMat("Player", new Color(0.98f, 0.92f, 0.85f), 0.05f),
                ["roof"] = ToonMat("Roof", new Color(0.9f, 0.4f, 0.35f), 0.05f),
                ["wall"] = ToonMat("Wall", new Color(0.97f, 0.95f, 0.9f), 0.07f),
                ["marker"] = ToonMat("Marker", new Color(1f, 0.95f, 0.5f), 0f),
            };

            BuildLighting();
            var world = new GameObject("World");

            // Island + water.
            var water = Prim(PrimitiveType.Cylinder, "Water", world.transform, new Vector3(0, -0.3f, 0), new Vector3(60, 0.2f, 60), mats["water"]);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            var ground = Prim(PrimitiveType.Cylinder, "Island", world.transform, new Vector3(0, -0.25f, 0), new Vector3(30, 0.25f, 30), mats["ground"]);
            // The primitive CapsuleCollider would turn a flat disc into a dome; use the real mesh instead.
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.AddComponent<MeshCollider>().sharedMesh = ground.GetComponent<MeshFilter>().sharedMesh;

            // Paths from the plaza to each station.
            var plaza = Prim(PrimitiveType.Cylinder, "Plaza", world.transform, new Vector3(0, 0.01f, 0), new Vector3(5, 0.02f, 5), mats["path"]);
            Object.DestroyImmediate(plaza.GetComponent<Collider>());

            var stations = new List<Station>();
            foreach (var seed in Seeds)
            {
                var data = LoadOrCreateStation(seed);
                PathTo(world.transform, seed.position, mats["path"]);
                stations.Add(BuildStation(world.transform, seed, data, mats));
            }

            // Trees for decoration.
            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 10.5f + (float)rng.NextDouble() * 3.5f;
                Tree(world.transform, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), mats, 0.8f + (float)rng.NextDouble() * 0.6f);
            }

            // NavMesh over the island (trees/buildings carve it).
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            var navPath = Root + "/Scenes/World_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);

            var player = BuildPlayer(mats);
            var marker = Prim(PrimitiveType.Cylinder, "ClickMarker", null, Vector3.zero, new Vector3(0.6f, 0.01f, 0.6f), mats["marker"]);
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            var cam = BuildCamera(player.transform);
            var ui = BuildUI(out var panel, out var nav);

            var mover = player.GetComponent<ClickToMove>();
            SetRefs(mover, ("cam", cam), ("clickMarker", marker.transform), ("body", player.transform.Find("Body")));

            var game = new GameObject("PortfolioGame").AddComponent<PortfolioGame>();
            SetRefs(game, ("player", mover), ("panel", panel), ("quickNav", nav));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Portfolio] World scene built with {stations.Count} stations: {ScenePath}");
        }

        // ---------- World ----------

        static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.62f);
            RenderSettings.fog = false;
        }

        static Station BuildStation(Transform parent, StationSeed seed, StationData data, Dictionary<string, Material> mats)
        {
            var root = new GameObject("Station_" + seed.file);
            root.transform.SetParent(parent);
            root.transform.position = seed.position;
            root.transform.rotation = Quaternion.LookRotation(-seed.position.normalized); // face the plaza

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            var accentMat = ToonMat("Accent_" + seed.file, seed.accent, 0.07f);
            Prim(PrimitiveType.Cube, "Base", visual, new Vector3(0, 0.15f, 0), new Vector3(3.4f, 0.3f, 3.4f), mats["path"]).transform.localPosition = new Vector3(0, 0.15f, 0);
            Prim(PrimitiveType.Cube, "House", visual, Vector3.zero, new Vector3(2.4f, 1.8f, 2.4f), mats["wall"]).transform.localPosition = new Vector3(0, 1.2f, 0);
            var roof = Prim(PrimitiveType.Cube, "Roof", visual, Vector3.zero, new Vector3(1.9f, 1.9f, 2.6f), accentMat);
            roof.transform.localPosition = new Vector3(0, 2.3f, 0);
            roof.transform.localRotation = Quaternion.Euler(0, 0, 45);
            var door = Prim(PrimitiveType.Cube, "Door", visual, Vector3.zero, new Vector3(0.7f, 1.1f, 0.1f), mats["roof"]);
            door.transform.localPosition = new Vector3(0, 0.85f, 1.21f);
            Object.DestroyImmediate(door.GetComponent<Collider>());

            // One trigger-sized collider on the root for clicking; child colliders stay for NavMesh carving.
            var click = root.AddComponent<BoxCollider>();
            click.center = new Vector3(0, 1.5f, 0);
            click.size = new Vector3(3.4f, 3.2f, 3.4f);
            click.isTrigger = true;

            var approach = new GameObject("Approach").transform;
            approach.SetParent(root.transform, false);
            approach.localPosition = new Vector3(0, 0, 2.6f);

            var label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0, 4.1f, 0);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.fontSize = 64;
            label.characterSize = 0.1f;
            label.fontStyle = FontStyle.Bold;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Ink;
            label.text = data.title;

            var station = root.AddComponent<Station>();
            station.data = data;
            station.approachPoint = approach;
            SetRefs(station, ("visual", visual), ("label", label));
            return station;
        }

        static void PathTo(Transform parent, Vector3 target, Material mat)
        {
            var dir = new Vector3(target.x, 0, target.z);
            var p = Prim(PrimitiveType.Cube, "Path", parent, dir * 0.5f + Vector3.up * 0.01f, new Vector3(1.6f, 0.02f, dir.magnitude), mat);
            p.transform.rotation = Quaternion.LookRotation(dir);
            Object.DestroyImmediate(p.GetComponent<Collider>());
        }

        static void Tree(Transform parent, Vector3 pos, Dictionary<string, Material> mats, float s)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(parent);
            t.position = pos;
            Prim(PrimitiveType.Cylinder, "Trunk", t, Vector3.zero, new Vector3(0.3f, 0.5f, 0.3f) * s, mats["trunk"]).transform.localPosition = new Vector3(0, 0.5f * s, 0);
            var crown = Prim(PrimitiveType.Sphere, "Crown", t, Vector3.zero, new Vector3(1.4f, 1.6f, 1.4f) * s, mats["leaves"]);
            crown.transform.localPosition = new Vector3(0, 1.6f * s, 0);
            Object.DestroyImmediate(crown.GetComponent<Collider>());
        }

        static GameObject BuildPlayer(Dictionary<string, Material> mats)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0, 0);
            var body = Prim(PrimitiveType.Capsule, "Body", player.transform, Vector3.zero, new Vector3(0.7f, 0.6f, 0.7f), mats["player"]);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var nose = Prim(PrimitiveType.Cube, "Facing", body.transform, Vector3.zero, new Vector3(0.35f, 0.25f, 0.35f), mats["roof"]);
            nose.transform.localPosition = new Vector3(0, 0.35f, 0.45f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            var agent = player.AddComponent<NavMeshAgent>();
            agent.speed = 5f;
            agent.angularSpeed = 720f;
            agent.acceleration = 20f;
            agent.radius = 0.35f;
            agent.stoppingDistance = 0.1f;
            player.AddComponent<ClickToMove>();
            return player;
        }

        static Camera BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.9f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            go.AddComponent<AudioListener>();
            var rc = go.AddComponent<ResponsiveCamera>();
            SetRefs(rc, ("target", target));
            return cam;
        }

        // ---------- UI ----------

        static GameObject BuildUI(out StationPanel panel, out QuickNav nav)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ResponsiveCanvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvas = canvasGo.transform;

            // --- Station panel ---
            var panelRoot = UIObj("StationPanel", canvas);
            Stretch(panelRoot);
            var group = panelRoot.gameObject.AddComponent<CanvasGroup>();
            panel = panelRoot.gameObject.AddComponent<StationPanel>();

            var sheet = UIObj("Sheet", panelRoot);
            sheet.gameObject.AddComponent<Image>().color = new Color(0.99f, 0.97f, 0.93f, 0.97f);
            sheet.gameObject.AddComponent<Outline>().effectColor = Ink;

            var accent = UIObj("Accent", sheet);
            accent.anchorMin = new Vector2(0, 1); accent.anchorMax = new Vector2(1, 1);
            accent.pivot = new Vector2(0.5f, 1); accent.sizeDelta = new Vector2(0, 12); accent.anchoredPosition = Vector2.zero;
            var accentImg = accent.gameObject.AddComponent<Image>();

            var close = Button("Close", sheet, font, "✕", 44, Ink, new Color(0, 0, 0, 0));
            close.anchorMin = close.anchorMax = new Vector2(1, 1);
            close.pivot = new Vector2(1, 1);
            close.sizeDelta = new Vector2(80, 80);
            close.anchoredPosition = new Vector2(-12, -20);

            // Scrollable content.
            var viewport = UIObj("Viewport", sheet);
            Stretch(viewport, 36, 36, 36, 100);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UIObj("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // catch drags

            var kind = Label("Kind", content, font, 26, FontStyle.Bold, Ink);
            var title = Label("Title", content, font, 58, FontStyle.Bold, Ink);
            var subtitle = Label("Subtitle", content, font, 32, FontStyle.Italic, new Color(0.35f, 0.33f, 0.38f));
            var pic = UIObj("Picture", content);
            var picImg = pic.gameObject.AddComponent<Image>();
            pic.gameObject.AddComponent<LayoutElement>().preferredHeight = 320;
            var body = Label("Body", content, font, 32, FontStyle.Normal, Ink);
            body.lineSpacing = 1.15f;

            var links = UIObj("Links", content);
            var lv = links.gameObject.AddComponent<VerticalLayoutGroup>();
            lv.spacing = 12; lv.childControlHeight = true; lv.childControlWidth = true; lv.childForceExpandHeight = false;
            var linkTpl = Button("LinkTemplate", links, font, "Link", 32, Color.white, Color.gray);
            linkTpl.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
            var linkBtn = linkTpl.gameObject.AddComponent<LinkButton>();
            SetRefs(linkBtn, ("label", linkTpl.GetComponentInChildren<Text>()));

            SetRefs(panel,
                ("sheet", sheet), ("group", group), ("kindLabel", kind), ("title", title), ("subtitle", subtitle),
                ("body", body), ("accentBar", accentImg), ("picture", picImg), ("linksRoot", links),
                ("linkTemplate", linkBtn), ("closeButton", close.GetComponent<Button>()), ("scroll", scroll));

            // --- Quick nav (top-left) ---
            var navRoot = UIObj("QuickNav", canvas);
            navRoot.anchorMin = navRoot.anchorMax = new Vector2(0, 1);
            navRoot.pivot = new Vector2(0, 1);
            navRoot.anchoredPosition = new Vector2(24, -24);
            navRoot.sizeDelta = new Vector2(380, 90);
            nav = navRoot.gameObject.AddComponent<QuickNav>();

            var toggle = Button("Toggle", navRoot, font, "☰  Stations", 34, Ink, new Color(0.99f, 0.97f, 0.93f));
            Stretch(toggle);
            toggle.gameObject.AddComponent<Outline>().effectColor = Ink;

            var list = UIObj("List", navRoot);
            list.anchorMin = new Vector2(0, 0); list.anchorMax = new Vector2(1, 0);
            list.pivot = new Vector2(0.5f, 1);
            list.sizeDelta = Vector2.zero;
            list.anchoredPosition = new Vector2(0, -10);
            list.gameObject.AddComponent<Image>().color = new Color(0.99f, 0.97f, 0.93f, 0.97f);
            list.gameObject.AddComponent<Outline>().effectColor = Ink;
            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(12, 12, 12, 12);
            listLayout.spacing = 8; listLayout.childControlHeight = true; listLayout.childControlWidth = true; listLayout.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var item = Button("ItemTemplate", list, font, "Station", 30, Ink, new Color(0.93f, 0.88f, 0.8f));
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;

            SetRefs(nav, ("toggle", toggle.GetComponent<Button>()), ("list", list.gameObject), ("itemTemplate", item.GetComponent<Button>()));

            // --- Hint ---
            var hint = Label("Hint", canvas, font, 28, FontStyle.Normal, new Color(0.1f, 0.1f, 0.12f, 0.8f));
            var hr = hint.rectTransform;
            hr.anchorMin = new Vector2(0, 0); hr.anchorMax = new Vector2(1, 0); hr.pivot = new Vector2(0.5f, 0);
            hr.anchoredPosition = new Vector2(0, 20); hr.sizeDelta = new Vector2(0, 50);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.text = "Click the floor to walk · click a workstation to open it";
            hint.raycastTarget = false;

            return canvasGo;
        }

        static RectTransform UIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        static Text Label(string name, Transform parent, Font font, int size, FontStyle style, Color color)
        {
            var t = UIObj(name, parent).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        static RectTransform Button(string name, Transform parent, Font font, string text, int size, Color textColor, Color bg)
        {
            var rt = UIObj(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            rt.gameObject.AddComponent<Button>().targetGraphic = img;
            var label = Label("Text", rt, font, size, FontStyle.Bold, textColor);
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return rt;
        }

        // ---------- Helpers ----------

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Material ToonMat(string name, Color color, float outline)
        {
            var path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Portfolio/ToonOutline"));
                mat.SetColor("_BaseColor", color);
                mat.SetColor("_ShadeColor", Color.Lerp(color, new Color(0.35f, 0.3f, 0.5f), 0.55f));
                mat.SetColor("_OutlineColor", Ink);
                mat.SetFloat("_OutlineWidth", outline);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static StationData LoadOrCreateStation(StationSeed seed)
        {
            var path = $"{StationsDir}/{seed.file}.asset";
            var data = AssetDatabase.LoadAssetAtPath<StationData>(path);
            if (data) return data; // never overwrite edited content

            data = ScriptableObject.CreateInstance<StationData>();
            data.title = seed.title;
            data.kind = seed.kind;
            data.subtitle = seed.subtitle;
            data.body = seed.body;
            data.accent = seed.accent;
            foreach (var (label, url) in seed.links) data.links.Add(new StationLink { label = label, url = url });
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null) { Debug.LogError($"[Portfolio] {target.GetType().Name} has no field '{field}'"); continue; }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
