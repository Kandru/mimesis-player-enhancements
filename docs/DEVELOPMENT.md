# Development guide

Architecture and conventions for working on Mimesis Player Enhancement. For AI agent rules and inspection tools, see [AGENTS.md](../AGENTS.md).

## Architecture

```
Mod.cs (MelonMod entry)
  ├── ModConfig.Initialize / Changed
  ├── Harmony instance
  └── FeatureModules.All
        ├── {Feature}Patches.Apply(harmony)
        ├── SyncFromConfig (optional)
        └── OnUpdate (optional, may be throttled)
```

Each feature lives under `src/MimesisPlayerEnhancement/Features/{FeatureName}/`:

| File pattern | Role |
|--------------|------|
| `{Feature}Patches.cs` | Harmony entry point, nested patch classes |
| `Patches/*.cs` | Additional patch types (optional) |
| `{Feature}Log.cs` | Feature-scoped logging via `ModLog` |
| `{Feature}Resolver.cs` | Computes multipliers/values from config |
| `{Feature}Applier.cs` | Applies resolved values to game state |
| `{Feature}Runtime.cs` | Per-frame or session state (optional) |

Shared utilities: `src/MimesisPlayerEnhancement/Util/` (`HarmonyPatchHelper`, `HostApplyGate`, `GameSessionAccess`, etc.).

Config: `src/MimesisPlayerEnhancement/Config/ModConfig.cs` + TOML sections `MimesisPlayerEnhancement_{FeatureName}`.

## Add a new feature

1. Create `Features/{FeatureName}/` with `{Feature}Patches.cs` implementing `Apply(Harmony)`.
2. Add nested `[HarmonyPatch]` classes; use `HarmonyPatchHelper` for apply/audit (see `MorePlayersPatches.cs`).
3. Add `Enable{FeatureName}` and options to `ModConfig.cs`.
4. Register in `FeatureModules.All` (`Util/FeatureModule.cs`).
5. If host-only, gate mutations with `HostApplyGate.ShouldApplyHostOnlyFeature()`.
6. If the feature has multipliers, implement resolver → applier with `FeatureToggleGate` neutral values when disabled.
7. Add `{Feature}Log.cs` delegating to `ModLog` with matching feature string.
8. Document config keys in [CONFIG.md](CONFIG.md) (linked from the README Config section).
9. Run `./scripts/build.sh` (Debug and Release if touching build-sensitive code).

## Host-only and session access

- **HostApplyGate** — returns false for join-anytime participants and when the feature toggle is off; allows solo/host play when network pdata is null.
- **GameSessionAccess** — save slot, session, and hub data lookups shared across features.

## Build and deploy

See [BUILD.md](BUILD.md) for compile commands, bootstrap, and copying the DLL into your game for testing.

## Formatting

Code style is defined in `.editorconfig` at the repo root. Format before committing:

```bash
dotnet format src/MimesisPlayerEnhancement.sln
```

Verify without modifying files:

```bash
dotnet format --verify-no-changes src/MimesisPlayerEnhancement.sln
```

Auto-format-on-build is intentionally not enabled — run `dotnet format` manually when needed.

Project-wide global usings live in `src/MimesisPlayerEnhancement/GlobalUsings.cs` (`System.Collections.Generic`, `HarmonyLib`, `MimesisPlayerEnhancement.Util`). `System` is omitted because it conflicts with `UnityEngine.Object` and `UnityEngine.Random`.
