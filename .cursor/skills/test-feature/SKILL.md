---
name: test-feature
description: >-
  Author regression tests for one MimesisPlayerEnhancement feature: pure logic,
  config sanitize, Harmony patch contracts vs game DLLs, blast-radius on shared
  Util/Players. Bootstraps xUnit harness on first use. Trigger: /test-feature
  {Name}, test {Name}, add tests for {Name}.
disable-model-invocation: true
---

# test-feature

Writes tests. Bootstraps harness if missing. Complements read-only `review-feature`.

Refs: `AGENTS.md`, `docs/DEVELOPMENT.md`, `.cursor/rules/*.mdc`, `deps/decompiled/`, `deps/reference/Managed/`.

## Scope

Resolve `{Name}` → `src/MimesisPlayerEnhancement/Features/{Name}/`, `FeatureModules.All` in `Util/FeatureModule.cs`, `{Name}Config.cs`/`ModConfig.cs`, web UI if any. Aliases: `Ui` → `UserInterface`. Folder missing → list matches, stop.

## Steps

1. **Resolve** scope (above).
2. **Harness** — if `src/MimesisPlayerEnhancement.Tests/` missing → bootstrap (below), then continue.
3. **Classify** each `.cs` in feature folder (see Classify).
4. **Mimesis contracts** — PatchContract: confirm target in `deps/decompiled/`, assert type/method resolvable via MetadataLoadContext on `deps/reference/Managed/*.dll` (reuse `MimesisInspectionTool` pattern or shared `Infrastructure/` helper).
5. **Author** tests — characterize **current** behavior; inject config structs/overloads only when needed for PureLogic.
6. **Blast radius** — if change touches `Util/`, `Features/Players/`, or shared API: grep consumers; add/update characterization tests in other features' test folders.
7. **Run** `make test` — full suite, not feature filter; fix until green.
8. **Report** (template below).

## Classify

| Tag | Target | Test |
|-----|--------|------|
| PureLogic | resolver/applier/parser/math | unit: inputs → outputs |
| ConfigSanitize | clamp/parse/default in `*Config` | boundary + invalid input |
| PatchContract | `[HarmonyPatch]`, `TargetMethod(s)`, `AccessTools` | type/method exists in Managed |
| UntestableRuntime | Unity scene, FishNet, UI mutation, MelonPreferences I/O | playtest bullet only |

Scene-gated (DungeonRandomizer, DungeonTime, Economy, LootMultiplicator, SpawnScaling): pass `*SceneConfig` snapshots — not live `ModConfig`.

Gates in PureLogic: disabled → `FeatureToggleGate.NeutralMultiplier`; host-only predicates testable without Unity.

## Do / Don't

**Do:** multipliers, parsers, filters, scaling tables, disabled→neutral, seed/list parsing, patch target resolution.

**Don't:** Harmony postfix bodies w/ mocked Unity; visual UI; network; locale keys (`make check`/`validate-locale` owns that); delete failing contracts after game patch.

**Shared:** `ScalingMath`, gates, parsers → one test file under `Util/`; feature tests assert via feature resolvers.

## Harness (first use only)

Create `src/MimesisPlayerEnhancement.Tests/`:

```
MimesisPlayerEnhancement.Tests.csproj   # net10.0, xUnit, Microsoft.NET.Test.Sdk
Infrastructure/                         # MetadataLoadContext, Managed paths
Features/{Name}/                        # mirror mod feature tree
Util/                                   # shared math/parser tests (once)
```

- `ProjectReference` mod with `-p:SkipWebBuild=true`
- `InternalsVisibleTo("MimesisPlayerEnhancement.Tests")` on mod assembly
- Package versions in `Directory.Packages.props` (add xUnit pins)
- Add to `src/MimesisPlayerEnhancement.sln`
- `make test` target: Docker `mcr.microsoft.com/dotnet/sdk:10.0`, `DOCKER_NUGET_CACHE`, `dotnet test` — separate from `make check`
- Managed DLL paths: `deps/reference/Managed/` (same as mod `GameAssemblyPath`)

## Paths

- Feature: `src/MimesisPlayerEnhancement.Tests/Features/{Name}/{Type}Tests.cs`
- Contracts: `.../Features/{Name}/{GameType}PatchContractTests.cs`
- Shared: `src/MimesisPlayerEnhancement.Tests/Util/{Type}Tests.cs`

Naming: `{ClassUnderTest}Tests`, `[Fact]` per behavior, theory for tables.

## Blast radius

When editing or testing shared code:

1. Grep symbol across `src/MimesisPlayerEnhancement/`
2. List consumer features
3. Add/update tests in each consumer's `Features/{Consumer}/` folder
4. Run full `make test`

Goal: Util tweak cannot silently change Economy vs SpawnScaling vs Loot.

## Drift

Red after game update = update patches or refresh `deps/decompiled/` — **never** delete contract to green. Message: "update patch or decompile".

## Output

```markdown
# Feature tests: {Name}
Harness: {exists|bootstrapped} | Files: {N} new, {M} updated

## Coverage
| File | Classify | Tests |
PureLogic|ConfigSanitize|PatchContract|UntestableRuntime

## New tests
- `{path}` — {what locked}

## Contracts
| Patch | Target | Status |

## Blast radius
{consumers touched + tests added}

## Playtest gaps
{UntestableRuntime bullets}

## Risk
{residual untested behavior}
```

Rules: characterize behavior not implementation; smallest injectable overload; `make test` before done; `make check` only if user asks.
