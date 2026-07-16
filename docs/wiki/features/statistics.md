# Statistics

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Statistics`](../CONFIG.md#statistics--mimesisplayerenhancement_statistics)

Tracks per-player and per-save statistics: combat breakdowns by enemy type, trap deaths, train value (credited to the first player who picked up an item), dungeon exit outcomes, median life time on death, and a weighted **team value** score for ranking.

## Layers

- **Current run** — wiped when the party restarts at zone 1; drives the web dashboard leaderboard and per-zone sections.
- **All-time totals** — survive run restarts (`Global` counters and `RunRestarts` counter).
- **Session** — connect/disconnect grace window (`SessionReconnectGraceMinutes`).

## Web dashboard

The **Statistics** page shows server summary cards, a ranked player table (sorted by team value score), and per-zone accordion sections (current zone expanded first). Player detail pages show current-run cards, all-time totals, localized kill/death lines, and zone breakdowns.

## Statistics toasts

`ShowStatisticsToasts` shows statistics messages in the bottom-left corner — session intro for you, global stats on join/leave. Does not replace the game's own connect messages.

**Full config keys →** [Statistics](../CONFIG.md#statistics--mimesisplayerenhancement_statistics)
