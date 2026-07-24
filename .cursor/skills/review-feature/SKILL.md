---
name: review-feature
description: >-
  Read-only feature audit: prod lag/SSoT/layout/lifecycle, tests PC/PL/CB, game@ refs.
  Trigger: /review-feature {Name}, audit {Name}.
disable-model-invocation: true
---

# review-feature

READ-ONLY. No edits unless user asks. Siblings: `test-feature` · `extend-feature`. Refs: `AGENTS.md` · `test-feature`.

## Scope

`{Name}` → `Features/{Name}/` · `Tests/Features/{Name}/` (`Ui`→`UserInterface`) · `FeatureModules.All` · `{Name}Config`/`ModConfig` · web if any. Missing → list matches, stop.

## Pipeline

`inv prod→inv tests→cov map→layout→hot paths→SSoT→game@ resolve→grep→dead/simplify→report`

Layout: `{Name}Patches·Config?·Log? · Patches/{GameType} · {Sub}/*Resolver? · Runtime?` — gates/harmony/logging → `AGENTS.md`. Scene-gated: DungeonRandomizer,DungeonTime,Economy,LootMultiplicator,SpawnScaling.

## game@

Resolve `deps/decompiled/{ver}/`; `{ver}`=`VersionInfo.GameVersion` (else sole/latest dir). **Cite only** (omit `deps/decompiled/<version>/`):

`game@{ver} {asmRel}:L{start}-{end}`

Ex: `game@0.3.1 Assembly-CSharp/Mimic.Voice.SpeechSystem/SpeechEventArchive.cs:L120-145`

Per `[HarmonyPatch]`/critical `AccessTools`: open decompiled method → record span. Patch: `// game@…` above type/method. Multi-site → one line each; prefer method span.

Convention (Info→Med): missing `// game@`, wrong ver, or path still has `deps/decompiled/`.

## Tests

PC/PL/CB parity (`test-feature` tags): `*Resolver`/`*Parser`/`*Applier`/math→`{Type}Tests` · every patch/`AccessTools`→`{Name}PatchContractTests` · `*Config` bounds→`{Name}ConfigBoundsTests`. Gaps→`/test-feature {Name}`. Untestable prod shape→**Simplify** finding.

## SSoT

`session→GameSessionAccess · host→HostApplyGate · names/voices disk→SaveSlotDocumentStore · players→PlayerRegistry · stats→StatisticsTracker · web actors→WebDashboardLiveRoster.Capture · scene cfg→SceneScopedConfigGate · live cfg→ModConfig · per-save→SaveSlotConfigStore · slot doc→SaveSlotDocumentStore+Sidecar · stat disk→StatisticsStore/WriteQueue · speech→SpeechEventPoolManager/MimesisSaveManager/PersistenceWriteQueue · join/leave→PlayerPresenceEvents/LifecycleCoordinator · l10n→ModL10n · ver→VersionInfo`

Drift **High**: cache w/o invalidate on `PlayerRegistry.Revision`|session|`SyncFromConfig` when join/leave/save/cfg/scene. Fix: delete redundant→derive snapshot→invalidate via revision/event.

## Grep · Rubric

Grep: `OnUpdate|Update` · `FindObjectsOfType` · `\.Where\(|\.Select\(|\.ToList\(` · `lock` · `File\.` · `ModLog\.` · `AccessTools|\.Invoke\(` · `new ` in loops · `GetProtoActorMap` · `JoinAnytimeHub.GetPdata` · `Dictionary<ulong` · `HashSet<ulong` · `DisplayName`/`SteamId`/`VoiceId` · `ModConfig.Enable*` in gated patches · `JsonSerializer`.

Rubric Critical→Info: per-frame `onUpdate` w/o early-exit · heavy Harmony spawn/loot/UI · scene scans · LINQ hot path · sync I/O · O(n²) · missing `throttledUpdate` · unbounded caches · `lock` hot path · statics not cleared `onSessionEnded`/`onDeinitialize` · disable check first · `SyncFromConfig` revert · scene deferral · idempotent appliers. Throttle hint: `EncounterSpawnTiming.RetryIntervalSeconds` when SpawnScaling/LootMultiplicator on (`Mod.cs`).

## Output

```markdown
# Feature review: {Name}
Scope: {N}p/{M}t | reg:{hooks} | layout:{ok|gaps}

## Summary
{≤3 sent}

## Game refs
| Patch | Game |
| … | game@{ver} {rel}:L-L |

## Findings
| Sev | Cat | Loc | Issue | Fix |
Cats: Lag|SSoT|Layout|Dead|Lifecycle|Convention|Simplify|Tests|Coverage

## Tests
{symbol→test|MISSING} · gaps→/test-feature

## SSoT
| Entity | Owner | OK|DRIFT |

## Hot
{entry→work→freq}

## Next
{optional one line}
```

Rules: `Loc=file:line|symbol|game@…` · confirmed≠suspected · gameplay SSoT drift=High · no style nits · `make check` iff asked.
