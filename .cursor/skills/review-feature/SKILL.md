---
name: review-feature
description: >-
  Read-only audit of one MimesisPlayerEnhancement feature: lag, SSoT violations,
  layout compliance, dead code, lifecycle, conventions, simplification. Trigger:
  /review-feature {Name}, audit/health-check {Name}.
disable-model-invocation: true
---

# review-feature

READ-ONLY. No edits unless user asks to fix findings. Related: `test-feature` for regression tests.

## Scope

Resolve `{Name}` → `src/MimesisPlayerEnhancement/Features/{Name}/`, `FeatureModules.All` entry in `Util/FeatureModule.cs`, `{Name}Config.cs`/`ModConfig.cs`, web UI if any (`src/MimesisPlayerEnhancementWeb/`). Folder missing → list matches, stop.

Refs: `AGENTS.md`, `docs/DEVELOPMENT.md`, `.cursor/rules/*.mdc`.

## Steps

1. Inventory `.cs` files → patch|resolver|applier|runtime|config|log|service|UI
2. Layout vs standard tree
3. Hot paths: patches, `onUpdate`/`throttledUpdate`, UI loops, HTTP/async→main, session hooks
4. SSoT: map every `static`/cache/`Dictionary`/`_cached*` to canonical owner
5. Grep patterns (below)
6. Dead/legacy: unreferenced symbols, orphaned patches, `#if false`, undiscovered patch types
7. Simplify: smallest diff only, no rewrites

## Layout

```
Features/{Name}/
  {Name}Patches.cs          # Apply(Harmony), optional RefreshFromConfig
  {Name}Config.cs           # if options
  {Name}Log.cs              # only shared format/debug helpers
  Patches/GlobalUsings.cs + {GameType}Patches.cs  # one game type/file, Feature const if logs
  {Name}Resolver|Applier|Runtime.cs  # optional
```

Must: `FeatureModules.All` (hooks, `sessionScope`, `throttledUpdate`), `HarmonyPatchHelper.GetNamespacePatchTypes`+`ApplyPatchTypes`, `Enable{Name}`+`[MimesisPlayerEnhancement_{Name}]`, `docs/CONFIG.md` if new options, `internal`, ns `MimesisPlayerEnhancement.Features.{Name}`, `ModLog` not `MelonLogger`.

Gates: host mutations → `HostApplyGate.ShouldApplyHostOnlyFeature()`; disabled → `FeatureToggleGate` neutral + patch early-return + `SyncFromConfig` revert.

Scene-gated gameplay (DungeonRandomizer, DungeonTime, Economy, LootMultiplicator, SpawnScaling): read `SceneScopedConfigGate` snapshots — not live `ModConfig` mid-scene.

Layout violations: patches outside `Patches/` or undiscovered; logic in patches not resolver/applier/runtime; missing `FeatureModules` entry; `public` without need; deep service chains.

## SSoT

Rule: one canonical owner per domain. Others read/project — no parallel mutable copies.

| Domain | Owner | Forbidden duplicate |
|--------|-------|---------------------|
| session/slot/pdata | `GameSessionAccess` | raw `Hub.s`/vworld reflection |
| host/client | `HostApplyGate` | inline `ClientMode` (except JoinAnytime internals) |
| persisted names/voices | `SaveSlotDocumentStore` | local `Dictionary<ulong,string>` name maps |
| runtime player+stats | `PlayerRegistry` | parallel player dicts, cached name/stat |
| stat counters | `StatisticsTracker`/`PlayerRecord.Statistics` | local accumulators not synced |
| live actors (web) | `WebDashboardLiveRoster.Capture()` | per-service `GetProtoActorMap` walks |
| scene gameplay config | `SceneScopedConfigGate` | `ModConfig.Enable*`/multipliers mid-scene (gated modules) |
| live config | `ModConfig`/`GlobalConfigStore` | hardcoded defaults |
| per-save overrides | `SaveSlotConfigStore` | feature override dicts |
| slot document | `SaveSlotDocumentStore`+`SaveSlotSidecarPersistence` | second on-disk format |
| stat disk | `StatisticsStore`/`StatisticsWriteQueue` | sync `File.Write` |
| speech/voice disk | `SpeechEventPoolManager`/`MimesisSaveManager`/`PersistenceWriteQueue` | bypass pool |
| join/leave | `PlayerPresenceEvents`/`PlayerLifecycleCoordinator` | per-feature join tracking |
| strings | `ModL10n`/`l10n/*.json` | hardcoded UI (non-debug) |
| version | `VersionInfo` | local version strings |

