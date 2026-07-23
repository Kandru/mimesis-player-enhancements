# Agent guide

## Game source & tools

No `.tmp-*` scratch folders at repo root.

| Need | Path |
|------|------|
| Game source (types, methods, constants) | `deps/decompiled/<version>/**/*` |
| MelonLoader APIs | `src/MimesisReflectionTool/` |
| Quick metadata (optional) | `src/MimesisInspectionTool/` |
| Dungeon seed pools | `src/MimesisSeedScanner/` |
| Refresh decompiled after game patch | `make decompile` (or `scripts/decompile-game.sh` with host ilspycmd) |

`make deps` → `deps/reference/Managed/` + `deps/reference/MelonLoader/net35/` (fallback: `MIMESIS_PATH`, `--game`/`--managed`/`--melonloader`). Tools: `make tools` → `src/*/bin/`. Extend existing tool `Program.cs` for new commands; new concern → new `src/` project with README.

Start in `deps/decompiled/<version>/` for patch design. InspectionTool for one-off metadata; ReflectionTool for MelonLoader.

## Layout & build

- Mod: `src/MimesisPlayerEnhancement/` (netstandard2.1) → `dist/{debug|prod}/`
- Web: `src/MimesisPlayerEnhancementWeb/` → `dist/webinterface/{debug|prod}/`
- Architecture/UI/sidecars: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)

Verify before done: `make check`, `make debug` (`make deps` first time; `make release`/`make tools` as needed). Docker (`--rm`): `mpe-ops:local`, `mcr.microsoft.com/dotnet/sdk:10.0`, `node:22-alpine`. Package caches: `mpe-nuget-cache`, `mpe-npm-cache`.

## New feature

1. `Features/{Name}/` + `{Name}Patches.Apply(Harmony)` → `HarmonyPatchHelper.GetNamespacePatchTypes` + `ApplyPatchTypes`
2. Patch types in `Features/{Name}/Patches/*.cs` (one game type per file; `Feature` const per class that logs)
3. Register in `FeatureModules.All` (`Util/FeatureModule.cs`); `syncFromConfig` to revert live state; `onUpdate`/`throttledUpdate`/`onDeinitialize` as needed
4. `Enable{Name}` + options in `ModConfig.cs`; TOML `[MimesisPlayerEnhancement_{Name}]`; update [docs/CONFIG.md](docs/CONFIG.md); per-save via `SaveSlotConfigStore`
5. Host-only: `HostApplyGate.ShouldApplyHostOnlyFeature()`; session context: `GameSessionAccess`
6. Disabled (`FeatureToggleGate`): resolver neutral → patch early-return → `SyncFromConfig` revert

Example: `Features/MorePlayers/MorePlayersPatches.cs` + `MorePlayers/Patches/`.

## Harmony

- Confirm targets in `deps/decompiled/<version>/` before patching
- `AccessTools.Method`/`Field` over string literals; `TargetMethod()` must return valid `MethodBase`
- try/catch in patch bodies; `ModLog.Warn(Feature, …)` on recoverable failure

## Logging

`ModLog` only (not `MelonLogger`). `ModLog.Info/Debug/Warn/Error(Feature, msg)` — tag auto-prefixed, em dashes in messages, don't repeat tag in body. `Debug` gated by `EnableDebugLogging`.

`{Feature}Log.cs` only for shared formatting or hot-path debug helpers with early-return. Info = run-level; Debug = diagnostic; Warn = recoverable; Error = rare critical. Log first apply, changed values, `MarkSkippedOnce` skips; not every frame or unchanged state.
