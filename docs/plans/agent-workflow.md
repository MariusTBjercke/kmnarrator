# Multi-agent workflow

Use this when more than one agent (or human + agent) works on KMNarrator.

## Before starting a task

1. Read `execution-plan.md` for context on the task's phase.
2. Open `progress.json`.
3. Pick a task with `status: "pending"` whose `dependsOn` tasks are all `"completed"`.
4. Set the task to `"in_progress"`, set `owner` to your agent/session id, set `lastUpdated` at the root.
5. **Do not** take another task that touches the same `files` as an `in_progress` task (file ownership).

## While working

- Stay within the task scope; avoid drive-by refactors.
- Harmony patches: use **manual** `harmony.Patch(...)` per class — no `PatchAll` (older .NET / project convention).
- Never commit API keys; settings only.
- Reference game DLLs from `lib/` or `GameManagedPath` in `Directory.Build.props` — do not copy copyrighted game DLLs into `src/`.
- Decompiled sources go in `reference/` only (gitignored).

## After finishing

1. Mark the task `"completed"` and clear or keep `owner` as completed-by note.
2. Update root `lastUpdated` and `summary` in `progress.json`.
3. If you made a design choice not in the plan, append to `decisions[]`.
4. If blocked, set task `"blocked"`, add an entry to `blockers[]`.

## Parallelism rules

| Safe in parallel | Serialize (one at a time) |
|------------------|---------------------------|
| Research / decompile reference | `Main.cs`, `Settings.cs` |
| `ElevenLabs/*`, `Tts/*`, `Cache/*` | `Patches/*` (coordinate by file) |
| Docs only | First-time `Info.json` + deploy layout |
| Port shared RT code (audio, cache) | `ModHarmony.cs` until patch targets confirmed |

## Claim format for `owner`

Use a short id, e.g. `agent-2026-06-17-a` or `cursor-session-1`.

## Testing handoff

When a phase expects in-game verification, leave notes in the task's `notes` field:

- Build output path: `deploy/KMNarrator/`
- Deploy to: `...\Pathfinder Kingmaker\Mods\KMNarrator\`
- Log prefix to grep: `[KMNarrator]`
- Suggested test: start a dialog with an NPC that has no VO line
