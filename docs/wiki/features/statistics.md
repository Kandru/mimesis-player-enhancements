# Statistics

Tracks per-player statistics for each save: combat by enemy type, trap deaths, train value (credited to the first player who picked up an item), dungeon exit outcomes, median life time on death, and a weighted team value score for ranking. Only the host must enable Statistics for the whole lobby to get the effect — joiners do not need the mod or this setting. Stats are kept in three layers: **current run** (resets when the party restarts at zone 1), **all-time totals** (survive run restarts), and **session** (connect/disconnect with a grace window).

## Configuration

TOML section: [`MimesisPlayerEnhancement_Statistics`](../CONFIG.md#statistics--mimesisplayerenhancement_statistics). The game reloads the config while running — no restart needed. Unset keys use the code defaults below.

### `EnableStatistics`

Master switch for statistics tracking on the host. When turned off, open sessions are finalized and tracking stops. Applies to the whole lobby; only the host needs this enabled.

| Value | Meaning |
|---|---|
| `true` | Track player stats per save game (default). |
| `false` | Stop tracking; finalize and clear runtime state. |

Default: `true`

### `SessionReconnectGraceMinutes`

If someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh. Values below `1` are reset to `1`.

| Value | Meaning |
|---|---|
| integer ≥ `1` | Grace period in **minutes** before a disconnected session is treated as ended. |

Default: `5`

### `ShowStatisticsToasts`

Shows statistics messages in the bottom-left corner — session intro for you, global stats on join/leave. Does not replace the game's own connect messages. Requires `EnableStatistics`.

| Value | Meaning |
|---|---|
| `true` | Show statistics toasts (default). |
| `false` | Disable statistics toasts only; tracking continues if `EnableStatistics` is on. |

Default: `true`
