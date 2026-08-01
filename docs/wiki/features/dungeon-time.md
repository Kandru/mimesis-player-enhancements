# Dungeon Time

**Scope:** host

**Host only** — only the host must enable this for the whole lobby to get the effect. Joining clients do not need the mod.

Extends the real dungeon shift clock when more players are present than a baseline, and optionally overrides the synced in-game start hour / tram console clock. Once enabled, everyone in the run gets the longer shift and any clock overrides. Shift extension is applied once when all members have entered the dungeon room. Useful so larger groups get a fairer time window to finish a run.

When a start-time preset is set, real shift length is rescaled so the in-game day still runs from that start until ~24:00 (earlier starts get more real time; later starts get less). Player-count bonus seconds then stack on top of that adjusted duration. When time is extended by the player bonus, the host also slows the in-game day clock (tram/alarm) so start→24:00 still fills the longer real shift instead of running past the usual end and counting up.

## Configuration

Section: `[MimesisPlayerEnhancement_DungeonTime]`. Most changes during an active gameplay scene are held until that scene ends (turning the feature **off** still applies immediately). `EnableRealtimeTramClock` applies immediately. Missing keys use the defaults below.

> **Moved from Weather (breaking):** `StartTimePreset` and `EnableRealtimeTramClock` used to live under `[MimesisPlayerEnhancement_Weather]`. Re-set them here — old Weather keys are not migrated.

### `EnableDungeonTime`

Master switch on the host's config. When the host leaves this off, no bonus time is added and start-time / tram-clock overrides do not run. Joining players do not need their own copy enabled.

| Value | Meaning |
|---|---|
| `true` | Host enables extra shift time (when above baseline) and clock options for the whole party |
| `false` | No extra time / no clock overrides |

Default: `false`

### `DungeonTimeBaselinePlayerCount`

Player count at or below which no extra time is added. Vanilla party size is 4. Values below 1 are rejected and reset to 1.

| Value | Meaning |
|---|---|
| `1`… | Minimum allowed; each player above this count can earn bonus seconds |
| `4` | Typical baseline — matches a full vanilla squad |

Default: `4`

### `ExtraShiftSecondsPerPlayerAboveBaseline`

Real seconds added to the shift deadline for each player above the baseline. Example: baseline 4, this value 10, and 6 players → +20 seconds. `0` disables the bonus while leaving the feature enabled (so start-time / tram clock can still run). Negative values are rejected and reset to 0.

| Value | Meaning |
|---|---|
| `0` | No bonus seconds (even if enabled and above baseline) |
| `> 0` | Seconds per extra player (fractions allowed) |

Default: `10.0`

### `StartTimePreset`

Sets the **synced in-game clock** when a dungeon starts (tram alarm and outdoor lighting) and **rescales real shift length** so the run still ends near ~24:00. Earlier starts (e.g. Morning) add real time; later starts (e.g. Night) shorten it. Player-count bonus seconds (above) still stack on top. Sunrise ~06:00, sunset ~18:00.

| Value | Clock at start | Lighting |
|---|---|---|
| `Vanilla` | ~10:00 (from dungeon data) | Bright daytime (default) |
| `Morning` | 08:00 | Bright morning |
| `Noon` | 12:00 | Bright midday |
| `Dusk` | 18:00 | Sunset / dim |
| `Night` | 21:00 | Dark (moonlit) |
| `Midnight` | 00:00 | Darkest at start |

Invalid values reset to `Vanilla`. Case-insensitive. Requires `EnableDungeonTime`.

Default: `Vanilla`

### `EnableRealtimeTramClock`

Vanilla only updates the tram console clock when the in-game **hour** changes (~once per real minute at default time scale), so the display shows `HH:00` until the next hour. When enabled, the host syncs every in-game **minute** instead (~once per real second at default scale). Weather and lighting still change on hour boundaries only.

Turning it **off** mid-run snaps the tram/alarm **display** to `HH:00` (one floored `TimeSyncSig`) without changing dungeon elapsed or start time. Progression keeps running; only the shown minutes reset until the next vanilla hourly sync.

| Value | Meaning |
|---|---|
| `false` | Hourly tram clock updates (default) |
| `true` | Minute-level tram clock updates during dungeon runs |

Requires `EnableDungeonTime`. Applies **immediately** when toggled (not held until scene end).

Default: `false`
