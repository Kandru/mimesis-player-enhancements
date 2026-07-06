# Agent guide — Mimesis Player Enhancement

Instructions for AI coding agents working in this repository.

## Do not create `.tmp-*` scratch projects

When you need game source or MelonLoader APIs, **use the paths below**. Do not create throwaway folders like `.tmp-inspect/`, `.tmp-reflect/`, or similar at the repo root.

| Need | Where | Path |
|------|-------|------|
| Game source (types, methods, call chains, constants) | **Decompiled source** | `deps/decompiled/**/*` |
| MelonLoader APIs (logging, mod base types, loader internals) | **MimesisReflectionTool** | `src/MimesisReflectionTool/` |
| Quick metadata lookup (optional; no file read) | **MimesisInspectionTool** | `src/MimesisInspectionTool/` |
| Regenerate decompiled trees after a game update | **decompile-game.sh** | `scripts/decompile-game.sh` |

Inspection and reflection tools live in `src/MimesisPlayerEnhancement.sln`, build with `dotnet build`, and output to their own `bin/` folders (not `dist/`).

### Quick start

```bash
# Game source — search/read under deps/decompiled/ (e.g. Assembly-CSharp/**/MMSaveGameData.cs)
rg "CheckSaveSlotID" deps/decompiled/

# Optional quick metadata (no file read)
dotnet run --project src/MimesisInspectionTool -- constants MMSaveGameData
dotnet run --project src/MimesisInspectionTool -- member MMSaveGameData CheckSaveSlotID

# MelonLoader runtime reflection
dotnet run --project src/MimesisReflectionTool -- properties MelonLoader.Logging.ColorARGB Green
dotnet run --project src/MimesisReflectionTool -- type MelonLoader.MelonMod
```

See each tool's README for full command reference:

- [src/MimesisInspectionTool/README.md](src/MimesisInspectionTool/README.md)
- [src/MimesisReflectionTool/README.md](src/MimesisReflectionTool/README.md)

### When to extend a tool vs. add a new one

- **New command or printer for the same assembly family** → extend `Program.cs` or the printer in the existing tool.
- **Truly different concern** (e.g. IL decompilation, patch validation) → add a new project under `src/` with a proper name, README, and solution entry — not a `.tmp-*` folder.

### Reference assemblies

Run `./scripts/bootstrap-deps.sh` once to populate `deps/reference/Managed/` and `deps/reference/MelonLoader/net35/`. Tools fall back to `MIMESIS_PATH` or `--game` / `--managed` / `--melonloader` flags when bootstrap paths are missing.

Game source is already decompiled under `deps/decompiled/` (gitignored). Search and read files there — e.g. `deps/decompiled/Assembly-CSharp/**/*.cs`. Run `./scripts/decompile-game.sh` only to refresh after a game patch (requires `dotnet tool install -g ilspycmd`).

## Mod project layout

- Main mod: `src/MimesisPlayerEnhancement/` (netstandard2.1, Harmony patches)
- Build output: `dist/debug/` or `dist/prod/` (not committed)
- Game references: `deps/reference/` after bootstrap, or local install via `PathConfig.props` / `MIMESIS_PATH`

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for architecture, feature scaffolding, and formatting commands.

## Build verification

Always verify changes compile before considering work complete:

```bash
./scripts/bootstrap-deps.sh   # first time only
./scripts/build.sh              # Debug → dist/debug/
./scripts/build.sh Release      # Release → dist/prod/
```

Equivalent: `dotnet build src/MimesisPlayerEnhancement.sln -c Debug`.

Build runs `./scripts/format-code.sh` before compile (skip with `SKIP_FORMAT=true`). Verify only: `./scripts/format-code.sh --verify`

## Mod development

### Feature module registration

New features must be registered in `FeatureModules.All` (`src/MimesisPlayerEnhancement/Util/FeatureModule.cs`). This is the project's convention-based discovery — there is no DI container.

```csharp
new FeatureModule("MyFeature", MyFeaturePatches.Apply, MyFeaturePatches.RefreshFromConfig),
```

Use `syncFromConfig` when live state must revert on config reload; use `onUpdate` for per-frame work; set `throttledUpdate: true` when `Mod.cs` should throttle calls.

### Harmony patch pattern

Follow existing features (e.g. `Features/MorePlayers/MorePlayersPatches.cs`):

1. Static `{Feature}Patches.Apply(Harmony harmony)` entry point
2. Nested `[HarmonyPatch]` classes in the same file, or separate types under `Features/{Feature}/Patches/`
3. Use `HarmonyPatchHelper` for discovery (`GetNestedPatchTypes`, `GetNamespacePatchTypes`), apply, audit, and summary logging
4. `private const string Feature = "FeatureName"` — must match the config section name and log feature tag

