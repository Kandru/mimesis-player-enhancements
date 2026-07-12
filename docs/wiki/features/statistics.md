# Statistics

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Statistics`](../CONFIG.md#statistics--mimesisplayerenhancement_statistics)

Keeps track of how each player is doing over time: deaths, kills, mimic voice events, play time, and more. Stats are stored per save game and can be viewed on leaderboards in the [Web Dashboard](./web-dashboard.md). Useful for friendly competition or seeing who survived the longest.

## Session tracking

`EnableStatistics` turns per-save tracking on or off. Stats load from disk when a save is loaded, stay in memory during gameplay, and are written on vanilla save (including auto-save).

## Statistics toasts

`ShowStatisticsToasts` shows statistics messages in the bottom-left corner — session intro for you, global stats on join/leave. Does not replace the game's own connect messages.

## Reconnect grace period

`SessionReconnectGraceMinutes` — if someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh.

**Full config keys →** [Statistics](../CONFIG.md#statistics--mimesisplayerenhancement_statistics)
