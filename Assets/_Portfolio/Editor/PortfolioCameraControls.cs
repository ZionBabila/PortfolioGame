using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// Adds Coin Master-style camera control to the open Workshop scene: drag to pan, pinch / wheel to zoom,
    /// and a "Find me" button that brings the view back to the player. Only adds what's missing.
    /// </summary>
    public static class PortfolioCameraControls
    {
        [MenuItem("Portfolio/Camera/Add Pan & Zoom (drag, pinch, Find me)")]
        public static void AddPanZoom()
        {
            var target = Object.FindAnyObjectByType<OverviewTarget>(FindObjectsInactive.Include);
            var overview = GameObject.Find("CM Overview");
            var player = Object.FindAnyObjectByType<ClickToMove>(FindObjectsInactive.Include);
            if (!target || !overview || !player)
            {
                EditorUtility.DisplayDialog("Pan & Zoom", "Open the Workshop scene first (needs Overview Target, CM Overview and the Player).", "OK");
                return;
            }

            var panZoom = target.GetComponent<CameraPanZoom>();
            if (!panZoom) panZoom = Undo.AddComponent<CameraPanZoom>(target.gameObject);
            var so = new SerializedObject(panZoom);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("viewCamera").objectReferenceValue = overview.GetComponent<Unity.Cinemachine.CinemachineCamera>();
            so.ApplyModifiedProperties();

            var lens = new SerializedObject(overview.GetComponent<AspectLens>());
            lens.FindProperty("userZoom").objectReferenceValue = panZoom;
            lens.ApplyModifiedProperties();

            AddFindMeButton(panZoom);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Debug.Log("[Portfolio] Pan & zoom added: drag to pan, pinch / mouse wheel to zoom, \"Find me\" to recenter. Save the scene.");
        }

        static void AddFindMeButton(CameraPanZoom panZoom)
        {
            if (GameObject.Find("FindMeButton")) return;
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(c => c.isRootCanvas);
            if (!canvas) return;

            // Bottom-left, just above the music toggle.
            var go = new GameObject("FindMeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Add Find me button");
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(24f, 108f);
            rt.sizeDelta = new Vector2(260f, 72f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.99f, 0.97f, 0.93f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            go.AddComponent<Outline>().effectColor = new Color(0.08f, 0.07f, 0.1f);
            UnityEventTools.AddPersistentListener(button.onClick, panZoom.Recenter);

            var labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(rt, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset) label.font = TMP_Settings.defaultFontAsset;
            label.text = "Find me";
            label.fontSize = 30;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.08f, 0.07f, 0.1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
    }
}
