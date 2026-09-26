# WebGames

Put the WebGL builds of your other games here, one folder each:

```
WebGames/
  game-one/   <- contents of that game's WebGL build (index.html, Build/, TemplateData/)
  game-two/
```

`deploy.ps1` publishes each folder to `https://<user>.github.io/<repo>/games/<folder>/`.
Link to it from a station as `games/<folder>/`.

The folders are git-ignored on `main` to keep the repo light; they only live on the `gh-pages` branch.
Build them with **Compression: Gzip + Decompression Fallback** (or Disabled), since GitHub Pages can't send Content-Encoding headers.
