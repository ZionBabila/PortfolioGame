using UnityEngine;
using UnityEngine.InputSystem;

namespace Portfolio
{
    /// <summary>Wires the player, stations and UI together.</summary>
    public class PortfolioGame : MonoBehaviour
    {
        [SerializeField] ClickToMove player;
        [SerializeField] StationPanel panel;
        [SerializeField] QuickNav quickNav;

        void Start()
        {
            var stations = FindObjectsByType<Station>(FindObjectsInactive.Exclude);
            System.Array.Sort(stations, (a, b) => string.CompareOrdinal(a.name, b.name));

            player.ArrivedAtStation += s => panel.Show(s.data);
            player.StartedMoving += panel.Hide;
            if (quickNav) quickNav.Build(stations, player);
            if (panel) HudSkin.Apply(panel.GetComponentInParent<Canvas>().rootCanvas.transform, UISkin.Default);

            // The fortune machine (HUD stamp + overlay) and the Zion you catch for its energy.
            if (panel && Slot.SlotMachineData.Default)
            {
                Slot.SlotMachineUI.Install(panel.GetComponentInParent<Canvas>().rootCanvas.transform);
                Slot.ZionRunner.Spawn(player);
                // Who's visiting: HR, Team Lead, Head of Department or just visiting (asked on the first visit).
                Slot.VisitorSelect.Install(panel.GetComponentInParent<Canvas>().rootCanvas.transform, player);
            }

            // Dana, the old HR recruiter NPC, is retired: the visitor can now *be* HR. Switched off here rather than
            // deleted from Workshop.unity, so the scene file stays untouched.
            foreach (var hr in FindObjectsByType<HRRecruiter>(FindObjectsInactive.Include)) hr.gameObject.SetActive(false);
            foreach (var dialog in FindObjectsByType<HRDialog>(FindObjectsInactive.Include)) dialog.gameObject.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (ProjectViewer.IsOpen)
                {
                    ProjectViewer.HideCurrent(); // Esc closes the project page first, then the station panel
                    return;
                }
                panel.Hide();
                if (quickNav) quickNav.Close();
            }
        }
    }
}
