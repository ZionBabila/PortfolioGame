using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Background music: import settings for Assets/_Portfolio/Audio, and a menu command that adds the
    /// music player + a mute button to the open scene (only if they aren't there yet; nothing else is touched).
    /// </summary>
    public class PortfolioAudio : AssetPostprocessor
    {
        const string AudioDir = "Assets/_Portfolio/Audio/";
        const string DefaultTrack = AudioDir + "HappyUkuleleIsland.mp3";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioDir)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f; // plenty for background music, keeps the web build small
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
        }

        [MenuItem("Portfolio/Audio/Add Music To Scene")]
        public static void AddMusicToScene()
        {
            if (Object.FindAnyObjectByType<MusicPlayer>(FindObjectsInactive.Include))
            {
                Debug.Log("[Portfolio] The scene already has a MusicPlayer.");
                return;
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultTrack);
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(c => c.isRootCanvas);

            var music = new GameObject("Music");
            Undo.RegisterCreatedObjectUndo(music, "Add Music");
            var source = music.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var player = music.AddComponent<MusicPlayer>();

            if (canvas)
            {
                // Bottom-left: clear of the station panel (right side in 16:9, bottom sheet in 9:16 sits above it).
                var go = new GameObject("MusicToggle", typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, "Add Music");
                var rt = (RectTransform)go.transform;
                rt.SetParent(canvas.transform, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(24f, 24f);
                rt.sizeDelta = new Vector2(260f, 72f);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.99f, 0.97f, 0.93f, 0.95f);
                go.GetComponent<Button>().targetGraphic = img;
                go.AddComponent<Outline>().effectColor = new Color(0.08f, 0.07f, 0.1f);

                var labelGo = new GameObject("Text", typeof(RectTransform));
                labelGo.transform.SetParent(rt, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
                label.text = "Music: On";
                label.fontSize = 30;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.08f, 0.07f, 0.1f);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;

                var so = new SerializedObject(player);
                so.FindProperty("toggleButton").objectReferenceValue = go.GetComponent<Button>();
                so.FindProperty("toggleLabel").objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(music.scene);
            Debug.Log($"[Portfolio] Added background music ({(clip ? clip.name : "no clip found")}) and a mute button. Save the scene.");
        }
    }
}
