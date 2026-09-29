# PortfolioGame — notes for Claude

Interactive portfolio: a point-and-click industrial-design workshop (loft) built in Unity 6.6 (URP), shipped as a
WebGL build on GitHub Pages: https://zionbabila.github.io/PortfolioGame/ . The owner speaks Hebrew; answer in Hebrew.

## Layout
- `Assets/_Portfolio/Art/Models/Workshop.blend` — THE world model. Unity imports the .blend directly (runs Blender in
  the background). Stations are empties `ST_<Name>` with child `ST_<Name>_Approach` (where the player stands; front = local -Y).
- `Assets/_Portfolio/Scenes/Workshop.unity` — the scene. The owner edits it by hand: never regenerate it
  (`Portfolio → Advanced → Rebuild Workshop Scene From Scratch` overwrites it — only with explicit permission).
- `Assets/_Portfolio/Content/Stations/*.asset` — station text/links (StationData). Content lives here, not in code.
- `Assets/_Portfolio/Editor/` — tooling (menu **Portfolio**): Sync Workshop From Blender, Build WebGL,
  Publish To Website, Upload Last Build, Test Build Locally, camera zones, fonts, TMP conversion, render quality.
- `Assets/_Portfolio/Scripts/` — runtime: ClickToMove (NavMesh), Station, StationPanel (side panel 16:9 / bottom sheet
  9:16), QuickNav, MusicPlayer, camera: OverviewTarget + AspectLens + CameraZone + CinemachineRoomConfiner.
- `Art/Blender/build_workshop.py` — generator for the FIRST version of the model. Running it rebuilds the Workshop
  collection and erases manual Blender edits. Normally don't use it.

## How things work (non-obvious)
- Import pipeline (`WorkshopModelPostprocessor`): bakes smoothed normals into UV3 for the toon outline; resolves textures
  by file name (Blender paths arrive broken as "C//Users/..."); Poliigon add-on materials are resolved by name
  (`Poliigon_<Name>_<Id>_<Size>` → `..._BaseColor` in the `<Size>` folder) because their color goes through a Mix node.
- After a .blend re-import, the open Workshop scene auto-syncs (toon materials by NAME — the importer reorders slots —,
  colliders, new stations, NavMesh). Station labels (TMP) must never get toon materials.
- Camera: Cinemachine. `CM Overview` follows `Overview Target` (look-ahead: player on the third line, smoothed walking
  direction; rises/zooms with player height). Station zones (`CameraZone` with `station` set) activate only when the
  player is visiting that station. `CM Gallery` zone faces the mezzanine. `CinemachineRoomConfiner` keeps every view
  inside the walls (Confiner2D can't: it bakes in world XY). Game view: Ctrl+Alt+1 = 1920×1080, Ctrl+Alt+2 = 1080×1920.
- HR recruiter NPC (`Scripts/NPC`): wanders, chases the player, asks a question from
  `Content/HR/HRQuestions.asset` (edit text there), then leaves them alone for `cooldown` seconds.
  Rebuild/re-wire: Portfolio → NPC → Add HR Recruiter.
- Text is TextMeshPro: Roboto (default) with Heebo as Hebrew fallback, then LiberationSans for symbols.
- Web: `PROJECT:Responsive` WebGL template (full window), Gzip + decompression fallback (Pages has no Content-Encoding),
  no splash, Render Scale 1 + MSAA 4 (Web uses the "Mobile" URP asset).

## Build & publish
- In Unity: **Portfolio → Publish To Website (Build + Upload)**, or Build WebGL then answer the dialog.
- Without the menu: build to `Builds/WebGL`, then `pwsh -File deploy.ps1 -SkipBuild` (copies to the `gh-pages`
  worktree `.site/`, commits, pushes). Needs PowerShell 7 (`pwsh`, installed from the Store, in WindowsApps).
- Before uploading, make sure `Builds/WebGL` is the build you mean to publish (the owner also builds by hand).
- A running Editor can be driven with the Unity CLI Pipeline (`unity command ...`); pass `--caller plugin`.

## Licensing (public repo)
- Poliigon textures are gitignored (`Assets/_Portfolio/Art/Textures/Poliigon_*`): usable in the build, not
  redistributable. A fresh clone has no wood/concrete/plaster textures until they're downloaded again.
- Fonts Roboto/Heebo: OFL. Music: CC0 (Tarush Singhal, credited in README). Don't add assets without checking the license.

## What cannot run without the owner's PC
Unity Editor work, Blender, and WebGL builds need the Windows PC. From a cloud session you can edit C#, station content
(`.asset` YAML), docs and scripts, commit and push — but you can't compile, play-test or build; say so when relevant.
