using System.Collections.Generic;
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
                item.GetComponentInChildren<Text>().text = station.data ? station.data.title : station.name;
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
    }
}
