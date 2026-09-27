using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// A "stations" menu for visitors who'd rather not walk: lists every station and sends the player there.
    /// </summary>
    public class QuickNav : MonoBehaviour
    {
        [SerializeField] Button toggle;
        [SerializeField] GameObject list;
        [SerializeField] Button itemTemplate;

        readonly List<Button> items = new();

        public void Build(IReadOnlyList<Station> stations, ClickToMove player)
        {
            itemTemplate.gameObject.SetActive(false);
            list.SetActive(false);
            toggle.onClick.AddListener(() => list.SetActive(!list.activeSelf));

            foreach (var station in stations)
            {
                var item = Instantiate(itemTemplate, itemTemplate.transform.parent);
                item.gameObject.SetActive(true);
                SetLabel(item, station.data ? station.data.title : station.name);
                var s = station;
                item.onClick.AddListener(() =>
                {
                    list.SetActive(false);
                    player.GoTo(s);
                });
                items.Add(item);
            }
        }

        public void Close() => list.SetActive(false);

        // Works before and after Portfolio → Convert Scene Text To TextMeshPro.
        static void SetLabel(Component item, string text)
        {
            var tmp = item.GetComponentInChildren<TMP_Text>(true);
            if (tmp) { tmp.text = text; return; }
            var legacy = item.GetComponentInChildren<Text>(true);
            if (legacy) legacy.text = text;
        }
    }
}
