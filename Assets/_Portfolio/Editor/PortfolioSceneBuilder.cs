using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Shared pieces for building the Workshop scene: station content seeds, player, camera, lighting and UI.
    /// The Workshop-specific parts live in WorkshopSceneBuilder / WorkshopCameraRig.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const string Root = "Assets/_Portfolio";
        const string StationsDir = Root + "/Content/Stations";
        const string MaterialsDir = Root + "/Materials";

        static readonly Color Ink = new(0.08f, 0.07f, 0.1f);

        struct StationSeed
        {
            public string file, title, subtitle, body;
            public StationKind kind;
            public Color accent;
            public (string label, string url)[] links;
        }

        static readonly StationSeed[] Seeds =
        {
            new()
            {
                file = "01_About", kind = StationKind.About, title = "About Me", subtitle = "Game developer",
                body = "Short intro: who you are, what you build, what you're looking for.\n\nEdit this in Assets/_Portfolio/Content/Stations/01_About.asset",
                accent = new Color(0.98f, 0.62f, 0.32f),
                links = new[] { ("Download CV", "https://example.com/cv.pdf") },
            },
            new()
            {
                file = "02_Games", kind = StationKind.Game, title = "Playable Games", subtitle = "Play in the browser",
                body = "WebGL builds of games I made. Each link opens the game in a new tab.\n\nPut builds in WebGames/<name>/ and link them as games/<name>/",
                accent = new Color(0.45f, 0.78f, 0.45f),
                links = new[] { ("Game One", "games/game-one/"), ("Game Two", "games/game-two/") },
            },
            new()
            {
                file = "03_Projects", kind = StationKind.Project, title = "Projects", subtitle = "Selected work",
                body = "Highlights of projects: what it is, your role, tech used, result.",
                accent = new Color(0.42f, 0.62f, 0.95f),
                links = new[] { ("Project repo", "https://github.com/ZionBabila") },
            },
            new()
            {
                file = "04_Career", kind = StationKind.Career, title = "Career Path", subtitle = "Experience & education",
                body = "2024 - now   Role @ Company\n2022 - 2024  Role @ Company\n2019 - 2022  Studies @ School",
                accent = new Color(0.78f, 0.5f, 0.9f),
                links = new[] { ("LinkedIn", "https://www.linkedin.com/") },
            },
            new()
            {
                file = "05_Links", kind = StationKind.Links, title = "Contact", subtitle = "Let's talk",
                body = "Where to find me.",
                accent = new Color(0.95f, 0.8f, 0.3f),
                links = new[] { ("GitHub", "https://github.com/ZionBabila"), ("Email", "mailto:you@example.com") },
            },
        };

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

        static GameObject BuildPlayer(System.Collections.Generic.Dictionary<string, Material> mats)
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

        static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.9f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            go.AddComponent<AudioListener>();
            // Cinemachine drives this camera; the CinemachineCameras decide where it looks.
            var brain = go.AddComponent<Unity.Cinemachine.CinemachineBrain>();
            brain.DefaultBlend = new Unity.Cinemachine.CinemachineBlendDefinition(
                Unity.Cinemachine.CinemachineBlendDefinition.Styles.EaseInOut, 1.1f);
            return cam;
        }

        // ---------- UI (TextMeshPro) ----------

        /// <summary>
        /// All text is TextMeshPro, created with the project's default TMP font
        /// (Project Settings → TextMesh Pro → Settings → Default Font Asset). Change fonts per object in the Inspector.
        /// </summary>
        static GameObject BuildUI(out StationPanel panel, out QuickNav nav)
        {
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

            var close = Button("Close", sheet, "×", 56, Ink, new Color(0, 0, 0, 0));
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

            var kind = Label("Kind", content, 26, FontStyles.Bold, Ink);
            var title = Label("Title", content, 58, FontStyles.Bold, Ink);
            var subtitle = Label("Subtitle", content, 32, FontStyles.Italic, new Color(0.35f, 0.33f, 0.38f));
            var pic = UIObj("Picture", content);
            var picImg = pic.gameObject.AddComponent<Image>();
            pic.gameObject.AddComponent<LayoutElement>().preferredHeight = 320;
            var body = Label("Body", content, 32, FontStyles.Normal, Ink);
            body.lineSpacing = 10f; // TMP line spacing is in em/100

            var links = UIObj("Links", content);
            var lv = links.gameObject.AddComponent<VerticalLayoutGroup>();
            lv.spacing = 12; lv.childControlHeight = true; lv.childControlWidth = true; lv.childForceExpandHeight = false;
            var linkTpl = Button("LinkTemplate", links, "Link", 32, Color.white, Color.gray);
            linkTpl.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
            var linkBtn = linkTpl.gameObject.AddComponent<LinkButton>();
            SetRefs(linkBtn, ("label", linkTpl.GetComponentInChildren<TMP_Text>()));

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

            var toggle = Button("Toggle", navRoot, "Stations", 34, Ink, new Color(0.99f, 0.97f, 0.93f));
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
            var item = Button("ItemTemplate", list, "Station", 30, Ink, new Color(0.93f, 0.88f, 0.8f));
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;

            SetRefs(nav, ("toggle", toggle.GetComponent<Button>()), ("list", list.gameObject), ("itemTemplate", item.GetComponent<Button>()));

            // --- Hint ---
            var hint = Label("Hint", canvas, 28, FontStyles.Normal, new Color(0.1f, 0.1f, 0.12f, 0.8f));
            var hr = hint.rectTransform;
            hr.anchorMin = new Vector2(0, 0); hr.anchorMax = new Vector2(1, 0); hr.pivot = new Vector2(0.5f, 0);
            hr.anchoredPosition = new Vector2(0, 20); hr.sizeDelta = new Vector2(0, 50);
            hint.alignment = TextAlignmentOptions.Center;
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

        static TextMeshProUGUI Label(string name, Transform parent, float size, FontStyles style, Color color)
        {
            var t = UIObj(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset) t.font = TMP_Settings.defaultFontAsset;
            t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            return t;
        }

        static RectTransform Button(string name, Transform parent, string text, float size, Color textColor, Color bg)
        {
            var rt = UIObj(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            rt.gameObject.AddComponent<Button>().targetGraphic = img;
            var label = Label("Text", rt, size, FontStyles.Bold, textColor);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
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
