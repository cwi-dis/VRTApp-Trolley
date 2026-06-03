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

Three repository secrets must be set (`Settings → Secrets and variables → Actions`):

| Secret | Value |
|---|---|
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_LICENSE` | XML content of a Unity license file (see below) |

**Obtaining `UNITY_LICENSE`:**

1. On a machine with Unity installed, run:

   ```
   Unity -batchmode -createManualActivationFile -quit
   ```

   This produces `Unity_v6000.x.ulf` (or similar) in the current directory.

2. Upload that file at <https://license.unity3d.com/manual> and download the resulting `.ulf` licence file.

3. Copy the entire XML content of the `.ulf` file as the value of the `UNITY_LICENSE` secret.

Full instructions: <https://game.ci/docs/github/activation>

### Triggering a release

```
git tag v1.0
git push origin v1.0
```

Use `exp-` tags for experimental/pre-release builds and `v` tags for proper releases.
