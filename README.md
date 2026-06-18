# KM Narrator

AI voice acting for **unvoiced** dialogue in *Pathfinder: Kingmaker*, ported from
[RT Narrator](../RTVoiceMod). When the game would otherwise play no voice line, the mod
synthesizes speech via ElevenLabs, plays it in-game, and caches the result on disk so replays
are instant.

**ElevenLabs only** in v0.1.

## How it works

`dialogue cue → unvoiced check → text normalize → voice resolve → disk cache / ElevenLabs → playback`

- Hooks `LocalizedString.PlayVoiceOver` (Kingmaker has no separate `VoiceOverPlayer` class).
- Only narrates lines with **no** official Wwise VO — unless you disable **Only unvoiced lines**.
- **Stage directions** (`{n}…{/n}` narrator text) can be included or stripped before synthesis.
- **Per-character voices:** map a speaker name to an ElevenLabs voice ID and speed; unmapped
  speakers use the default voice and global speed slider.
- Generated MP3s are keyed by text, voice, model, and speed — each variant caches separately.
- Playback stops when dialogue advances (next cue or page turn).

Official voiced lines are never replaced. See [docs/plans/execution-plan.md](docs/plans/execution-plan.md)
for architecture details.

## Requirements

- *Pathfinder: Kingmaker* with **Unity Mod Manager** installed
- An [ElevenLabs](https://elevenlabs.io/) API key

**From source:** Visual Studio 2022 or the `dotnet` SDK with the .NET Framework 4.8 targeting pack.
Set `GameManagedPath` in [Directory.Build.props](Directory.Build.props) to your game's
`Kingmaker_Data\Managed` folder.

Reference DLLs under `lib/` — see [lib/README.md](lib/README.md).

## Download

Pre-built releases (when published) are on the GitHub Releases page. Download `KMNarrator-v*.zip`
and extract into the game's `Mods` folder:

```
<Steam>\steamapps\common\Pathfinder Kingmaker\Mods\
```

After extracting you should have `Mods\KMNarrator\KMNarrator.dll` plus `NAudio.dll`, `NLayer.dll`,
and `Info.json`. Launch the game, open Unity Mod Manager, enable **KM Narrator**, and load a save.

## Install (manual / from source)

1. Build or obtain `deploy/KMNarrator/` (see **Build** below). The folder must include **all**
   runtime DLLs — not just `KMNarrator.dll`.

2. Copy into the mod folder:

   ```powershell
   $dest = "D:\SteamLibrary\steamapps\common\Pathfinder Kingmaker\Mods\KMNarrator"
   New-Item -ItemType Directory -Force -Path $dest | Out-Null
   Copy-Item -Force deploy\KMNarrator\KMNarrator.dll, deploy\KMNarrator\NAudio.dll, deploy\KMNarrator\NLayer.dll, deploy\KMNarrator\Info.json $dest
   ```

   Or use the deploy script after a Release build: `.\deploy.ps1`

3. Launch the game, open Unity Mod Manager, enable **KM Narrator**, and load a save.

`NAudio.dll` and `NLayer.dll` are required — the mod fails to load without them.

## Configuration

Open Unity Mod Manager → select **KM Narrator** → expand settings:

1. Click **Show** next to **API key**, paste your ElevenLabs key, then **Save**.
2. Set **Voice ID** (or click **Refresh voices** and pick from the list).
3. Adjust **Model**, **Speed**, and **Volume** as needed.
4. Under **Character voices**, click **Add character** to map speaker names to voices and
   per-character speeds. Use the **speaker name** from either:
   - `GameLogFull.log` — search for `[KMNarrator] Speaker '...'` (logged on every narrated line), or
   - `Mods/KMNarrator/cache/manifest.json` — `speakerHint` on each entry (after the line is cached)
5. Click **Test synthesis** — you should hear the test line. Run again to verify cache (instant
   playback, no API call). **Stop playback** appears while audio is playing.

**Log file:** Kingmaker uses `GameLogFull.log`, not `Player.log`:

```
%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\GameLogFull.log
```

If the log is empty or missing, add `-logging` to the game's launch arguments (GOG: game Settings →
Custom settings → command line; Steam: Properties → Launch Options).

### Playback toggles

| Setting | Default | Effect |
|---------|---------|--------|
| Only unvoiced lines | On | Skip lines that already have official VO |
| Speak stage directions | On | Include narrator / `{n}…{/n}` text |
| Book event dialogues | On | Narrate book-style dialogues |
| Exploration barks | Off | Narrate ambient barks in exploration |
| Verbose logging | Off | Extra `[KMNarrator]` lines in `GameLogFull.log` |

## Cache

Generated audio is stored under `cache/` next to `KMNarrator.dll` in your mod folder:

```
Mods/KMNarrator/cache/
  manifest.json
  enGB/
    {hash}.mp3
```

Each manifest entry includes `speakerHint`, `text`, `voiceId`, and `speed` so you can look up which
name to use for character voice mapping.

**After upgrading:** delete `Mods/KMNarrator/cache/manifest.json` once (keep the `.mp3` files), then
play a few lines again so the manifest is rewritten with full metadata. Zip and share the `cache` folder with others — matching
lines play without API calls. MP3 files are gitignored in `deploy/`; your live cache lives in the
game `Mods` folder after deploy.

## Publishing

```powershell
.\publish.ps1
```

Builds Release, stages `artifacts/KMNarrator/`, and zips `artifacts/KMNarrator-v<version>.zip`
(version from `deploy/KMNarrator/Info.json`).

## Build

From the repo root:

```powershell
dotnet build KMNarrator.slnx -c Release
```

Output: `deploy/KMNarrator/` (`KMNarrator.dll` + `NAudio.dll` + `NLayer.dll` + `Info.json`)

Deploy to a local install:

```powershell
.\deploy.ps1
```

## Development

| Path | Purpose |
|------|---------|
| `src/KMNarrator/` | Mod source |
| `deploy/KMNarrator/` | Build output |
| `reference/Kingmaker/Assembly-CSharp/` | Decompiled game (gitignored) |
| `docs/plans/` | Execution plan + task tracking |
| `../RTVoiceMod/src/RTNarrator/` | Port source (ElevenLabs v0.1.0) |

## Sister project differences

| | RT Narrator | KM Narrator |
|---|-------------|-------------|
| Game DLL | `Code.dll` | `Assembly-CSharp.dll` |
| VO hook | `VoiceOverPlayer` | `LocalizedString.PlayVoiceOver` |
| Stop on cue advance | `SoundState.StopDialog` in `PlayCue` | `DialogController.PlayCue` prefix |
| Stop on dialog end | `SoundState.StopDialog` | `DialogController.StopDialog` |
| Mod path | AppData UMM folder | `Mods\KMNarrator\` |
| UMM ManagerVersion | `0.25.0` | `0.22.0` |
| TTS | ElevenLabs | ElevenLabs only (v0.1) |
