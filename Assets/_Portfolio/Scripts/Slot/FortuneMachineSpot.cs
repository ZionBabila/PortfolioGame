using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Portfolio.Slot
{
    /// <summary>
    /// The physical fortune machine in the studio (the "FortuneMachine" mesh from Workshop.blend). Tap it and the
    /// player walks over; on arrival the machine's screen opens. Also gives it a floating name tag like a station.
    /// </summary>
    public class FortuneMachineSpot : MonoBehaviour
    {
        [SerializeField] float openDistance = 2.4f;

        ClickToMove player;
        Collider hitbox;
        bool wanted;

        public static void Attach(ClickToMove player)
        {
            var go = GameObject.Find("FortuneMachine");
            if (!go || !player || go.GetComponent<FortuneMachineSpot>()) return;
            var spot = go.AddComponent<FortuneMachineSpot>();
            spot.player = player;
            spot.hitbox = go.GetComponent<Collider>();
            if (!spot.hitbox)
            {
                var box = go.AddComponent<BoxCollider>();
                var r = go.GetComponent<Renderer>();
                if (r) { box.center = go.transform.InverseTransformPoint(r.bounds.center); box.size = go.transform.InverseTransformVector(r.bounds.size); box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z)); }
                spot.hitbox = box;
            }
            spot.AddTag();
        }

        TMP_Text tag;
        float tilt;

        /// <summary>The same paper name tag as the stations, a little higher so it clears the "You" tag beside it.</summary>
        void AddTag()
        {
            var skin = UISkin.Default;
            var r = GetComponent<Renderer>();
            float top = r ? r.bounds.max.y : transform.position.y + 2f;
            tag = new GameObject("Label").AddComponent<TextMeshPro>();
            tag.transform.SetParent(transform, true);
            tag.transform.position = new Vector3(r ? r.bounds.center.x : transform.position.x, top + 1.0f, r ? r.bounds.center.z : transform.position.z);
            tag.text = "Fortune Machine";
            if (TMP_Settings.defaultFontAsset) tag.font = TMP_Settings.defaultFontAsset;
            tag.rectTransform.sizeDelta = new Vector2(8f, 1.5f);
            tag.fontSize = 5f * 0.6f; // the station labels' size
            tag.alignment = TextAlignmentOptions.Center;
            tag.textWrappingMode = TextWrappingModes.NoWrap;
            if (skin && skin.IsReady) tilt = PaperTag.Dress(tag, skin, name);
            else { tag.fontStyle = FontStyles.Bold; tag.color = new Color(0.89f, 0.38f, 0.12f); }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (tag && cam) tag.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, tilt);
        }

        void Update()
        {
            if (!player) return;
            var pointer = Pointer.current;
            var cam = Camera.main;
            if (pointer != null && cam && pointer.press.wasReleasedThisFrame &&
                !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                // A tap on the machine itself (ClickToMove walks the player to it): open on arrival.
                // A tap anywhere else cancels.
                wanted = hitbox && hitbox.Raycast(cam.ScreenPointToRay(pointer.position.ReadValue()), out _, 500f);
            }

            if (!wanted || SlotMachineUI.IsOpen || VisitorSelect.IsOpen) return;
            var a = player.transform.position; var b = hitbox ? hitbox.bounds.ClosestPoint(a) : transform.position;
            a.y = b.y = 0;
            if (Vector3.Distance(a, b) <= openDistance)
            {
                wanted = false;
                SlotMachineUI.Open();
            }
        }
    }
}
