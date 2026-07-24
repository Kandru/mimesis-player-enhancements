# Dungeon Time

Extends the real dungeon shift clock when more players are present than a baseline. Only the host needs to turn this on; once enabled, everyone in the run gets the longer shift. Applied once when all members have entered the dungeon room. Useful so larger groups get a fairer window to finish a run.

## Configuration

Section: `[MimesisPlayerEnhancement_DungeonTime]`. Changes during an active gameplay scene are held until that scene ends (turning the feature **off** still applies immediately). Missing keys use the defaults below.

### `EnableDungeonTime`

Master switch on the host's config. When the host leaves this off, no bonus time is added for the session. Joining players do not need their own copy enabled.

| Value | Meaning |
|---|---|
| `true` | Host enables extra shift time for the whole party when above baseline |
| `false` | No extra time |

Default: `false`

### `DungeonTimeBaselinePlayerCount`

Player count at or below which no extra time is added. Vanilla party size is 4. Values below 1 are rejected and reset to 1.

| Value | Meaning |
|---|---|
| `1`… | Minimum allowed; each player above this count can earn bonus seconds |
| `4` | Typical baseline — matches a full vanilla squad |

Default: `4`

### `ExtraShiftSecondsPerPlayerAboveBaseline`

Real seconds added to the shift deadline for each player above the baseline. Example: baseline 4, this value 10, and 6 players → +20 seconds. `0` disables the bonus while leaving the feature enabled. Negative values are rejected and reset to 0.

| Value | Meaning |
|---|---|
| `0` | No bonus seconds (even if enabled and above baseline) |
| `> 0` | Seconds per extra player (fractions allowed) |

Default: `10.0`
