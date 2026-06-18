### Pathfinder: Kingmaker — KM Narrator

**Scope:** ElevenLabs-only v0.1. Port from `../RTVoiceMod` at git baseline v0.1.0 (no Kokoro/Tts/LocalTts).

Decompiled game code: `reference/Kingmaker/Assembly-CSharp/` (user-provided dnSpy export).

Before implementing, read `/docs/plans/execution-plan.md` and update `/docs/plans/progress.json` per `/docs/plans/agent-workflow.md`.

### Game paths

| What | Path |
|------|------|
| Game install | `D:\SteamLibrary\steamapps\common\Pathfinder Kingmaker` |
| Managed DLLs | `...\Kingmaker_Data\Managed` |
| UMM DLLs | `...\Kingmaker_Data\Managed\UnityModManager\` |
| Mod deploy | `...\Mods\KMNarrator\` |
| Player.log | `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\GameLogFull.log` (enable `-logging` in GOG/Steam if empty) |

### Key KM differences from RT

- **No `VoiceOverPlayer`** — patch `Kingmaker.Localization.LocalizedString.PlayVoiceOver(MonoBehaviour)`.
- **No `GetVoiceOverSound()`** — use `VoiceOverHelper` + `LocalizationManager.SoundPack.GetText(key)`.
- **Stop on cue advance:** `DialogController.PlayCue` prefix (KM has no per-cue `SoundState.StopDialog`).
- **Stop on dialog end:** `DialogController.StopDialog` postfix.
- **Book events:** `BookEventVM` + `BookEventBaseController.SetPage` hooks; skip book in `PlayVoiceOver` postfix.
- **Locale:** `LocalizationManager.CurrentLocale` is static (no `.Instance`).
- **Drop:** `SpaceEventVMPatch` (RT-only).

### Port map (RT → KM)

Copy with namespace `RTNarrator` → `KMNarrator`:

- `ElevenLabs/`, `Cache/`, `Audio/`, `Voice/CachedSpeechService.cs`, `VoiceResolver.cs`, `TextNormalizer.cs`
- `Settings.cs`, `UI/SettingsGui.cs` — remove `EnableSpaceEvents`
- `Voice/Narrator.cs` — adapt unvoiced check

Rewrite:

- `ModHarmony.cs`, all `Patches/*`

### Harmony

Manual `harmony.Patch(...)` only — **no `PatchAll`**.

### Commits

[Conventional Commits](https://www.conventionalcommits.org/): `feat(voice): ...`, `fix(patches): ...`, etc.

### Versioning

`deploy/KMNarrator/Info.json` — baseline `0.1.0`, `ManagerVersion` `0.22.0`.
