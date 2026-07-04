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
| `{Feature}Log.cs` | Optional — shared formatting / semantic log helpers via `ModLog` |
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
7. Log via `ModLog` with a local `Feature` const; add `{Feature}Log.cs` only when you need shared message formatting or semantic helpers (see [AGENTS.md](../AGENTS.md#logging)).
8. Document config keys in [CONFIG.md](CONFIG.md) (linked from the README Config section).
9. Run `./scripts/build.sh` (Debug and Release if touching build-sensitive code).

## UI toolkit

Shared uGUI/TextMeshPro primitives live in `src/MimesisPlayerEnhancement/Ui/` (namespace `MimesisPlayerEnhancement.Ui`). Features compose these instead of doing RectTransform math or TMP reflection themselves; Harmony patches and game-specific wiring stay in the feature.

- **ModUiRoot** — attach point into the game's UI hierarchy (`UIManager.nodes[eUIHeight.Top]`).
- **ModUiAssets** — sprites/font/SFX/colors cloned from vanilla prefabs (`TryCaptureFromMainMenu`), with `Fallback` solid-color defaults.
- **ModPage** — full-screen overlay with title, content, action and back bands.
- **ModPanel** — dim overlay with a centered panel.
- **ModButton** — styled button with TMP label and hover/click SFX.
- **ModScrollList** — vertical scroll view; add rows into `Content` with a row factory.
- **ModUiText / ModUiLayout / ModUiFactory** — TMP reflection helpers, anchor math, low-level element creation.

Minimal example:

```csharp
if (!ModUiAssets.TryCaptureFromMainMenu(mainMenu, loadTram, out ModUiAssets assets))
{
    assets = ModUiAssets.Fallback;
}

GameObject root = ModUiRoot.CreateUiRoot(ModUiRoot.GetTop()!, "MyFeatureUi");
ModPage page = ModPage.Create(root.transform, assets);
page.CreateTitle(assets, "My Feature");
ModButton.Create(page.CreateActionButtonRow(), assets, "Apply", expandWidth: true, () => MyFeatureApplier.Apply());
```

`Features/ExtendedSaveSlots/` (save slot picker) is the reference consumer.

## Per-save sidecar persistence lifecycle

Per-save data uses sidecar files beside vanilla saves (`MMGameData{N}.mpe-{kind}.sav`). Runtime state lives in memory during an active host session. `SaveSlotSidecarPersistence` coordinates load, flush, and teardown.

| Sidecar | File | Runtime store |
|---------|------|---------------|
| Statistics | `.mpe-stats.sav` | `StatisticsTracker._players` |
| Config overrides | `.mpe-overrides.sav` | `SaveSlotConfigStore._runtimeDoc` |
| Player names | `.mpe-names.sav` | `WebDashboardPlayerNameStore._names` |

| Phase | Behavior |
|-------|----------|
| **Save load** | `GameSessionInfoLoadPatches` → `SaveSlotSidecarPersistence.OnSaveSlotLoaded` reads all three sidecars once. |
| **Gameplay** | Memory-only mutations. Statistics bump `StatisticsTracker.Revision` on stat changes. Per-save config overrides and player names mark their store dirty. No disk reads or writes. |
| **Vanilla save** | `MaintenanceRoom.SaveGameData` success → `SaveSlotSidecarPersistence.OnGameSaved` flushes dirty sidecars (manual save and auto-save slot 0). |
| **Session end** | Host leaves session (`SessionJoined` false) → `OnSessionEnded` finalizes open stats sessions in memory, clears runtime state, reloads global config. **No disk write.** |
| **Mod unload** | `SaveSlotSidecarPersistence.FlushAllSync` saves any remaining dirty sidecars (safety net). |

The web dashboard reads only from in-memory stores during gameplay. Leaderboard JSON is built on a background thread when `StatisticsTracker.Revision` changes. Global config edits from the web UI still write the main mod TOML immediately; per-save overrides apply live in memory and flush on vanilla save.

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
