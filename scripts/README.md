# Build scripts

| Script | What it does |
|---|---|
| `build-mac.sh` | Builds the macOS player and deposits `Build/VRTApp-Trolley-mac.zip` |
| `build-windows.ps1` | Builds the Windows player and deposits `Build/VRTApp-Trolley-win.zip` |

Both scripts require the matching Unity version (see `ProjectSettings/ProjectVersion.txt`) to be installed via Unity Hub. Output goes to `Build/` which is gitignored.

Run `build-mac.sh` from any directory on a Mac. On Windows, run:

```
powershell -ExecutionPolicy Bypass -File scripts\build-windows.ps1
```

## GitHub Actions automation

`.github/workflows/build-release.yml` triggers on tags matching `v*` or `exp-*`. It builds both players in parallel using [game-ci/unity-builder](https://game.ci) and creates a GitHub release with both zips attached.

### One-time setup per repository

Follow <https://game.ci/docs/github/activation> to set up the required repository secrets. That page covers both Unity Personal (free) and Pro/Plus licenses and is kept up to date by the game-ci team.

### Triggering a release

```
git tag v1.0
git push origin v1.0
```

Use `exp-` tags for experimental/pre-release builds and `v` tags for proper releases.
