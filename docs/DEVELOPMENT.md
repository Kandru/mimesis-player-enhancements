# Development guide

Architecture and conventions for working on Mimesis Player Enhancement. For AI agent rules, game inspection, and logging detail, see [AGENTS.md](../AGENTS.md).

## Layout

| Path | Purpose |
|------|---------|
| `src/MimesisPlayerEnhancement/` | Main mod (netstandard2.1, Harmony) |
| `src/MimesisPlayerEnhancement/Features/{FeatureName}/` | One folder per feature |
| `src/MimesisPlayerEnhancement/Config/` | `ModConfig`, per-save overrides, sidecar coordination |
| `src/MimesisPlayerEnhancement/Ui/` | Shared uGUI/TMP toolkit (`MimesisPlayerEnhancement.Ui`) |
| `src/MimesisPlayerEnhancement/Util/` | Cross-feature helpers (`HarmonyPatchHelper`, gates, session access) |
| `src/MimesisInspectionTool/`, `src/MimesisReflectionTool/` | Dev-time metadata / MelonLoader reflection |
| `deps/reference/` | Bootstrap game assemblies; `deps/decompiled/` for patch design (gitignored) |
| `dist/debug/`, `dist/prod/` | Build output (not committed) |
| [CUSTOM_GAME_MODELS.md](CUSTOM_GAME_MODELS.md) | Runtime OBJ meshes, materials, spawn patterns for mod world geometry |

## Architecture

```
Mod.cs (MelonMod entry)
  ├── ModConfig.Initialize / Changed
  ├── ModL10n.Initialize
  ├── Harmony → FeatureModules.All[].ApplyPatches
  ├── SyncFromConfig → FeatureModules.All[].SyncFromConfig (full or affected modules)
  ├── OnUpdate → modules; throttled modules batched on an interval
  ├── SaveSlotConfigLifecycle.Tick (sidecar retry load, session-end teardown)
  └── OnDeinitializeMelon → FeatureModules.All[].OnDeinitialize
```

Typical feature files under `Features/{FeatureName}/`:

```
Features/{FeatureName}/
├── {Feature}Config.cs          # config registration (where present)
├── {Feature}Patches.cs         # entry: Apply(Harmony) [+ RefreshFromConfig]
├── {Feature}Log.cs             # only when shared formatting is needed
├── Patches/                    # ALL [HarmonyPatch] classes, one file per patched game type
│   ├── GlobalUsings.cs
│   └── {GameType}Patches.cs
└── (resolvers/appliers/runtime/helpers at root; sub-feature folders keep theirs)
```

