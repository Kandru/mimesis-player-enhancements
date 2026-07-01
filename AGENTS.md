# Agent guide — Mimesis Player Enhancement

Instructions for AI coding agents working in this repository.

## Do not create `.tmp-*` scratch projects

When you need to inspect game or MelonLoader assemblies, **use the existing dev tools under `src/`**. Do not create throwaway folders like `.tmp-inspect/`, `.tmp-reflect/`, or similar at the repo root.

| Need | Tool | Path |
|------|------|------|
| Game types, methods, constants (`Assembly-CSharp`) | **MimesisInspectionTool** | `src/MimesisInspectionTool/` |
| Full decompiled C# source (browse/read code) | **decompile-game.sh** | `scripts/decompile-game.sh` → `deps/decompiled/` |
| MelonLoader APIs (logging, mod base types, loader internals) | **MimesisReflectionTool** | `src/MimesisReflectionTool/` |

Both tools are in `src/MimesisPlayerEnhancement.sln`, build with `dotnet build`, and output to their own `bin/` folders (not `dist/`).

### Quick start

```bash
# Game metadata (safe, no code execution)
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

For readable source when inspecting types or call chains, run `./scripts/decompile-game.sh` (requires `dotnet tool install -g ilspycmd`). Output lands in `deps/decompiled/` and is gitignored.

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

Optional formatting check: `dotnet format --verify-no-changes src/MimesisPlayerEnhancement.sln`

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

Per-feature static log wrappers (e.g. `SpawnScalingLog`) delegate to `ModLog`:

- Feature tag is added automatically — do not repeat it in messages
- Use em dashes (—) to separate clauses
- `ModLog.Debug` only emits when `EnableDebugLogging` is true

### Game inspection workflow

| Goal | Tool |
|------|------|
| Type/method signatures, constants, quick lookup | **MimesisInspectionTool** |
| MelonLoader / loader APIs | **MimesisReflectionTool** |
| Read full call chains, browse decompiled source | **decompile-game.sh** → `deps/decompiled/` |

Start with InspectionTool for targeted lookups; decompile when you need readable method bodies or cross-type navigation.
