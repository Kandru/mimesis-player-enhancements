# Statistics

**Scope:** host

Tracks per-player statistics for each save in a **save → zone → dungeon run → player** hierarchy, plus **global totals** that survive zone-1 restarts.

## Layers

| Layer | Persists across | Contents |
|-------|-----------------|----------|
| **Global** | Zone-1 restart | Lifetime counters per player, highest zone reached, sessions, run restarts, dungeon runs played |
| **Zone** | Zone-1 restart | Aggregates for each zone number while the current progression is active |
| **Dungeon run** | Until run closes | Per-run counters keyed by seed/map/cycle; closed on success, failure, or abandon |
| **Session** | Disconnect only (in memory) | Current connection counters with reconnect grace |

On **zone-1 restart** (wipeout), detailed zone and run history is discarded. Global totals and each player's **highest zone reached** are kept.

## Tracked counters

Combat: friends killed, killed by friends, monster kills/deaths, trap deaths, deaths, revives, survival outcomes, deathmatch outcomes.

Loot: items saved into the tram (count), train value saved, items carried (play report).

Presence: connected time, mimic encounters, damage to friends, voice events (global only).

## Configuration

TOML section: [`MimesisPlayerEnhancement_Statistics`](../CONFIG.md#statistics--mimesisplayerenhancement_statistics).

### `EnableStatistics`

Master switch for statistics tracking on the host.

Default: `true`

### `SessionReconnectGraceMinutes`

If someone disconnects and rejoins within this window, their session counters continue.

Default: `5`

### `ShowStatisticsToasts`

Shows statistics toasts on join/leave and dungeon completion.

Default: `true`

## Persistence

- File: `MMGameData{N}.mpe-stats.sav` (schema version **10**)
- Legacy files are backed up once as `*.legacy-v{n}.bak` and ignored (fresh start); atomic `.bak`/`.tmp` siblings are removed so the next read cannot recover the discarded schema
- Runs are capped at 60 per zone; zones at 40 per save

## Web dashboard

- **Statistics** page: global summary, player table, then zone sections (newest first) with dungeon run tables
- Run label: `{map name} · seed {seed}`
- History loaded from `GET /api/statistics/history` when the summary revision changes