### Config keys

For each feature:

1. Add `Enable{Feature}` master toggle and options in `src/MimesisPlayerEnhancement/Config/ModConfig.cs`
2. TOML section: `[MimesisPlayerEnhancement_{FeatureName}]`
3. Update [docs/CONFIG.md](docs/CONFIG.md)

Per-save overrides use `SaveSlotConfigStore`; global options live in the main section.

### Three-layer disable

When a feature's `Enable*` toggle is off (`FeatureToggleGate`):

1. **Resolvers** return neutral values (e.g. multiplier `1f`)
2. **Appliers / patches** early-return before mutating game state
3. **Live-state features** revert mutations in `SyncFromConfig` when disabled

### Host-only gating

Most gameplay features are host-only. Use `HostApplyGate.ShouldApplyHostOnlyFeature()` before applying changes. Use `GameSessionAccess` for session/save-slot context. Clients do not need this mod installed.

### Logging

All log output goes through `ModLog` (`src/MimesisPlayerEnhancement/Logging/ModLog.cs`). Do not call `MelonLogger` directly from feature code.

#### Default: inline `ModLog` + `Feature` const

For most logging — patch catch blocks, one-off messages, simple features — use a local feature tag and call `ModLog` directly:

```csharp
private const string Feature = "JoinAnytime"; // must match config section / HarmonyPatchHelper tag

ModLog.Info(Feature, $"Late joiner in maintenance — uid={player.UID}");
ModLog.Debug(Feature, "Moving player snapshot Maintenance -> Waiting");
ModLog.Warn(Feature, $"SendPreGameTramState failed — {ex.Message}");
```

- The feature tag is added automatically — **do not repeat it in message bodies**
- Use em dashes (—) to separate clauses
- `ModLog.Debug` only emits when `EnableDebugLogging` is true
- Patch files: put `private const string Feature` on **each nested class** that logs (nested classes do not inherit outer constants)

#### When to add `{Feature}Log.cs`

Add a dedicated log class **only when** the feature has shared formatting or repeated semantic events — not for every new feature:

```csharp
internal static class SpawnScalingLog
{
    private const string Feature = "SpawnScaling";

    internal static void DebugEntitySpawned(...) =>
        ModLog.Debug(Feature, $"Entity spawned — category={...}, ...");

    internal static void InfoScalingApplied(int playerCount) => ...;
}
```

Good candidates: multi-parameter message builders, hot-path debug helpers with early-return, formatting reused across many call sites.

#### Level guide

| Level | When | Examples |
|-------|------|----------|
| **Info** | Session/run-level outcomes users or hosts care about | Feature applied to a room, dungeon pick changed, player joined/kicked |
| **Debug** | Diagnostic detail; gated by `EnableDebugLogging` | Per-spawn traces, skip/defer reasons, hot-path scalings |
| **Warn** | Recoverable failures, fallbacks, patch errors | Empty dungeon pool, Harmony exception, missing reflection target |
| **Error** | Rare; unrecoverable or data-loss risk | Corrupt save, failed critical write |

**Do log:** first application to game state, values changed by mod logic, host-only skips once per context (`MarkSkippedOnce`), patch summaries via `HarmonyPatchHelper`.

**Do not log:** every frame/hot path when unchanged, when feature is off and vanilla runs unchanged, duplicate unchanged state.

For hot paths (scrap prices, per-spawn rolls), use debug helpers that early-return when `EnableDebugLogging` is false or values are unchanged. Reserve **Info** for run-level events (`EconomyLog.InfoApplied` vs inline debug in patches).

#### Reference implementations

| Style | Examples |
|-------|----------|
| Inline `ModLog` | JoinAnytime, Persistence, Statistics, MorePlayers, WebDashboard |
| `{Feature}Log` with semantic methods | SpawnScaling, LootMultiplicator, Economy, DungeonRandomizer, PlayerTuning, DungeonTime, DeadPlayerFeatures (MimicPossession) |

`HarmonyPatchHelper` may call `ModLog` directly (infrastructure).

### Game inspection workflow

| Goal | Where |
|------|-------|
| Game types, method bodies, call chains, constants | **`deps/decompiled/**/*`** |
| MelonLoader / loader APIs | **MimesisReflectionTool** |
| Quick metadata without opening files (optional) | **MimesisInspectionTool** |
| Refresh decompiled output after game update | **`scripts/decompile-game.sh`** |

Start in `deps/decompiled/` for patch design and cross-type navigation. Use MimesisInspectionTool only for fast one-off metadata when you do not need full method bodies.
