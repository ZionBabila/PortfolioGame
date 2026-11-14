using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Slot
{
    /// <summary>
    /// The Zion Babila fortune machine. Pull the lever (1 energy) and three reels spin; three of a kind reveals a
    /// short sentence about Zion. Energy comes from catching Zion in the studio (<see cref="ZionRunner"/>).
    /// Also owns the HUD stamp that opens it and the "+energy" note. Built in code; the scene doesn't change.
    /// </summary>
    public class SlotMachineUI : MonoBehaviour
    {
        const float Step = 150f;                 // one icon step on a reel (UI px)
        static SlotMachineUI instance;
        public static bool IsOpen => instance && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        SlotMachineData data;
        UISkin skin;
        TMP_FontAsset typeFont, labelFont;
        Color ink = new Color32(43, 38, 34, 255), paper = new Color32(239, 233, 220, 255), accent = new Color32(226, 97, 31, 255), soft = new Color32(109, 100, 90, 255);

        bool open, spinning;
        int misses;
        CanvasGroup group;
        RectTransform machine, lever, reveal, factsCard;
        readonly Reel[] reels = new Reel[3];
        TMP_Text energyText, hintText, revealTitle, revealBody, factsText;
        Image revealIcon;

        // HUD
        Button hudButton;
        TMP_Text hudText, noteText;
        CanvasGroup noteGroup;
        Coroutine noteRoutine;

        class Reel { public RectTransform strip; public List<Image> cells = new(); public int shown; }

        // ---------- Install ----------

        /// <summary>Adds the HUD stamp and the (hidden) machine to the canvas. Safe to call more than once.</summary>
        public static void Install(Transform canvas)
        {
            var d = SlotMachineData.Default;
            if (!canvas || !d || d.symbols.Count < 2 || instance) return;
            var go = new GameObject("Slot Machine", typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance = go.AddComponent<SlotMachineUI>();
            instance.data = d;
            instance.Build();
        }

        public static void Open() { if (instance) instance.Show(); }

        void OnDestroy() { Energy.Changed -= RefreshAll; Energy.Gained -= OnGained; Visitor.Changed -= RefreshAll; }

        // ---------- Open / close ----------

        void Show()
        {
            if (open) return;
            ProjectViewer.HideCurrent();
            foreach (var nav in FindObjectsByType<QuickNav>(FindObjectsInactive.Exclude)) nav.Close();
            open = true;
            group.blocksRaycasts = true;
            reveal.gameObject.SetActive(false);
            Layout();
            RefreshAll();
            hintText.text = Energy.Amount >= data.spinCost ? "Pull the lever." : "Out of energy. Catch Zion in the studio!";
        }

        void Hide()
        {
            if (spinning) return;
            open = false;
            group.blocksRaycasts = false;
        }

        void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        }

        // ---------- Spin ----------

        void Pull()
        {
            if (spinning) return;
            if (!Energy.TrySpend(data.spinCost))
            {
                hintText.text = "Out of energy. Catch Zion in the studio!";
                StartCoroutine(Shake(machine));
                return;
            }
            StartCoroutine(Spin());
        }

        IEnumerator Spin()
        {
            spinning = true;
            reveal.gameObject.SetActive(false);
            hintText.text = "";
            var result = PickResult(out bool triple);
            StartCoroutine(LeverPull());
            for (int i = 0; i < 3; i++) StartCoroutine(SpinReel(reels[i], result[i], 0.7f + i * 0.35f, 10 + i * 4));
            yield return new WaitForSecondsRealtime(0.7f + 2 * 0.35f + 0.15f);

            if (triple)
            {
                misses = 0;
                var sym = data.symbols[result[0]];
                bool fresh = !Energy.IsRevealed(sym.id);
                Energy.Reveal(sym.id);
                revealIcon.sprite = sym.icon;
                revealTitle.text = fresh ? "THREE OF A KIND" : "AGAIN!";
                revealBody.text = "";
                reveal.gameObject.SetActive(true);
                StartCoroutine(Pop(reveal));
                var role = Visitor.Current; // the same icon tells HR, a team lead and a department head different things
                var text = sym.FactFor(role) + (Energy.RevealedCount >= data.symbols.Count && fresh ? "\n\n" + data.AllRevealedFor(role) : "");
                yield return Type(revealBody, text);
            }
            else
            {
                misses++;
                bool near = result[0] == result[1] || result[1] == result[2] || result[0] == result[2];
                hintText.text = near ? "So close. Pull again." : Energy.Amount >= data.spinCost ? "Pull again." : "Out of energy. Catch Zion in the studio!";
            }
            RefreshAll();
            spinning = false;
        }

        int[] PickResult(out bool triple)
        {
            int n = data.symbols.Count;
            triple = Random.value < data.tripleChance + data.pityPerMiss * misses;
            if (triple)
            {
                var fresh = Enumerable.Range(0, n).Where(i => !Energy.IsRevealed(data.symbols[i].id)).ToList();
                int s = fresh.Count > 0 && Random.value < data.preferNew ? fresh[Random.Range(0, fresh.Count)] : Random.Range(0, n);
                return new[] { s, s, s };
            }
            var r = new[] { Random.Range(0, n), Random.Range(0, n), Random.Range(0, n) };
            if (Random.value < 0.45f) r[1] = r[0];                       // the near miss that keeps you pulling
            while (r[0] == r[1] && r[1] == r[2]) r[2] = Random.Range(0, n);
            return r;
        }

        IEnumerator SpinReel(Reel reel, int target, float duration, int steps)
        {
            int n = data.symbols.Count;
            // Lay the strip out top-down: the current icon, a run of random ones, then the target at the bottom.
            var seq = new List<int> { reel.shown };
            for (int i = 0; i < steps; i++) seq.Add(Random.Range(0, n));
            seq.Add(target);
            while (reel.cells.Count < seq.Count) reel.cells.Add(Cell(reel.strip));
            for (int i = 0; i < reel.cells.Count; i++)
            {
                var c = reel.cells[i];
                c.gameObject.SetActive(i < seq.Count);
                if (i >= seq.Count) continue;
                c.sprite = data.symbols[seq[i]].icon;
                c.rectTransform.anchoredPosition = new Vector2(0, i * Step); // strip moves down, cells stack up
            }
            float distance = (seq.Count - 1) * Step;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = UIEase.OutBack(t / duration, 0.9f);
                reel.strip.anchoredPosition = new Vector2(0, -distance * k);
                yield return null;
            }
            reel.strip.anchoredPosition = new Vector2(0, -distance);
            // Settle: keep only the target, re-centred.
            reel.cells[0].sprite = data.symbols[target].icon;
            reel.cells[0].rectTransform.anchoredPosition = Vector2.zero;
            for (int i = 1; i < reel.cells.Count; i++) reel.cells[i].gameObject.SetActive(false);
            reel.strip.anchoredPosition = Vector2.zero;
            reel.shown = target;
        }

        IEnumerator LeverPull()
        {
            // Seen from the front, a lever pulled toward you foreshortens: squash it down, then spring back.
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.16f) { lever.localScale = new Vector3(1, Mathf.Lerp(1, 0.45f, t), 1); yield return null; }
            yield return new WaitForSecondsRealtime(0.08f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.4f) { lever.localScale = new Vector3(1, Mathf.LerpUnclamped(0.45f, 1f, UIEase.OutBack(t, 2.2f)), 1); yield return null; }
            lever.localScale = Vector3.one;
        }

        IEnumerator Type(TMP_Text t, string text)
        {
            t.text = text; t.maxVisibleCharacters = 0;
            for (int i = 0; i <= text.Length; i++) { t.maxVisibleCharacters = i; yield return new WaitForSecondsRealtime(0.022f); }
        }

        IEnumerator Pop(Transform t)
        {
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.3f) { t.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, UIEase.OutBack(k)); yield return null; }
            t.localScale = Vector3.one;
        }

        IEnumerator Shake(Transform t)
        {
            var start = t.localPosition;
            for (float k = 0; k < 0.3f; k += Time.unscaledDeltaTime) { t.localPosition = start + Vector3.right * Mathf.Sin(k * 70f) * 8f * (1 - k / 0.3f); yield return null; }
            t.localPosition = start;
        }

        // ---------- Text ----------

        void RefreshAll()
        {
            if (!energyText) return;
            energyText.text = $"ENERGY  <size=150%>{Energy.Amount}</size>";
            hudText.text = $"<size=125%>{Energy.Amount}</size>   Fortune machine";
            factsText.text = $"<b>SECRETS  {Energy.RevealedCount}/{data.symbols.Count}</b>\n\n" + string.Join("\n\n",
                data.symbols.Select(s => Energy.IsRevealed(s.id) ? s.FactFor(Visitor.Current) : "· · ·"));
        }

        void OnGained(int value)
        {
            noteText.text = $"You caught Zion!  <b>+{value} energy</b>";
            if (noteRoutine != null) StopCoroutine(noteRoutine);
            noteRoutine = StartCoroutine(Flash());
            if (open && !spinning) hintText.text = "Pull the lever.";
        }

        IEnumerator Flash()
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f) { noteGroup.alpha = t; yield return null; }
            noteGroup.alpha = 1;
            yield return new WaitForSecondsRealtime(2.5f);
            for (float t = 1; t > 0; t -= Time.unscaledDeltaTime / 0.4f) { noteGroup.alpha = t; yield return null; }
            noteGroup.alpha = 0;
        }

        // ---------- Build ----------

        void Build()
        {
            skin = UISkin.Default;
            if (skin && !skin.IsReady) skin = null;
            if (skin) { ink = skin.ink; paper = skin.paper; accent = skin.accent; soft = skin.inkSoft; }
            typeFont = skin && skin.typewriter ? skin.typewriter : TMP_Settings.defaultFontAsset;
            labelFont = skin && skin.label ? skin.label : typeFont;
            var root = (RectTransform)transform;

            BuildHud(root);

            // Overlay on its own sorting layer above the HUD.
            var overlay = UIObj("Overlay", root); Stretch(overlay);
            var layer = overlay.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 100;
            overlay.gameObject.AddComponent<GraphicRaycaster>();
            group = overlay.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            var dim = overlay.gameObject.AddComponent<Image>(); dim.color = new Color(ink.r, ink.g, ink.b, 0.88f);

            // The cabinet (500x700) with three reel windows, energy counter and lever.
            machine = UIObj("Machine", overlay);
            machine.sizeDelta = new Vector2(500, 700);
            Img(machine, data.body, Color.white);
            for (int i = 0; i < 3; i++)
            {
                var win = UIObj("Reel " + (i + 1), machine);
                win.anchorMin = win.anchorMax = new Vector2(0, 1); win.pivot = new Vector2(0.5f, 0.5f);
                win.sizeDelta = new Vector2(132, 185);
                win.anchoredPosition = new Vector2(108 + i * 135, -265);
                var face = UIObj("Face", win); Stretch(face, 11, 11, 11, 11);
                Img(face, null, new Color32(244, 238, 226, 255)).raycastTarget = false;
                face.gameObject.AddComponent<RectMask2D>();
                var strip = UIObj("Strip", face);
                strip.anchorMin = strip.anchorMax = strip.pivot = new Vector2(0.5f, 0.5f); strip.sizeDelta = Vector2.zero;
                var reel = new Reel { strip = strip, shown = i % data.symbols.Count };
                var first = Cell(strip); first.sprite = data.symbols[reel.shown].icon; reel.cells.Add(first);
                reels[i] = reel;
                var frame = UIObj("Frame", win); Stretch(frame);
                Img(frame, data.window, Color.white).raycastTarget = false;
            }

            energyText = Label("Energy", machine, 30, FontStyles.Bold, accent, TextAlignmentOptions.Center, typeFont);
            var et = energyText.rectTransform; et.anchorMin = et.anchorMax = new Vector2(0.5f, 1); et.sizeDelta = new Vector2(360, 60); et.anchoredPosition = new Vector2(-10, -470);
            var bolt = UIObj("Bolt", machine); bolt.anchorMin = bolt.anchorMax = new Vector2(0.5f, 1); bolt.sizeDelta = new Vector2(48, 48); bolt.anchoredPosition = new Vector2(-150, -470);
            Img(bolt, data.energyIcon, Color.white);

            hintText = Label("Hint", machine, 24, FontStyles.Italic, paper, TextAlignmentOptions.Center, typeFont);
            var ht = hintText.rectTransform; ht.anchorMin = new Vector2(0, 0); ht.anchorMax = new Vector2(1, 0); ht.sizeDelta = new Vector2(0, 60); ht.anchoredPosition = new Vector2(0, -50);

            // Lever: tap (or click) to pull.
            lever = UIObj("Lever", machine);
            lever.anchorMin = lever.anchorMax = new Vector2(1, 0.5f); lever.pivot = new Vector2(0.5f, 0.05f);
            lever.sizeDelta = new Vector2(80, 310); lever.anchoredPosition = new Vector2(48, -60);
            var leverImg = Img(lever, data.lever, Color.white);
            leverImg.preserveAspect = false;
            var lb = lever.gameObject.AddComponent<Button>(); lb.targetGraphic = leverImg; lb.transition = Selectable.Transition.None;
            lb.onClick.AddListener(Pull);
            // The whole reel bay is a big tap target too (mobile thumbs).
            var bay = UIObj("Tap Reels", machine); bay.anchorMin = bay.anchorMax = new Vector2(0, 1); bay.pivot = new Vector2(0, 1);
            bay.sizeDelta = new Vector2(420, 220); bay.anchoredPosition = new Vector2(40, -155);
            var bayImg = Img(bay, null, new Color(0, 0, 0, 0));
            var bb = bay.gameObject.AddComponent<Button>(); bb.targetGraphic = bayImg; bb.transition = Selectable.Transition.None; bb.onClick.AddListener(Pull);
            bay.SetAsLastSibling(); // on top of the reels, so a tap anywhere on them pulls

            // Secrets so far.
            factsCard = UIObj("Secrets", overlay);
            Img(factsCard, skin ? skin.Sliced("BG_Paper", 48) : null, skin ? Color.white : paper).type = Image.Type.Sliced;
            factsText = Label("Text", factsCard, 21, FontStyles.Normal, ink, TextAlignmentOptions.TopLeft, typeFont);
            Stretch(factsText.rectTransform, 30, 30, 26, 26);
            factsText.enableAutoSizing = true; factsText.fontSizeMin = 14; factsText.fontSizeMax = 21;

            // Reveal letter.
            reveal = UIObj("Reveal", overlay);
            reveal.anchorMin = reveal.anchorMax = new Vector2(0.5f, 0.5f); reveal.sizeDelta = new Vector2(620, 300);
            Img(reveal, skin ? skin.Sliced("BG_Paper", 48) : null, skin ? Color.white : paper).type = Image.Type.Sliced;
            var rs = reveal.gameObject.AddComponent<Shadow>(); rs.effectColor = new Color(0, 0, 0, 0.35f); rs.effectDistance = new Vector2(8, -12);
            var ri = UIObj("Icon", reveal); ri.anchorMin = ri.anchorMax = new Vector2(0, 0.5f); ri.sizeDelta = new Vector2(150, 150); ri.anchoredPosition = new Vector2(105, 0);
            revealIcon = Img(ri, null, Color.white);
            revealTitle = Label("Title", reveal, 22, FontStyles.Bold, accent, TextAlignmentOptions.TopLeft, labelFont);
            Stretch(revealTitle.rectTransform, 200, 30, 34, 220); revealTitle.characterSpacing = 8;
            revealBody = Label("Body", reveal, 30, FontStyles.Bold, ink, TextAlignmentOptions.TopLeft, typeFont);
            Stretch(revealBody.rectTransform, 200, 34, 80, 30);
            revealBody.enableAutoSizing = true; revealBody.fontSizeMin = 18; revealBody.fontSizeMax = 30;
            var rb = reveal.gameObject.AddComponent<Button>(); rb.transition = Selectable.Transition.None;
            rb.onClick.AddListener(() => { if (!spinning) reveal.gameObject.SetActive(false); });

            // Leave.
            var leave = UIObj("Leave", overlay);
            leave.anchorMin = leave.anchorMax = new Vector2(1, 1); leave.pivot = new Vector2(1, 1); leave.sizeDelta = new Vector2(160, 60); leave.anchoredPosition = new Vector2(-30, -24);
            var lt = Label("Text", leave, 24, FontStyles.Italic | FontStyles.Underline, paper, TextAlignmentOptions.Center, typeFont);
            Stretch(lt.rectTransform); lt.text = "Leave"; lt.raycastTarget = true;
            var leaveBtn = leave.gameObject.AddComponent<Button>(); leaveBtn.targetGraphic = lt; leaveBtn.onClick.AddListener(Hide);

            Energy.Changed += RefreshAll;
            Energy.Gained += OnGained;
            Visitor.Changed += RefreshAll;
            RefreshAll();
        }

        void Layout()
        {
            bool portrait = Aspect.IsPortrait;
            machine.anchorMin = machine.anchorMax = new Vector2(portrait ? 0.46f : 0.38f, portrait ? 0.6f : 0.52f);
            machine.anchoredPosition = Vector2.zero;
            machine.localScale = Vector3.one * (portrait ? 1.15f : 1.05f);
            if (portrait) { factsCard.anchorMin = new Vector2(0.06f, 0.02f); factsCard.anchorMax = new Vector2(0.94f, 0.22f); }
            else { factsCard.anchorMin = new Vector2(0.62f, 0.26f); factsCard.anchorMax = new Vector2(0.93f, 0.78f); }
            factsCard.offsetMin = factsCard.offsetMax = Vector2.zero;
        }

        void BuildHud(RectTransform root)
        {
            var b = new GameObject("Fortune Machine Button", typeof(RectTransform), typeof(Image), typeof(Button));
            b.layer = gameObject.layer;
            var brt = (RectTransform)b.transform; brt.SetParent(root, false);
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1, 0); brt.anchoredPosition = new Vector2(-24, 24); brt.sizeDelta = new Vector2(420, 80);
            hudButton = b.GetComponent<Button>(); hudButton.onClick.AddListener(Show);
            var icon = UIObj("Bolt", brt); icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f); icon.sizeDelta = new Vector2(44, 44); icon.anchoredPosition = new Vector2(44, 0);
            Img(icon, data.energyIcon, Color.white).raycastTarget = false;
            hudText = Label("Text", brt, 24, FontStyles.Bold, accent, TextAlignmentOptions.MidlineLeft, labelFont);
            Stretch(hudText.rectTransform, 74, 16, 0, 0);
            hudText.textWrappingMode = TextWrappingModes.NoWrap;
            if (skin) StampButton.Apply(b, skin, 3, paperBacking: true); else b.GetComponent<Image>().color = paper;
            icon.SetAsLastSibling(); hudText.transform.SetAsLastSibling();

            var n = UIObj("Energy Note", root);
            n.anchorMin = n.anchorMax = n.pivot = new Vector2(0.5f, 1); n.anchoredPosition = new Vector2(0, -24); n.sizeDelta = new Vector2(560, 80);
            Img(n, skin ? skin.Sliced("BG_Paper", 48) : null, skin ? Color.white : paper).type = Image.Type.Sliced;
            noteGroup = n.gameObject.AddComponent<CanvasGroup>(); noteGroup.alpha = 0; noteGroup.blocksRaycasts = false;
            noteText = Label("Text", n, 26, FontStyles.Normal, ink, TextAlignmentOptions.Center, typeFont);
            Stretch(noteText.rectTransform, 20, 20, 6, 6);
        }

        Image Cell(RectTransform strip)
        {
            var c = UIObj("Icon", strip);
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f); c.sizeDelta = new Vector2(104, 104);
            var img = c.gameObject.AddComponent<Image>(); img.preserveAspect = true; img.raycastTarget = false;
            return img;
        }

        static Image Img(RectTransform rt, Sprite sprite, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.preserveAspect = sprite && sprite.border == Vector4.zero;
            return img;
        }

        TextMeshProUGUI Label(string name, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions align, TMP_FontAsset face)
        {
            var t = UIObj(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (face) t.font = face;
            t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal; t.richText = true; t.raycastTarget = false;
            return t;
        }

        static RectTransform UIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }
    }
}
