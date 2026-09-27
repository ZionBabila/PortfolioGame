# Portfolio Game

An interactive portfolio: a small low-poly island you explore point-and-click style.
Each building is a station — **About**, **Playable Games**, **Projects**, **Career Path**, **Contact** —
and walking into one opens its content and links.

**Play:** https://ZionBabila.github.io/PortfolioGame/ *(after the first deploy)*

Built with Unity 6.6 (URP), WebGL. One build serves desktop (16:9) and phones (9:16).

## How it works

| Piece | File |
|---|---|
| Click-to-walk on a NavMesh, hover highlight, arrival events | `Assets/_Portfolio/Scripts/Player/ClickToMove.cs` |
| Station content (title, text, image, links) — edit these, not code | `Assets/_Portfolio/Content/Stations/*.asset` |
| Isometric camera that zooms/follows in portrait | `Assets/_Portfolio/Scripts/World/ResponsiveCamera.cs` |
| Content card: side panel in landscape, bottom sheet in portrait | `Assets/_Portfolio/Scripts/UI/StationPanel.cs` |
| Popup-safe links in WebGL (open on the DOM pointerup) | `Scripts/Web/WebLinks.cs`, `Plugins/WebGL/PortfolioLinks.jslib` |
| Toon shading + inverted-hull ink outline (URP) | `Assets/_Portfolio/Shaders/ToonOutline.shader` |
| Full-window responsive WebGL page | `Assets/WebGLTemplates/Responsive/` |

## Workflow

- **Model:** edit `Assets/_Portfolio/Art/Models/Workshop.blend` in Blender and save. Unity imports the .blend directly and
  syncs the scene (toon materials, colliders, new `ST_<Name>` stations, NavMesh). Textures go in `Assets/_Portfolio/Art/Textures`.
- **Edit content:** select an asset in `Assets/_Portfolio/Content/Stations/` and fill the Inspector.
- **Camera:** Cinemachine cameras under **Cameras**; add zones with **Portfolio → Camera → Add Camera Zone**.
- **Preview both layouts:** Ctrl+Alt+1 (16:9) / Ctrl+Alt+2 (9:16) in the Game view.
- **Build:** **Portfolio → Build WebGL** → `Builds/WebGL/`.
- **Test locally:** serve the build folder, e.g. `npx http-server Builds/WebGL` (opening `index.html` from disk won't work).
- **Deploy:** `./deploy.ps1 -SkipBuild` pushes the build to the `gh-pages` branch. Other games go in `WebGames/<name>/` and are served at `games/<name>/`.

## Credits

- Music: "Happy Ukelele Island Surfing Theme" - Tarush Singhal ([OpenGameArt](https://opengameart.org/content/happy-ukelele-island-surfing-theme), CC0)
- Fonts: Roboto and Heebo (SIL Open Font License, see `Assets/_Portfolio/Art/Fonts`)
