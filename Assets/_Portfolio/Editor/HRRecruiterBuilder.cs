using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Portfolio → NPC → Add HR Recruiter: adds the recruiter NPC, its question card (UI) and the questions asset
    /// (Content/HR/HRQuestions.asset — edit the text there). Only adds what's missing; re-running just re-wires.
    /// </summary>
    public static partial class PortfolioSceneBuilder
    {
        const string HRQuestionsPath = Root + "/Content/HR/HRQuestions.asset";

        [MenuItem("Portfolio/NPC/Add HR Recruiter")]
        public static void AddHRRecruiter()
        {
            var player = Object.FindAnyObjectByType<ClickToMove>(FindObjectsInactive.Include);
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);
            if (!player || !canvas)
            {
                EditorUtility.DisplayDialog("HR Recruiter", "Open the Workshop scene first (needs the Player and the UI canvas).", "OK");
                return;
            }

            var questions = LoadOrCreateHRQuestions();
            var dialog = Object.FindAnyObjectByType<HRDialog>(FindObjectsInactive.Include);
            if (!dialog) dialog = BuildHRDialog(canvas.transform);

            var npc = Object.FindAnyObjectByType<HRRecruiter>(FindObjectsInactive.Include);
            Transform body; TMP_Text label;
            if (npc)
            {
                body = npc.transform.Find("Body");
                label = npc.GetComponentInChildren<TMP_Text>(true);
            }
            else npc = BuildHRRecruiter(out body, out label);

            SetRefs(npc, ("player", player), ("dialog", dialog), ("questions", questions),
                ("stationPanel", Object.FindAnyObjectByType<StationPanel>(FindObjectsInactive.Include)),
                ("body", body), ("label", label));

            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Selection.activeObject = npc.gameObject;
            Debug.Log("[Portfolio] HR recruiter added. Edit the questions in " + HRQuestionsPath + ", tune the chase on the NPC. Save the scene.");
        }

        static HRRecruiter BuildHRRecruiter(out Transform body, out TMP_Text label)
        {
            var blazer = ToonMat("NPC_Blazer", new Color(0.22f, 0.28f, 0.48f), 0.05f);
            var shirt = ToonMat("NPC_Shirt", new Color(0.97f, 0.96f, 0.93f), 0.03f);
            var tie = ToonMat("NPC_Tie", new Color(0.86f, 0.22f, 0.16f), 0.02f);
            var glasses = ToonMat("NPC_Glasses", new Color(0.1f, 0.1f, 0.12f), 0f);
            var board = ToonMat("NPC_Clipboard", new Color(0.62f, 0.4f, 0.2f), 0.02f);

            var root = new GameObject("HR Recruiter");
            Undo.RegisterCreatedObjectUndo(root, "Add HR Recruiter");
            // Start on the far side of the room from the entrance, next to the side table.
            var spawn = new Vector3(6.2f, 0f, -2.2f);
            if (NavMesh.SamplePosition(spawn, out var hit, 3f, NavMesh.AllAreas)) spawn = hit.position;
            root.transform.SetPositionAndRotation(spawn, Quaternion.Euler(0f, 200f, 0f));

            body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0, 0.6f, 0), new Vector3(0.62f, 0.6f, 0.62f), blazer);
            Part(PrimitiveType.Cube, "Shirt", body, new Vector3(0, 0.78f, 0.27f), new Vector3(0.2f, 0.26f, 0.08f), shirt);
            Part(PrimitiveType.Cube, "Tie", body, new Vector3(0, 0.74f, 0.32f), new Vector3(0.07f, 0.22f, 0.03f), tie);
            Part(PrimitiveType.Cube, "Lens L", body, new Vector3(-0.09f, 1.0f, 0.29f), new Vector3(0.11f, 0.07f, 0.04f), glasses);
            Part(PrimitiveType.Cube, "Lens R", body, new Vector3(0.09f, 1.0f, 0.29f), new Vector3(0.11f, 0.07f, 0.04f), glasses);
            Part(PrimitiveType.Cube, "Bridge", body, new Vector3(0, 1.01f, 0.3f), new Vector3(0.08f, 0.015f, 0.02f), glasses);
            var clip = new GameObject("Clipboard").transform;
            clip.SetParent(body, false);
            clip.localPosition = new Vector3(0.35f, 0.62f, 0.1f);
            clip.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Part(PrimitiveType.Cube, "Board", clip, Vector3.zero, new Vector3(0.04f, 0.32f, 0.24f), board);
            Part(PrimitiveType.Cube, "Paper", clip, new Vector3(0.025f, -0.01f, 0f), new Vector3(0.01f, 0.26f, 0.19f), shirt);
            Part(PrimitiveType.Cube, "Clip", clip, new Vector3(0.02f, 0.15f, 0f), new Vector3(0.05f, 0.04f, 0.09f), glasses);

            var tmp = new GameObject("Label").AddComponent<TextMeshPro>();
            tmp.transform.SetParent(root.transform, false);
            tmp.transform.localPosition = new Vector3(0, 1.75f, 0);
            if (TMP_Settings.defaultFontAsset) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
            tmp.fontSize = 4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = new Color(0.86f, 0.22f, 0.16f);
            tmp.text = "HR";
            label = tmp;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 1.3f;
            agent.angularSpeed = 540f;
            agent.acceleration = 12f;
            agent.radius = 0.32f;
            agent.height = 1.2f;
            agent.stoppingDistance = 0.1f;
            agent.avoidancePriority = 60;
            return root.AddComponent<HRRecruiter>();
        }

        static void Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = Prim(type, name, parent, pos, scale, mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static HRDialog BuildHRDialog(Transform canvas)
        {
            var cream = new Color(0.99f, 0.97f, 0.93f, 0.98f);
            var tieRed = new Color(0.86f, 0.22f, 0.16f);

            var root = UIObj("HRDialog", canvas);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Add HR dialog");
            root.SetAsLastSibling(); // above every other button, so the blocker covers them
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var dialog = root.gameObject.AddComponent<HRDialog>();

            var blocker = UIObj("Blocker", root);
            Stretch(blocker);
            blocker.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 0.28f);

            var card = UIObj("Card", root);
            card.anchorMin = new Vector2(0.24f, 0f); card.anchorMax = new Vector2(0.76f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(0f, 24f);
            card.gameObject.AddComponent<Image>().color = cream;
            card.gameObject.AddComponent<Outline>().effectColor = Ink;
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 28, 30);
            layout.spacing = 14;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var speaker = Label("Speaker", card, 26, FontStyles.Bold, tieRed);
            var greeting = Label("Greeting", card, 28, FontStyles.Italic, new Color(0.35f, 0.33f, 0.38f));
            var question = Label("Question", card, 40, FontStyles.Bold, Ink);

            var answers = UIObj("Answers", card);
            var al = answers.gameObject.AddComponent<VerticalLayoutGroup>();
            al.spacing = 10;
            al.childControlWidth = al.childControlHeight = true;
            al.childForceExpandWidth = true; al.childForceExpandHeight = false;
            var template = Button("AnswerTemplate", answers, "Answer", 30, Ink, new Color(0.93f, 0.88f, 0.8f));
            var tl = template.gameObject.AddComponent<HorizontalLayoutGroup>(); // grows with two-line answers
            tl.padding = new RectOffset(22, 22, 14, 14);
            tl.childControlWidth = tl.childControlHeight = true;
            tl.childForceExpandWidth = true; tl.childForceExpandHeight = true;
            template.gameObject.AddComponent<LayoutElement>().minHeight = 76;

            var reply = Label("Reply", card, 34, FontStyles.Italic, new Color(0.16f, 0.22f, 0.42f));

            var row = UIObj("Buttons", card);
            var rl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rl.spacing = 12;
            rl.childAlignment = TextAnchor.MiddleRight;
            rl.childControlWidth = rl.childControlHeight = true;
            rl.childForceExpandWidth = rl.childForceExpandHeight = false;
            var runAway = Button("RunAway", row, "Run away!", 28, new Color(0.35f, 0.33f, 0.38f), new Color(0, 0, 0, 0));
            var re = runAway.gameObject.AddComponent<LayoutElement>();
            re.preferredWidth = 220; re.preferredHeight = 64;
            var bye = Button("Goodbye", row, "Bye!", 30, Color.white, tieRed);
            var be = bye.gameObject.AddComponent<LayoutElement>();
            be.preferredWidth = 300; be.preferredHeight = 72;

            SetRefs(dialog, ("group", group), ("card", card), ("speakerLabel", speaker), ("greetingLabel", greeting),
                ("questionLabel", question), ("replyLabel", reply), ("answersRoot", answers),
                ("answerTemplate", template.GetComponent<Button>()), ("goodbyeButton", bye.GetComponent<Button>()),
                ("runAwayButton", runAway.GetComponent<Button>()));
            return dialog;
        }

        static HRQuestionSet LoadOrCreateHRQuestions()
        {
            var set = AssetDatabase.LoadAssetAtPath<HRQuestionSet>(HRQuestionsPath);
            if (set) return set; // never overwrite edited content

            EnsureFolder(System.IO.Path.GetDirectoryName(HRQuestionsPath).Replace('\\', '/'));
            set = ScriptableObject.CreateInstance<HRQuestionSet>();
            set.intros.AddRange(new[]
            {
                "Hi! I'm Dana from HR. Got a minute? Great, it wasn't really a question.",
                "There you are! I've been looking all over the workshop for you.",
            });
            set.greetings.AddRange(new[]
            {
                "Oh, hi again! Quick follow-up:",
                "Found you! Just one more tiny thing:",
                "You walk fast. Anyway:",
                "Don't mind me, just updating your file:",
                "Sorry, sorry, last one. Probably.",
            });
            set.goodbyes.AddRange(new[] { "Great chat!", "Can I go now?", "Thanks, Dana!", "Noted. Bye!" });

            void Q(string q, params (string a, string r)[] answers)
            {
                var item = new HRQuestion { question = q };
                foreach (var (a, r) in answers) item.answers.Add(new HRAnswer { text = a, reply = r });
                set.questions.Add(item);
            }

            Q("Where do you see yourself in five years?",
                ("Right here. This loft has great light.", "Stability. Writing down: \"emotionally attached to windows\"."),
                ("Behind you. Always one step behind you.", "...That's MY line. Let's move on."),
                ("Still waiting for that render to finish.", "Relatable. Our payroll system renders too."));
            Q("What's your greatest weakness?",
                ("I can't stop fixing other people's fillets.", "So... perfectionism. Classic."),
                ("Bad kerning. I see it everywhere.", "Please don't look at our logo."),
                ("Snacks near the 3D printers.", "We have a strict no-crumbs-in-the-AMS policy."));
            Q("Tell me about a time you failed.",
                ("My first 3D print. It turned into spaghetti.", "Growth mindset! And carbs."),
                ("I once worked six hours without saving.", "Ctrl+S. It's on page one of the handbook."),
                ("I'll let you know. Any minute now.", "Confidence! Slightly worrying, but I like it."));
            Q("Why should we hire you?",
                ("I design it, prototype it, and ship it.", "\"End to end.\" HR loves that phrase."),
                ("I built a whole game just to show you my CV.", "Fair. Most people just send a PDF."),
                ("I bring my own screwdrivers.", "Saves on the budget. Noted."));
            Q("Are you a team player?",
                ("Yes. I also get along great with the printers.", "The printers gave you a glowing reference."),
                ("Absolutely. I even share my calipers.", "That's basically a love language here."),
                ("I'm talking to HR voluntarily, aren't I?", "You were chased. But I appreciate the spirit."));
            Q("How do you handle pressure?",
                ("With a hydraulic press, ideally.", "Engineers. Every single time."),
                ("Deep breath. Then a sketch.", "Healthy! Adding \"sketches under stress\"."),
                ("Same as deadlines: coffee.", "Our coffee machine is ready for you."));
            Q("What are your salary expectations?",
                ("Enough for unlimited filament.", "PLA or PETG? This affects the budget."),
                ("Let's talk after you see the portfolio.", "Smart. Very smart. Suspiciously smart."),
                ("Can I be paid in walnut veneer?", "I'll ask accounting. They'll say no."));
            Q("Describe yourself in three words.",
                ("Curious. Hands-on. Caffeinated.", "That's four if you count the hyphen."),
                ("Designer who codes.", "Also four... never mind, nobody counts anymore."),
                ("Please hire me.", "Direct! I respect that."));
            Q("Do you have any questions for us?",
                ("Is there a dental plan for bent drill bits?", "Only for the senior bits."),
                ("Why are you chasing me?", "Talent acquisition. Emphasis on \"acquisition\"."),
                ("Can I get the desk by the windows?", "Everyone asks. The windows are very popular."));
            Q("How do you keep your skills up to date?",
                ("Tutorials at 2x speed.", "And comprehension at 0.5x? Just checking."),
                ("I take things apart to see how they work.", "Please don't take the CNC apart. It's rented."),
                ("I follow the smell of new tools.", "That's the laser cutter. Stay back."));
            Q("What motivates you?",
                ("The moment a prototype actually works.", "Beautiful. I'll print that on a mug."),
                ("Solving problems nobody noticed yet.", "Could you notice ours? It's the Wi-Fi."),
                ("Currently? Getting away from HR.", "Motivation is motivation."));
            Q("Can you work under tight deadlines?",
                ("Deadlines are where the best prototypes come from.", "Inspiring. Also slightly alarming."),
                ("Deadlines are suggestions with a clock.", "Please never say that to a producer."),
                ("Only if the printer cooperates.", "It never does. Welcome to the team."));

            AssetDatabase.CreateAsset(set, HRQuestionsPath);
            AssetDatabase.SaveAssets();
            return set;
        }
    }
}
