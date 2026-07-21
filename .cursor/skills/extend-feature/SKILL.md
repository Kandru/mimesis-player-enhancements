---
name: extend-feature
description: >-
  Add sub-feature to existing feature: resolver-first design, gates, patches,
  PC/PL/CB tests, early-exit. Trigger: /extend-feature {Name} {intent}.
disable-model-invocation: true
---

# extend-feature

Develop sub-feature under parent `{Name}` from user `{intent}`. Siblings: `test-feature` (coverage) · `review-feature` (audit after).
Refs: `AGENTS.md` · `docs/DEVELOPMENT.md` · `deps/decompiled/` · `.cursor/rules/*.mdc`.

## Input

Parent `{Name}` + `{intent}` (behavior, config keys, patch targets). Vague → 1 focused question, proceed.

## Design (before code)

Minimal vertical slice → resolver-first? patches unavoidable? → gates → implement → PC+PL+CB → `make check`+`make test`.

| Decision | Default |
|----------|---------|
| Location | `Features/{Name}/{SubName}/*Resolver.cs` (pattern: `MimicTuning/MimicPossession/`) |
| Patches | shared `Patches/{GameType}Patches.cs` — confirm `deps/decompiled/` |
| Module | reuse parent `{Name}Patches`/`{Name}Runtime` — **no** new `FeatureModules.All` unless independent |
| Config | extend `{Name}Config`+`ModConfig`+`docs/CONFIG.md` |
| Disable | `FeatureToggleGate` neutral + patch early-return + `SyncFromConfig` revert |
| Host | `HostApplyGate` for mutations |
| Scene | `SceneScopedConfigGate` if gameplay-affecting (see gated modules) |
| Session | clear statics `onSessionEnded`/`onDeinitialize` |

## Implement (testability-first)

1. Resolver+types (pure, early-exit)
2. Config registration + CB test
3. Patches thin: gate→resolver→apply
4. PC for new Harmony/`AccessTools` targets
5. PL for resolver branches (`[Theory]`)
6. Wire runtime/hooks on parent module if needed
7. `make check` · `make test` · list UR playtest gaps

## Constraints

- Logic in resolver not patch · injectable overloads for test · no live `ModConfig` in tests
- `internal` · ns `MimesisPlayerEnhancement.Features.{Name}` · `ModLog` · try/catch in patches
- No game runtime tests · no web UI unless intent requires · no new top-level feature folder
- SeamNeeded during impl → see `test-feature` refactor rules

## Output

```markdown
# Extend {Name}: {SubName}
Slice: {1-line} | Files: +N | Tests: +M | Playtest: {UR gaps}

## Design
{resolver vs patches · gates · config keys}

## Changes
| Path | Role |

## Tests
PC: … | PL: … | CB: …

## Config
{new keys → docs/CONFIG.md}

## Verify
make check: … | make test: …
```

Then `/test-feature {Name}` or `/review-feature {Name}` if gaps remain.
