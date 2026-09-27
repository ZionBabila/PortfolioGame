using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// Looping background music with a mute toggle.
    /// Browsers block audio until the visitor interacts with the page, so the music starts (with a fade-in)
    /// on the first click or tap. The mute choice is remembered between visits.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        const string MutedKey = "portfolio.music.muted";

        [SerializeField, Range(0f, 1f)] float volume = 0.35f;
        [SerializeField] float fadeSeconds = 2f;
        [SerializeField] Button toggleButton;
        [SerializeField] TMP_Text toggleLabel;
        [SerializeField] string onText = "Music: On";
        [SerializeField] string offText = "Music: Off";

        AudioSource source;
        bool muted;
        bool started;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            if (toggleButton) toggleButton.onClick.AddListener(Toggle);
            RefreshLabel();
        }

        void Update()
        {
            // First interaction unlocks audio in the browser.
            if (!started)
            {
                var pointer = Pointer.current;
                var keyboard = Keyboard.current;
                bool interacted = (pointer != null && pointer.press.wasPressedThisFrame) ||
                                  (keyboard != null && keyboard.anyKey.wasPressedThisFrame);
                if (!interacted) return;
                started = true;
                source.Play();
            }

            float target = muted ? 0f : volume;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * volume / Mathf.Max(fadeSeconds, 0.01f));
        }

        public void Toggle()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (toggleLabel) toggleLabel.text = muted ? offText : onText;
        }
    }
}
