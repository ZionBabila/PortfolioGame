# Publishes the portfolio to the gh-pages branch (served by GitHub Pages).
#   ./deploy.ps1            -> build WebGL, then publish
#   ./deploy.ps1 -SkipBuild -> publish the existing Builds/WebGL
# Other games' WebGL builds in WebGames/<name>/ are published under games/<name>/.
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$build = Join-Path $root 'Builds/WebGL'
$site = Join-Path $root '.site'

if (-not $SkipBuild) {
    unity run $root --no-banner -- -executeMethod Portfolio.EditorTools.WebBuild.BuildCli -logFile (Join-Path $root 'Logs/web-build.log')
    if ($LASTEXITCODE -ne 0) { throw "WebGL build failed - see Logs/web-build.log" }
}
if (-not (Test-Path (Join-Path $build 'index.html'))) { throw "No build at $build" }

Push-Location $root
try {
    if (-not (Test-Path $site)) {
        git fetch origin gh-pages 2>$null
        if ($LASTEXITCODE -eq 0) { git worktree add $site gh-pages }
        else { git worktree add --orphan -b gh-pages $site }
    }

    Get-ChildItem $site -Force | Where-Object Name -ne '.git' | Remove-Item -Recurse -Force
    Copy-Item (Join-Path $build '*') $site -Recurse
    $games = Join-Path $root 'WebGames'
    Get-ChildItem $games -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $site "games/$($_.Name)") -Recurse
    }
    New-Item -ItemType File (Join-Path $site '.nojekyll') -Force | Out-Null

    git -C $site add -A
    git -C $site commit -m "Deploy $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    git -C $site push origin gh-pages
}
finally { Pop-Location }
