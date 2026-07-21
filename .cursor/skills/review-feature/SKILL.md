---
name: review-feature
description: >-
  Read-only audit of one feature: prod code (lag, SSoT, layout, lifecycle) + tests
  (PC/PL/CB coverage, quality). Trigger: /review-feature {Name}, audit {Name}.
disable-model-invocation: true
---

# review-feature

READ-ONLY. No edits unless user asks. Siblings: `test-feature` (fill gaps) · `extend-feature` (new sub-feature).
Refs: `AGENTS.md` · `docs/DEVELOPMENT.md` · `.cursor/rules/*.mdc`.

## Scope

`{Name}` → prod `Features/{Name}/` · tests `Tests/Features/{Name}/` (`Ui`→`UserInterface`) · `FeatureModules.All` · `{Name}Config`/`ModConfig` · web `MimesisPlayerEnhancementWeb/` if any. Missing folder → list matches, stop.

## Steps

1. Inventory prod `.cs` → patch|resolver|applier|runtime|config|log|UI
2. Inventory tests → PC|PL|CB|infra; build coverage map (`symbol→test|MISSING`)
3. Layout vs standard tree (both roots)
4. Hot paths: patches · `onUpdate`/`throttledUpdate` · UI loops · HTTP/async→main · session hooks
5. SSoT: map `static`/cache/`Dictionary`/`_cached*` → canonical owner
6. Grep (below) · dead/legacy · simplify (smallest diff)
7. Test gaps implying untestable prod shape → **Simplify** finding

## Layout

```
Features/{Name}/
  {Name}Patches.cs · {Name}Config.cs? · {Name}Log.cs? (shared format only)
  Patches/GlobalUsings.cs + {GameType}Patches.cs  # 1 game type/file
  {SubName}/*Resolver.cs? · {Name}Runtime.cs?
```

Must: `FeatureModules.All` · `HarmonyPatchHelper.GetNamespacePatchTypes`+`ApplyPatchTypes` · `Enable{Name}`+`[MimesisPlayerEnhancement_{Name}]` · `docs/CONFIG.md` if options · `internal` · ns `MimesisPlayerEnhancement.Features.{Name}` · `ModLog` not `MelonLogger`.

Gates: host→`HostApplyGate` · disabled→`FeatureToggleGate` neutral + patch early-return + `SyncFromConfig` revert.
Scene-gated (DungeonRandomizer,DungeonTime,Economy,LootMultiplicator,SpawnScaling): `SceneScopedConfigGate` snapshots — not live `ModConfig` mid-scene.

Violations: patches outside `Patches/` · undiscovered types · logic in patches not resolver/runtime · missing `FeatureModules` · needless `public` · deep service chains.

## Tests

| Check | How |
|-------|-----|
| Parity | `*Resolver`/`*Parser`/`*Applier`/math → `{Type}Tests.cs`? |
| PC | every `[HarmonyPatch]`+critical `AccessTools` → `{Name}PatchContractTests.cs` |
| CB | `*Config` bounds → `{Name}ConfigBoundsTests.cs` |
| Quality | `[Theory]` branches · edges (0,neg,disabled) · no live `ModConfig`/Unity/FishNet |
| Harness | `MimesisMetadataContext` pattern · no runtime traps (see `test-feature`) |
| Infra | shared fixtures in `Infrastructure/` only when reused |

Gaps → suggest `/test-feature {Name}`.

## SSoT

One owner per domain — others read/project, no parallel mutable copies.

| Domain | Owner | Forbidden |
|--------|-------|-----------|
| session/slot | `GameSessionAccess` | raw `Hub.s`/vworld |
| host/client | `HostApplyGate` | inline `ClientMode` (except JoinAnytime) |
| names/voices disk | `SaveSlotDocumentStore` | local `Dictionary<ulong,string>` |
| runtime players | `PlayerRegistry` | parallel dicts, cached name/stat |
| stat counters | `StatisticsTracker`/`PlayerRecord.Statistics` | local accumulators |
| live actors (web) | `WebDashboardLiveRoster.Capture()` | `GetProtoActorMap` walks |
| scene config | `SceneScopedConfigGate` | `ModConfig.Enable*` mid-scene |
| live config | `ModConfig`/`GlobalConfigStore` | hardcoded defaults |
| per-save | `SaveSlotConfigStore` | feature override dicts |
| slot doc | `SaveSlotDocumentStore`+`SaveSlotSidecarPersistence` | 2nd format |
| stat disk | `StatisticsStore`/`StatisticsWriteQueue` | sync `File.Write` |
| speech disk | `SpeechEventPoolManager`/`MimesisSaveManager`/`PersistenceWriteQueue` | bypass pool |
| join/leave | `PlayerPresenceEvents`/`PlayerLifecycleCoordinator` | per-feature tracking |
| strings | `ModL10n`/`l10n/*.json` | hardcoded UI |
| version | `VersionInfo` | local strings |

Detect: feature state → entity → owner. Cache w/o invalidation on `PlayerRegistry.Revision`/session/`SyncFromConfig` = **High** if drift on join/leave/save/config/scene.
Grep: `GetProtoActorMap` · `JoinAnytimeHub.GetPdata` · `Dictionary<ulong` · `HashSet<ulong` · `DisplayName`/`SteamId`/`VoiceId` on DTOs · `ModConfig.Enable*` in gated patches · direct `File.`/`JsonSerializer`.
Fix: delete redundant → derive snapshot → invalidate via revision/event → `Util/`/`Players/` only if ≥2 consumers. Never sync-cache between caches.

## Grep · Lag · Other

Grep: `OnUpdate|Update` · `FindObjectsOfType` · `\.Where\(|\.Select\(|\.ToList\(` · `lock` · `File\.` · `ModLog\.` (ungated hot path) · `AccessTools|\.Invoke\(` · `new ` in loops.

Lag (Critical→Info): per-frame `onUpdate` w/o early-exit · heavy Harmony on spawn/loot/UI · scene scans · LINQ in hot path · sync I/O · O(n²) · missing `throttledUpdate` · unbounded caches · `lock` on hot path. Throttled batch: `EncounterSpawnTiming.RetryIntervalSeconds` when SpawnScaling/LootMultiplicator on (`Mod.cs`).

Lifecycle: statics cleared `onSessionEnded`/`onDeinitialize` · no stale refs · sidecar via `SaveSlotSidecarPersistence`.
Correctness: disable check first · `SyncFromConfig` revert · scene deferral · idempotent appliers.
Harmony: `deps/decompiled/` · `AccessTools` not strings · try/catch+`ModLog.Warn`.
C#: netstandard2.1 · nullable · early return · no single-use abstractions.

## Output

```markdown
# Feature review: {Name}
Scope: {N} prod, {M} test | Registration: {hooks,sessionScope,throttled?} | Layout: {ok|gaps}

## Summary
{≤3 sentences}

## Findings
| Sev | Cat | Location | Issue | Fix |
Cat: Lag|SSoT|Layout|Dead|Lifecycle|Convention|Simplify|Tests|Coverage — Critical→Info

## Tests
Coverage: {symbol→test|MISSING}
| Sev | Cat | Location | Issue | Fix |
Gaps → `/test-feature {Name}`

## SSoT map
| Entity | Owner | Usage | OK|DRIFT |

## Hot paths
{entry→work→freq}

## Layout | Dead | Simplify | Positives | Next steps
```

Rules: finding=`file:line` or symbol · confirmed vs suspected · SSoT drift in gameplay=High · no style nits · `make check` only if asked.
