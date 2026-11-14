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
                // Paper-colored type with an ink edge: the studio floor is dark concrete.
                text.color = skin.paper;
                text.characterSpacing = 2f;
                text.outlineWidth = 0.2f;
                text.outlineColor = (Color32)skin.ink;
            }
        }
    }
}