| Pattern | Role |
|---------|------|
| `{Feature}Patches.cs` | Harmony entry (`Apply(Harmony)`), uses `HarmonyPatchHelper.GetNamespacePatchTypes` |
| `Patches/*.cs` | Patch types in namespace `{Feature}.Patches` (standard for every feature) |
| `{Feature}Config.cs` | Registers `[MimesisPlayerEnhancement_{FeatureName}]`; properties live on `ModConfig` |
| `{Feature}Resolver.cs` / `{Feature}Applier.cs` | Optional — multiplier/value features |
| `{Feature}Runtime.cs` | Optional — session or per-frame state |
| `{Feature}Log.cs` | Optional — shared `ModLog` formatting (see [AGENTS.md](../AGENTS.md#logging)) |

Register every feature in `FeatureModules.All` (`Util/FeatureModule.cs`). Use `syncFromConfig` when toggling off must revert live state; `onUpdate` for per-frame work; `throttledUpdate: true` when `Mod.cs` should batch calls; `onDeinitialize` for flush/shutdown (e.g. sidecar or HTTP server cleanup).

## Add a new feature

1. Create `Features/{FeatureName}/` with `{Feature}Patches.cs` implementing `Apply(Harmony)`.
2. Add nested `[HarmonyPatch]` classes; use `HarmonyPatchHelper` for apply/audit (see `MorePlayersPatches.cs`).
3. Add `{Feature}Config.cs` (or wire entries in `ModConfig.Initialize`) with `Enable{FeatureName}` and options; expose properties on `ModConfig`.
4. Register in `FeatureModules.All`.
5. Gate host-only mutations with `HostApplyGate.ShouldApplyHostOnlyFeature()`.
6. For multipliers: resolver → applier, with `FeatureToggleGate` neutral values when disabled.
7. Log via `ModLog` and a local `Feature` const; add `{Feature}Log.cs` only when message formatting is reused.
8. Document keys in [CONFIG.md](CONFIG.md).
9. Run `./scripts/build.sh` (Debug; Release if build-sensitive).

## Config

- File: `UserData/MimesisPlayerEnhancement.cfg` (MelonPreferences TOML, separate from vanilla MelonLoader prefs).
- Global toggles/values: `[MimesisPlayerEnhancement]` and `[MimesisPlayerEnhancement_{FeatureName}]`.
- Per-save overrides: `SaveSlotConfigStore` runtime apply + `SaveSlotDocumentStore` (`MMGameData{N}.mpe-slot.sav`); loaded at save load, flushed on vanilla save.
- Runtime edits: `GlobalConfigStore` (global) or `SaveSlotConfigStore` (per slot); `ModConfig.Changed` triggers selective `SyncFromConfig`.
- **Scene-boundary apply:** DungeonRandomizer, DungeonTime, Economy, LootMultiplicator, and SpawnScaling read frozen config snapshots for the current scene. Mid-scene edits (e.g. web dashboard) defer until the scene ends (maintenance, tram, dungeon, or deathmatch). Turning a master `Enable*` toggle **off** still applies immediately. See `Util/SceneScopedConfigGate.cs`.

## Web dashboard (Svelte)

Built via Docker — no local Node.js required:

```bash
./scripts/build-webdashboard.sh          # Docker → Assets/WebDashboard/
SKIP_WEB_BUILD=true ./scripts/build.sh   # dotnet only when assets already built
```

See [src/MimesisPlayerEnhancementWeb/README.md](../src/MimesisPlayerEnhancementWeb/README.md).

## UI toolkit

Shared primitives in `src/MimesisPlayerEnhancement/Ui/`. Features compose these; Harmony wiring stays in the feature.

| Type | Role |
|------|------|
| **ModUiRoot** | Attach point (`UIManager.nodes[eUIHeight.Top]`) |
| **ModUiAssets** | Sprites/font/SFX/colors from vanilla prefabs (`TryCaptureFromMainMenu`) or `Fallback` |
| **ModPage** | Full-screen overlay (title, content, action/back bands) |
| **ModButton** | Styled button with TMP label and hover/click SFX |
| **ModScrollList** | Vertical scroll view; rows via factory into `Content` |
| **ModUiScrollForwarder** | Forwards wheel events to parent `ScrollRect` |
| **ModUiGameAccess** | Reflection helpers for `UIManager` / audio |
| **ModUiText / ModUiLayout / ModUiFactory** | TMP helpers, anchors, low-level creation |

```csharp
if (!ModUiAssets.TryCaptureFromMainMenu(mainMenu, loadTram, out ModUiAssets assets))
    assets = ModUiAssets.Fallback;

GameObject root = ModUiRoot.CreateUiRoot(ModUiRoot.GetTop()!, "MyFeatureUi");
ModPage page = ModPage.Create(root.transform, assets);
page.CreateTitle(assets, "My Feature");
ModButton.Create(page.CreateActionButtonRow(), assets, "Apply", expandWidth: true, () => MyFeatureApplier.Apply());
```

Reference: `Features/ExtendedSaveSlots/` (save slot picker).

## Localization

User-facing strings: `ModL10n.Get("key")` with `{named}` placeholders. Locale source JSON lives in [`l10n/`](../l10n/) (for example `en.json`, `de.json`); `./scripts/build.sh` stages them into `Assets/Locale/` before embed. Config registration resolves titles and descriptions from the same files — see [TRANSLATIONS.md](TRANSLATIONS.md). Pick locale via `GameLocaleAccess.GetCurrentLanguage()`.

## Per-save sidecars

Files beside vanilla saves: `MMGameData{N}.mpe-{kind}.sav` (see `SaveSidecarPaths`).

| Suffix | Feature | Runtime / flush |
|--------|---------|-----------------|
| `slot` | Unified mod document (lobby, settings profile, config overrides, player roster) | `SaveSlotDocumentStore`; `SaveSlotSidecarPersistence` |
| `stats` | Statistics | `StatisticsTracker`; `SaveSlotSidecarPersistence` |
| `speech` | Persistence (voice binary, MPEV) | `MimesisSaveManager` / `PersistenceWriteQueue` |

Account-wide: `MMGameData.mpe-quick-presets.sav` (quick settings preset catalog).

**Coordinated lifecycle** (`SaveSlotSidecarPersistence` — stats + slot document):

| Phase | Behavior |
|-------|----------|
| **Save load** | `GameSessionInfoLoadPatches` → `OnSaveSlotLoaded`; `SaveSlotConfigLifecycle` retries via `EnsureSaveSlotLoaded` if host was not ready |
| **Gameplay** | In-memory only; stores mark dirty on change |
| **Vanilla save** | `MaintenanceRoom.SaveGameData` success → `OnGameSaved` flushes dirty sidecars |
| **Session end** | `SessionJoined` false → `OnSessionEnded` clears runtime state, reloads global config (**no disk write**) |
| **Mod unload** | Statistics `onDeinitialize` → `FlushAllSync`; Persistence → `PersistenceWriteQueue.FlushAllSync` |

Persistence voice binary loads/saves through the Persistence feature (`SpeechEventPoolManager`, `MaintenanceRoom` save patch). Player voice IDs live in the slot document (`players[steamId].voiceId`). The web dashboard reads in-memory stores during gameplay; leaderboard JSON rebuilds when `StatisticsTracker.Revision` changes.

## Host-only and session access

- **HostApplyGate** — false for join-anytime participants and when the feature toggle is off; allows solo/host when network pdata is null.
- **GameSessionAccess** — save slot, session, and hub lookups shared across features.

Clients do not need this mod installed.

## Build and deploy

See [BUILD.md](BUILD.md) for bootstrap, compile, and copying the DLL into your game.

## Formatting

Style: `.editorconfig`. `./scripts/build.sh` runs `./scripts/format-code.sh` first (skip with `SKIP_FORMAT=true`). Verify only: `./scripts/format-code.sh --verify`. Direct `dotnet build` does not format.

Global usings: `src/MimesisPlayerEnhancement/GlobalUsings.cs` (`System.Collections.Generic`, `HarmonyLib`, `MimesisPlayerEnhancement.Util`). `System` is omitted — conflicts with `UnityEngine.Object` / `UnityEngine.Random`.
