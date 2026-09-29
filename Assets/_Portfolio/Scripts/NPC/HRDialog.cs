using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// The HR recruiter's question card: a question, answer buttons, then the recruiter's reply and a goodbye button.
    /// Sits at the bottom of the screen (centered in 16:9, full width in 9:16). A dimmed blocker behind it stops
    /// clicks from walking or dragging the map while it's open.
    /// </summary>
    public class HRDialog : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform card;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text greetingLabel;
        [SerializeField] TMP_Text questionLabel;
        [SerializeField] TMP_Text replyLabel;
        [SerializeField] RectTransform answersRoot;
        [SerializeField] Button answerTemplate;
        [SerializeField] Button goodbyeButton;
        [SerializeField] Button runAwayButton;

        [Header("Layout")]
        [SerializeField, Range(0.3f, 0.9f)] float landscapeWidth = 0.52f;
        [SerializeField] float margin = 24f;
        [SerializeField] float animSpeed = 8f;
        [Tooltip("Closes by itself this many seconds after the reply (0 = wait for the button).")]
        [SerializeField] float autoCloseAfter = 0f;

        public static bool IsOpen { get; private set; }

        readonly List<Button> spawned = new();
        HRQuestion current;
        Action onClosed;
        bool open;
        float shown;
        float closeAt = -1f;

        void Awake()
        {
            answerTemplate.gameObject.SetActive(false);
            goodbyeButton.onClick.AddListener(Close);
            runAwayButton.onClick.AddListener(Close);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            IsOpen = false;
        }

        public void Show(HRQuestionSet set, HRQuestion question, string greeting, Action closed)
        {
            current = question;
            onClosed = closed;
            SetText(speakerLabel, set.speakerName);
            SetText(greetingLabel, greeting);
            greetingLabel.gameObject.SetActive(!string.IsNullOrEmpty(greeting));
            SetText(questionLabel, question.question);
            replyLabel.gameObject.SetActive(false);
            goodbyeButton.gameObject.SetActive(false);
            SetButtonText(goodbyeButton, set.goodbyes.Count > 0 ? set.goodbyes[UnityEngine.Random.Range(0, set.goodbyes.Count)] : "Bye!");
            SetButtonText(runAwayButton, set.runAwayLabel);
            runAwayButton.gameObject.SetActive(true);

            foreach (var b in spawned) Destroy(b.gameObject);
            spawned.Clear();
            for (int i = 0; i < question.answers.Count; i++)
            {
                int index = i;
                var b = Instantiate(answerTemplate, answersRoot);
                b.gameObject.SetActive(true);
                SetButtonText(b, question.answers[i].text);
                b.onClick.AddListener(() => Answer(index));
                spawned.Add(b);
            }
            answersRoot.gameObject.SetActive(true);

            open = true;
            IsOpen = true;
            closeAt = -1f;
            group.blocksRaycasts = true;
        }

        void Answer(int index)
        {
            answersRoot.gameObject.SetActive(false);
            runAwayButton.gameObject.SetActive(false);
            SetText(replyLabel, current.answers[index].reply);
            replyLabel.gameObject.SetActive(true);
            goodbyeButton.gameObject.SetActive(true);
            if (autoCloseAfter > 0f) closeAt = Time.unscaledTime + autoCloseAfter;
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            IsOpen = false;
            group.blocksRaycasts = false;
            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        void Update()
        {
            if (closeAt > 0f && Time.unscaledTime >= closeAt) { closeAt = -1f; Close(); }

            shown = Mathf.MoveTowards(shown, open ? 1f : 0f, Time.unscaledDeltaTime * animSpeed);
            float eased = 1f - Mathf.Pow(1f - shown, 3f);
            group.alpha = eased;

            float half = Aspect.IsPortrait ? 0.5f : landscapeWidth * 0.5f;
            card.anchorMin = new Vector2(0.5f - half, 0f);
            card.anchorMax = new Vector2(0.5f + half, 0f);
            card.offsetMin = new Vector2(Aspect.IsPortrait ? margin * 0.5f : 0f, card.offsetMin.y);
            card.offsetMax = new Vector2(Aspect.IsPortrait ? -margin * 0.5f : 0f, card.offsetMax.y);
            card.anchoredPosition = new Vector2(card.anchoredPosition.x, margin - (1f - eased) * 160f);
        }

        static void SetText(TMP_Text t, string value)
        {
            if (t) t.text = value;
        }

        static void SetButtonText(Button b, string value)
        {
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t) t.text = value;
        }
    }
}
