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
