---
name: test-feature
description: >-
  Regression tests for one feature: patch contracts vs game DLLs, pure logic,
  config bounds, blast-radius. Bootstraps xUnit harness if missing.
  Trigger: /test-feature {Name}, test {Name}.
disable-model-invocation: true
---

# test-feature

Lock **functions** (resolvers, parsers, math) and **game connections** (patch/`AccessTools` targets) in `make test`. Complements `review-feature`.

Refs: `AGENTS.md`, `deps/decompiled/`, `deps/reference/Managed/`.

## Priority

1. **PatchContract** — every `[HarmonyPatch]` + critical `AccessTools` in feature → `{Name}PatchContractTests.cs`
2. **PureLogic** — `[Theory]` tables; inject `bool`/`*SceneConfig` overload — never live `ModConfig`
3. ConfigSanitize / blast-radius `Util/` / playtest gaps (list only)

Scope: `Features/{Name}/`, `FeatureModules.All`, config. `Ui` → `UserInterface`. Patches registered elsewhere (e.g. `UiPatches`) still get contracts under `Features/{Name}/`.

## Steps

Resolve → harness exists? reuse `src/MimesisPlayerEnhancement.Tests/` : bootstrap → classify `.cs` → **contracts** (grep `HarmonyPatch`/`AccessTools`, confirm in `deps/decompiled/`, assert via `MimesisMetadataContext`) → pure logic → blast radius → `make test` full suite → report.

## Classify

| Tag | Test |
|-----|------|
| PatchContract | type/method/field in Managed metadata |
| PureLogic | inputs → outputs |
| ConfigSanitize | `*Config` bounds |
| UntestableRuntime | Unity, FishNet, UI, disk, MelonPreferences → playtest |

Scene-gated features: `*SceneConfig` snapshots. Disabled gates: `FeatureToggleGate.NeutralMultiplier`.

## Contracts (detect broken patches)

Metadata-only. Pattern: `DungeonRoomPatchContractTests.cs` — `ManagedAssemblyPaths.Resolve()` → `context.RequireType("T")` → `GetMethod`/`GetField`.

Assert: each Harmony target + `AccessTools`/`TypeByName` + reflection chains (each hop).

MetadataLoadContext: no runtime `typeof()` in `GetMethod`; compare types via `.Name`; `PlatformMgr.Load` is instance generic.

Red after game update → fix patch / refresh decompile — **never** delete contract. Inspect: `dotnet run --project src/MimesisInspectionTool -- member T M`.

## Pure logic

Pattern: `DungeonTimeResolverTests.cs`. Test DTOs/resolvers, not `MMSaveGameData`. One injectable overload if `ModConfig` blocks. Don't mock Unity/Harmony bodies.

## Runtime traps

Bootstrap `deps/reference/Managed/` ≈ 12 DLLs — not full game. Avoid: `new MMSaveGameData`/`SaveSlotEntry`, `SaveSlotDocumentStore`/`PlatformMgr`/`Hub` invoke, copying `MelonLoader.dll` (hangs vstest) or full Managed folder.

## Harness (first use)

`MimesisPlayerEnhancement.Tests/` net10.0 xUnit, `Infrastructure/`, `Features/{Name}/`, `Util/`. `InternalsVisibleTo` on mod. `make test` + `make deps`. Paths: `{Type}Tests.cs`, `{Name}PatchContractTests.cs`.

## Output

```markdown
# Feature tests: {Name}
Harness: exists|bootstrapped | Files: N new, M updated
## Coverage | New tests | Contracts | Blast radius | Playtest gaps | Risk
```

Characterize behavior; smallest seam; `make test` before done.