SSoT detect: list feature-owned state → map entity → owner. Cached mutable copy without invalidation on `PlayerRegistry.Revision`/session end/`SyncFromConfig` = **High** if drift on join/leave/save/config/scene.

Grep: `GetProtoActorMap`, `JoinAnytimeHub.GetPdata` (outside roster/session helpers), `Dictionary<ulong`, `Dictionary<long`, `HashSet<ulong`, independent `DisplayName`/`SteamId`/`VoiceId` on DTOs, `ModConfig.Enable*` in gated gameplay patches, direct `File.`/`JsonSerializer`/sidecar paths.

Fix order: delete redundant store → derive snapshot (`WebDashboardLiveRoster`) → invalidate via revision/event → extract to `Util/`/`Features/Players/` only if ≥2 consumers. Never add sync-cache between two caches.

## Grep (feature folder)

`OnUpdate|Update|LateUpdate|FixedUpdate` · `FindObjectsOfType|FindObjectsOfTypeAll|GetComponentsInChildren` · `\.Where\(|\.Select\(|\.ToList\(|\.ToArray\(` · `lock` · `File\.|Stream` · `Thread\.Sleep` · `ModLog\.` (ungated in hot path) · `AccessTools|GetField|GetMethod|\.Invoke\(` · `new ` in loops · `$"`/concat in hot paths · `.ToList()` on dict iteration (alloc note)

## Lag (main thread)

Severity: Critical|High|Medium|Low|Info.

Signals: per-frame `onUpdate` w/o disabled early-exit · heavy Harmony on spawn/loot/move/UI · scene scans per frame · LINQ/allocs in patch/per-frame · ungated `ModLog` · sync I/O · O(n²) actor/room/loot · missing `throttledUpdate` for periodic work · HTTP thread→Unity w/o marshal · unbounded caches · `lock` on hot path.

Context: non-throttled modules every `OnUpdate`; throttled batch on `EncounterSpawnTiming.RetryIntervalSeconds` when SpawnScaling or LootMultiplicator enabled (`Mod.cs`).

## Other

Lifecycle: static cleared `onSessionEnded`/`onDeinitialize`; no stale `ProtoActor`/UI refs; sidecar via `SaveSlotSidecarPersistence`.

Correctness: cheap disable check first in patches; `SyncFromConfig` full revert; scene deferral respected; idempotent appliers.

Coupling: no other feature's private statics; shared player/session via `Features/Players/`/`GameSessionAccess`/`Util/`.

Harmony: confirm `deps/decompiled/`; `AccessTools` not strings; try/catch+`ModLog.Warn`; minimal transpilers.

Config: l10n for user-facing; `SaveSlotConfigStore` for slot keys; writes via queues.

C#: netstandard2.1, nullable, early return, `StringComparison.Ordinal`, no single-use abstractions.

## Output

```markdown
# Feature review: {Name}
Scope: {N} files | Registration: {hooks, sessionScope, throttled?} | Layout: {ok|N gaps}

## Summary
{≤3 sentences}

## Findings
| Sev | Cat | Location | Issue | Fix |
Cat: Lag|SSoT|Layout|Dead|Lifecycle|Convention|Simplify — sort Critical→Info

## SSoT map
| Entity | Owner | Feature usage | OK|DRIFT |

## Hot paths
{entry→work→freq}

## Layout gaps | Dead | Simplify | Positives | Next steps
```

Rules: every finding = `file:line` or symbol; confirmed vs suspected; SSoT drift in normal gameplay = High; no style nitpicks; `make check` only if asked.
