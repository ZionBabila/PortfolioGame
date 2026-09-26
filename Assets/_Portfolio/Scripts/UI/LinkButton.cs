using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio
{
    public class LinkButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] Text label;
        string url;

        public void Bind(StationLink link)
        {
            url = link.url;
            if (label) label.text = link.label + "  ↗";
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (WebLinks.DeferToPointerUp && !string.IsNullOrEmpty(url)) WebLinks.ArmForPointerUp(url);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!WebLinks.DeferToPointerUp && !string.IsNullOrEmpty(url)) WebLinks.OpenNow(url);
        }
    }
}
