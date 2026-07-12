# Dungeon Time

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_DungeonTime`](../CONFIG.md#dungeon-time--mimesisplayerenhancement_dungeontime)

Adds extra time to each dungeon shift when you have more players than a baseline count. For example, with default settings, every player above four adds ten seconds to the clock. Gives bigger groups a fairer window to finish a run.

## Baseline and bonus

When a dungeon shift starts (all members entered), the real shift deadline extends by `ExtraShiftSecondsPerPlayerAboveBaseline` for each player above `DungeonTimeBaselinePlayerCount`.

Applied once per dungeon room — late [Join Anytime](./join-anytime.md) arrivals do not add more time.

Value changes during an active gameplay scene are deferred until that scene ends (same as Spawn Scaling, Economy, etc.).

**Full config keys →** [Dungeon Time](../CONFIG.md#dungeon-time--mimesisplayerenhancement_dungeontime)
