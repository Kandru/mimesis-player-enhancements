---
name: test-feature
description: >-
  Tests + coverage-driven refactors for one feature: PC/PL/CB, minimal prod seams.
  Bootstraps xUnit if missing. Trigger: /test-feature {Name}, test {Name}.
disable-model-invocation: true
---

# test-feature

Lock functions (resolvers,parsers,math) + game connections (patch/`AccessTools`) in `make test`. May refactor prod for seams. Siblings: `review-feature` (audit) · `extend-feature` (new sub-feature).
Refs: `AGENTS.md` · `deps/decompiled/` · `deps/reference/Managed/` · `docs/DEVELOPMENT.md`.

## Workflow

`resolve → inventory(prod+tests) → gap map → [refactor?] → tests → make test → report`

Scope: `Features/{Name}/` · `Tests/Features/{Name}/` · `FeatureModules.All` · config. `Ui`→`UserInterface`. Patches elsewhere (e.g. `UiPatches`) → contracts under feature test folder.

## Gap map

| Tag | Action |
|-----|--------|
| PC | add/extend `{Name}PatchContractTests.cs` |
| PL | add `{Type}Tests.cs` `[Theory]` |
| CB | add `{Name}ConfigBoundsTests.cs` |
| UR | playtest gap only |
| SeamNeeded | minimal prod refactor → PL/CB |

Scene-gated: `*SceneConfig` snapshots. Disabled: `FeatureToggleGate.NeutralMultiplier`.

## Refactor (SeamNeeded)

**Allowed** (behavior-preserving, smallest diff):
- Extract patch logic → `*Resolver`/helper (root or subfolder)
- Injectable overload: `bool enabled` · `*SceneConfig` · explicit params — never live `ModConfig` in tests
- Early-return guard top of hot path
- Move string/format/math out of Harmony bodies

**Forbidden**: behavior change · new SSoT cache · single-use abstraction · mock Unity/Harmony/patch bodies · delete PC after game update (fix patch instead).

**Before refactor**: SSoT/lifecycle check (`review-feature` rules). Drift risk → fix arch first or stop.

## PC

Metadata-only via `MimesisMetadataContext`. `ManagedAssemblyPaths.Resolve()` → `RequireType` → `GetMethod`/`GetField`. Assert every `[HarmonyPatch]`+`AccessTools`/`TypeByName`+reflection chain hop. No runtime `typeof()` in `GetMethod`; compare via `.Name`. Red after game update → fix patch/decompile, never delete contract. Inspect: `dotnet run --project src/MimesisInspectionTool -- member T M`.

## PL · CB

PL pattern: `DungeonTimeResolverTests.cs` — DTOs/resolvers, not `MMSaveGameData`. One injectable overload if `ModConfig` blocks.
CB: bounds/sanitize on `*Config` properties.

## Runtime traps

`deps/reference/Managed/` ≈12 DLLs. Avoid: `MMSaveGameData`/`SaveSlotEntry` · `SaveSlotDocumentStore`/`PlatformMgr`/`Hub` invoke · `MelonLoader.dll` (hangs vstest) · full Managed copy.

## Harness (first use)

`MimesisPlayerEnhancement.Tests/` net10.0 xUnit · `Infrastructure/` · `Features/{Name}/` · `Util/`. `InternalsVisibleTo` on mod. `make test`+`make deps`.

## Output

```markdown
# Feature tests: {Name}
Harness: exists|bootstrapped | Files: +N ~M | make test: pass|fail

## Coverage
{symbol→test|MISSING|refactored}

## Refactors
| File | Change | Tests unlocked |

## New tests | Contracts | Blast radius | Playtest (UR) | Risk
```

Characterize behavior · smallest seam · `make test` before done · then `/review-feature {Name}` optional.
