# Mimic Tuning

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_MimicTuning`](../CONFIG.md#mimic-tuning--mimesisplayerenhancement_mimictuning)

Tune mimic behavior on the host: how often mimics replay archived player voices, which player inventory they copy for decoy loadouts, and — when you are dead — how long each **E** possession through a mimic lasts and how long you wait before the next one.

Off by default. Changes take effect on the next mimic voice attempt, inventory copy, or E-possession — no restart required. Already-playing audio, cloned inventories, and active possession sessions are not reverted.

With `EnableMimicTuning = true`, archived mimic voice lines are stopped when a mimic enters possessed state and blocked from replaying while possessed (local audio cleanup during E-possession).

## Voice tuning

`MimicVoiceTuningMode`: `Vanilla` uses game timing; `Custom` applies the response keys below.

- `PeriodicVoiceIntervalMultiplier` — scales ambient mimic voice cooldown (`0.5` ≈ twice as chatty).
- `PlayerVoiceResponseChancePercent` — chance a nearby mimic replays a line after a player speaks.
- `PlayerVoiceResponseCooldownSeconds` — minimum seconds between mimic reactions (vanilla is 3).
- `PlayerVoiceResponseDelayMinSeconds` / `MaxSeconds` — random pause before a mimic replies.
- `PlayerVoiceResponseMaxDistance` — max range for mimics to react (vanilla is 20 m).

## Inventory copy

`MimicInventoryCopyMode`: `Vanilla` uses behavior-tree pick rules; `Custom` forces `MimicInventoryCopyPickRule`:

- `MinDistance` — copy nearest player's inventory.
- `MaxDistance` — copy farthest player's inventory.
- `Random` — random player pick.

## Possession timing

When dead and pressing **E** to speak through a mimic, vanilla uses a fixed 12-second speak window and fixed cooldown. `EnableMimicPossessionTuning` (requires master toggle) enables:

- `RandomizeMimicPossessionDuration` — roll speak duration between min/max seconds per possession.
- `MimicPossessionCooltimeMultiplier` — post-possession cooldown multiplier (`1` = vanilla).

**Full config keys →** [Mimic Tuning](../CONFIG.md#mimic-tuning--mimesisplayerenhancement_mimictuning)
