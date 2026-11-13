# Workshop Letters UI: PSD build spec

Source of truth for the UI art file. Style mockup (approved 2026-11-12):
https://claude.ai/artifact/5r8XAzHkdMC78Dw7xXd4SW

Direction: each station opens as a typed **letter on grainy paper**. Interactive things are **physical objects**:
admit-one **tickets** (games, projects) and **rubber stamps** (CTAs). All line art is **single-stroke**, in one accent.

## File

- **Built 2026-11-12:** `Assets/_Portfolio/Art/UI/UI_Workshop.psd` (built in Photoshop via adb-mcp). Every layer is a
  smart object holding a full-canvas PNG from `Art/UI/layers/`, which `Art/UI/tools/gen.html` draws from the mockup's
  exact paths (render: headless Edge `--dump-dom`, then `python -I decode.py <dom> <out>`).
- Unity import: `com.unity.2d.psdimporter` 15.0.0 is in `Packages/manifest.json`. Run **Portfolio → UI → Import
  Workshop PSD** (`Editor/WorkshopUIImport.cs`): it switches the .psd to the PSD Importer, ticks **Include Hidden
  Layers** (all `_Hover` / `_Pressed` / `_Active` states and the other ticket art are hidden), sets PPU 200 and max
  size 4096, then fills `Resources/UISkin.asset` with every layer sprite. Re-run it after every PSD edit.
- Sprites are looked up **by layer name**, so names must be unique across the file (hence `Panel_Shadow`,
  `Ticket_Shadow`, `Ticket_Play_*`, `Player_Play_*`). 9-slice borders are applied at runtime (`UISkin.Sliced`),
  not in the Sprite Editor. The old `_Ref_World` layer was removed so it doesn't bloat the atlas.
- Runtime wiring: `StationPanel` loads the skin from Resources (no scene edit). Paper sheet, close X with hover state,
  letterhead loop and signature that draw on; games and projects as `TicketCard`s; other links as `StampButton`s.
  Without a skin asset the UI keeps the old flat look.
- Color mode RGB 8-bit, sRGB. Canvas **3840 × 2160** = 2× the Canvas Scaler reference (1920×1080, `ResponsiveCanvas.cs`).
  In Unity set the sprites' Pixels Per Unit to 200 (2×) so 1 PSD px = 0.5 UI px.
- **No text layers for live content.** Titles, body, link labels, ticket names come from StationData/ProjectData via TMP.
  Text in the PSD only as a hidden `_Ref_Text` group for layout reference.
- Every layer that Unity animates or swaps is its **own layer**, named exactly as below. States are sibling layers
  (`_Normal`, `_Hover`, `_Pressed`); Unity shows one at a time.
- Strokes (`*_Stroke`) are separate layers on transparent background, line width 5 px (2×), round caps and joins.
  Unity draws them on with a fill/mask, so keep each one a single continuous path.

## Tokens

| Token | Hex | Use |
|---|---|---|
| paper | `#EFE9DC` | panel |
| paper-edge | `#D9D0BD` | edge shadow, perforation, dividers |
| card | `#F8F4EA` | tickets, stamp fill when inked text |
| ink | `#2B2622` | text, default strokes |
| ink-soft | `#6D645A` | secondary text |
| accent | `#E2611F` | single-stroke orange, stamps, active icons |
| world sky / floor | `#9DC3C9` / `#C9B79C` | reference only |

Ticket art tints: ShellBound `#DFE9E6`, Maskadu `#F1E0CF`, Where is my Mind `#E6E2EF`, ChatBaker `#F3E6C9`,
Kaa's Journey Back `#DFE8D5`, project default `#E3E8F2`.

Fonts (TMP in Unity, install locally for the reference text): Courier Prime (body, titles), Work Sans 300/500 (letterhead,
labels). Both OFL from Google Fonts. Add the OFL text next to the font files, as with Roboto/Heebo.

## Layer tree (sizes at 2×)

```
UI_Workshop.psb
├─ FX/
│  └─ Grain_Tile                 512×512, tileable noise, multiply, 35% (shared overlay)
├─ Panel_Station/
│  ├─ Panel_Shadow               soft drop shadow, separate so it can fade
│  ├─ BG_Paper                   1040×1400, paper + grain; 9-slice border 48
│  ├─ Header/
│  │  ├─ Logo_Stroke             92×68, accent, single loop
│  │  └─ Divider                 hairline, paper-edge
│  ├─ Btn_Close/
│  │  ├─ Btn_Close_Normal        60×60, ink X, 2 strokes
│  │  └─ Btn_Close_Hover         accent X
│  ├─ Link/
│  │  ├─ Arrow_Stroke            44×24, accent
│  │  └─ Underline_Stroke        600×12, wavy, accent (stretched in Unity)
│  ├─ Footer/
│  │  └─ Signature_Stroke        340×112, ink, one continuous line
│  └─ _Ref_Text (hidden)
├─ Ticket/
│  ├─ Ticket_Shadow
│  ├─ BG_Card                    300×340, card, side notches r=14 at 76 px from bottom; 9-slice top 30 / bottom 90
│  ├─ Art_Area                   300×168 tint block (recolored per game in Unity: keep it white, tint by Image.color)
│  ├─ Perforation                dashed line, paper-edge, 4 px
│  ├─ Btn_Play/
│  │  ├─ Ticket_Play_Normal      outline stamp, accent, rotated −4°
│  │  └─ Ticket_Play_Pressed     filled accent, card-colored glyph
│  └─ Art/
│     ├─ Art_ShellBound_Stroke   132×112 each, ink (Unity tints accent on hover)
│     ├─ Art_Maskadu_Stroke
│     ├─ Art_WhereIsMyMind_Stroke
│     ├─ Art_ChatBaker_Stroke
│     ├─ Art_KaasJourney_Stroke
│     └─ Art_Project_Stroke
├─ Stamp/
│  ├─ Stamp_Normal               double frame, accent, rough edge; 9-slice 24
│  ├─ Stamp_Inked                filled accent
│  ├─ Icon_Download_Stroke       32×32
│  └─ Icon_External_Stroke       32×32
├─ Career/
│  ├─ Rail                       ink, 6 px
│  ├─ Tick                       ink, reused per year
│  ├─ Arc_Stroke                 accent, reused and scaled per period
│  └─ Player/  Btn_Prev_*, Player_Play_*, Btn_Next_*  (each _Normal / _Hover)
└─ HUD/
   └─ QuickNav/
      ├─ BG                      paper bar; 9-slice 16
      └─ Icons/  Icon_About, Icon_Games, Icon_Projects, Icon_Career, Icon_Contact
                 (each _Normal = ink, _Active = accent), 60×60
```

## Motion (implemented in Unity, listed here so the art supports it)

| Element | Trigger | Motion |
|---|---|---|
| Panel_Station | open / close | slides up 40 px + rotates 2°→0 with overshoot / lifts −24 px, fades |
| *_Stroke | panel open, hover | draws on along the path (0.8–1.6 s) |
| Ticket | open | dealt in one by one (90 ms stagger), each with its own small tilt |
| Ticket | hover | straightens, lifts 6 px, shadow grows, art redraws in accent, Btn_Play scales 1.12 |
| Ticket | click | Btn_Play_Pressed, then the ticket tears away up-right and the link opens |
| Stamp | hover / press | straightens + 1.06 / 0.92 and swaps to Stamp_Inked, then a stamp-down settle |
| QuickNav icon | hover / select | redraws; _Active state on the open station |
