using TMPro;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Dresses the loose HUD pieces the scene builders made (Find me, Music, the hint line) in the Workshop Letters
    /// look. Finds them by name, so the scene file doesn't need to change; anything missing is skipped.
    /// </summary>
    public static class HudSkin
    {
        public static void Apply(Transform canvas, UISkin skin)
        {
            if (!canvas || !skin || !skin.IsReady) return;

            int i = 1;
            foreach (var name in new[] { "FindMeButton", "MusicToggle" })
            {
                var t = canvas.Find(name);
                if (t) StampButton.Apply(t.gameObject, skin, i++, paperBacking: true);
            }

            var hint = canvas.Find("Hint");
            if (hint && hint.TryGetComponent<TMP_Text>(out var text))
            {
                skin.Type(text, FontStyles.Italic);
                text.color = skin.ink;
                text.characterSpacing = 2f;
                // A thin outline keeps the ink readable over both the light floor and the dark machines.
                text.outlineWidth = 0.18f;
                text.outlineColor = (Color32)skin.paper;
            }
        }
    }
}
