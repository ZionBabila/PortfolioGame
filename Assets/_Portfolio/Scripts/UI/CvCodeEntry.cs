using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// The "Get my CV" block in the About letter: a typed-paper code box and an Unlock stamp. A code from
    /// <see cref="CvAccess"/> reveals a download stamp for the CV it belongs to (tech art or industrial design); the visitor
    /// only ever sees "Download CV" and ZionBabila_CV.pdf.
    /// The download is an ordinary <see cref="LinkButton"/>, so the browser sees a real click and doesn't block it.
    /// </summary>
    public class CvCodeEntry : MonoBehaviour
    {
        const string RememberKey = "Portfolio.CvCode";

        CvAccess access;
        UISkin skin;
        TMP_InputField input;
        TMP_Text message;
        LinkButton download;
        string downloadUrl;

        public static GameObject Build(RectTransform parent, UISkin skin, TMP_FontAsset font, LinkButton linkTemplate, int stampIndex)
        {
            var access = CvAccess.Default;
            if (!parent || !access || !linkTemplate) return null;
            var root = UIObj("CV Code", parent);
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12; v.padding = new RectOffset(0, 0, 6, 6);
            v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var entry = root.gameObject.AddComponent<CvCodeEntry>();
            entry.access = access;
            entry.skin = skin;
            entry.Make(root, font, linkTemplate, stampIndex);
            return root.gameObject;
        }

        void Make(RectTransform root, TMP_FontAsset font, LinkButton linkTemplate, int stampIndex)
        {
            var ink = skin ? skin.ink : new Color(0.17f, 0.15f, 0.13f);
            var soft = skin ? skin.inkSoft : new Color(0.43f, 0.39f, 0.35f);
            var typeFont = skin && skin.typewriter ? skin.typewriter : font;

            var prompt = Text("Prompt", root, 24, FontStyles.Italic, soft, typeFont);
            prompt.text = access.prompt;

            // Code box + Unlock stamp on one row.
            var row = UIObj("Row", root);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;

            input = MakeInput(row, typeFont, ink, soft);
            input.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var unlock = Instantiate(linkTemplate, row);
            unlock.gameObject.SetActive(true);
            unlock.Bind(new StationLink { label = "Unlock", url = "" }); // no url: a plain button
            var unlockLabel = unlock.GetComponentInChildren<TMP_Text>(true);
            if (unlockLabel) unlockLabel.text = "Unlock";
            if (!unlock.TryGetComponent<LayoutElement>(out var ul)) ul = unlock.gameObject.AddComponent<LayoutElement>();
            ul.preferredWidth = 190; ul.minWidth = 160; ul.flexibleWidth = 0;
            if (!unlock.TryGetComponent<Button>(out var ub)) ub = unlock.gameObject.AddComponent<Button>();
            ub.onClick.AddListener(Submit);
            if (skin) StampButton.Apply(unlock.gameObject, skin, stampIndex);

            message = Text("Message", root, 22, FontStyles.Italic, soft, typeFont);
            message.text = "";

            download = Instantiate(linkTemplate, root);
            download.gameObject.SetActive(false);
            if (skin) StampButton.Apply(download.gameObject, skin, stampIndex + 1);

            input.onValueChanged.AddListener(_ => Check(false));
            input.onSubmit.AddListener(_ => Submit());

            // A visitor who already unlocked keeps their CV on the next visit.
            var remembered = PlayerPrefs.GetString(RememberKey, "");
            if (access.Find(remembered) != null) input.SetTextWithoutNotify(remembered);
            Check(false);
        }

        void Submit() => Check(true);

        /// <summary>Live while typing; on Unlock / Enter a wrong code also says so.</summary>
        void Check(bool submitted)
        {
            var found = access.Find(input.text);
            if (found != null)
            {
                if (found.url != downloadUrl)
                {
                    downloadUrl = found.url;
                    download.Bind(new StationLink { label = string.IsNullOrEmpty(found.label) ? "Download CV" : found.label, url = found.url });
                }
                bool fresh = !download.gameObject.activeSelf;
                download.gameObject.SetActive(true);
                message.text = "Unlocked. Here's my CV:";
                message.color = skin ? skin.accent : message.color;
                PlayerPrefs.SetString(RememberKey, input.text);
                PlayerPrefs.Save();
                if (fresh) { StartCoroutine(Pop(download.transform)); StartCoroutine(Reveal()); }
                return;
            }

            download.gameObject.SetActive(false);
            downloadUrl = null;
            message.color = skin ? skin.inkSoft : message.color;
            if (submitted)
            {
                message.text = string.IsNullOrWhiteSpace(input.text) ? "Type the code I sent you." : access.wrongCode;
                StartCoroutine(Shake(input.transform));
                StartCoroutine(Reveal());
            }
            else message.text = "";
        }

        TMP_InputField MakeInput(Transform parent, TMP_FontAsset font, Color ink, Color soft)
        {
            var go = new GameObject("Code Box", typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.SetActive(false); // wire everything before TMP_InputField enables
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            if (skin) { bg.sprite = skin.Sliced("BG_Paper", 48); bg.type = Image.Type.Sliced; bg.color = skin.card; }
            else bg.color = new Color(0.97f, 0.96f, 0.92f);
            var edge = go.AddComponent<Outline>();
            edge.effectColor = new Color(ink.r, ink.g, ink.b, 0.55f); edge.effectDistance = new Vector2(2, -2);

            var area = UIObj("Text Area", rt); Stretch(area, 22, 22, 8, 8);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text("Placeholder", area, 28, FontStyles.Italic, new Color(soft.r, soft.g, soft.b, 0.7f), font);
            Stretch(placeholder.rectTransform);
            placeholder.text = "Your code";
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            var text = Text("Text", area, 30, FontStyles.Bold, ink, font);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var field = go.AddComponent<TMP_InputField>();
            field.targetGraphic = bg;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = font;
            field.pointSize = 30;
            field.characterLimit = 24;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterValidation = TMP_InputField.CharacterValidation.None; // allow dashes in codes
            field.contentType = TMP_InputField.ContentType.Custom;
            field.inputType = TMP_InputField.InputType.Standard;
            field.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            field.caretColor = skin ? skin.accent : ink;
            field.customCaretColor = true;
            field.caretWidth = 3;
            field.selectionColor = skin ? new Color(skin.accent.r, skin.accent.g, skin.accent.b, 0.3f) : new Color(1, 0.6f, 0.3f, 0.3f);
            go.SetActive(true);
            return field;
        }

        /// <summary>The letter scrolls; bring the new message and download stamp into view.</summary>
        IEnumerator Reveal()
        {
            yield return null;
            var scroll = GetComponentInParent<ScrollRect>();
            if (!scroll || !scroll.content) yield break;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                scroll.verticalNormalizedPosition = Mathf.Lerp(scroll.verticalNormalizedPosition, 0f, UIEase.OutCubic(t));
                yield return null;
            }
            scroll.verticalNormalizedPosition = 0f;
        }

        IEnumerator Pop(Transform t)
        {
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.3f) { t.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, UIEase.OutBack(k)); yield return null; }
            t.localScale = Vector3.one;
        }

        IEnumerator Shake(Transform t)
        {
            var start = t.localPosition;
            for (float k = 0; k < 0.3f; k += Time.unscaledDeltaTime) { t.localPosition = start + Vector3.right * Mathf.Sin(k * 70f) * 8f * (1 - k / 0.3f); yield return null; }
            t.localPosition = start;
        }

        static TextMeshProUGUI Text(string n, Transform parent, float size, FontStyles style, Color color, TMP_FontAsset font)
        {
            var t = UIObj(n, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false;
            return t;
        }

        static RectTransform UIObj(string n, Transform parent)
        {
            var go = new GameObject(n, typeof(RectTransform)); go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false); return rt;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }
    }
}
