using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio
{
    public class LinkButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] TMP_Text label;
        string url;

        public void Bind(StationLink link)
        {
            url = link.url;
            if (label) label.text = link.label + "  →"; // → is in the default TMP font; ↗ isn't
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
