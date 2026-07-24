# Weather

Control dungeon weather (fixed, cycling, or vanilla), optionally strip random weather rolls, and set the synced in-game start hour for outdoor lighting and the tram clock. Only the host needs to enable this — the whole lobby gets the effect. All settings apply in real time during an active dungeon; joining clients without the mod stay in sync via vanilla network messages.

## Configuration

Settings live in `[MimesisPlayerEnhancement_Weather]` in `MimesisPlayerEnhancement.cfg`. No restart needed — changes apply while a dungeon is running.

### `EnableWeather`

Master toggle for the feature. When off, weather and start-time overrides are not applied.

| Value | Meaning |
|---|---|
| `false` | Feature disabled (default) |
| `true` | Host applies weather and start-time settings to the lobby |

Default: `false`

### `WeatherMode`

How dungeon weather is chosen for the run.

| Value | Meaning |
|---|---|
| `Vanilla` | Game's built-in schedule; optional `DisableRandomWeather` removes procedural random blocks |
| `Fixed` | One preset for the entire run (`FixedWeatherPreset`) |
| `Cycle` | Rotate through `WeatherCyclePresets` on random real-time delays between min and max seconds |

Invalid values reset to `Vanilla`. Case-insensitive.

Default: `Vanilla`

### `FixedWeatherPreset`

Weather preset used when `WeatherMode` is `Fixed`.

| Value | Meaning |
|---|---|
| `Sunny` | Clear sky |
| `Rain` | Rain |
| `HeavyRain` | Heavy rain |
| `Squall` | Squall |

Invalid values reset to `Sunny`. Case-insensitive.

Default: `Sunny`

### `DisableRandomWeather`

When `WeatherMode` is `Vanilla`, removes procedural random weather rolls while keeping the scheduled hourly changes.

| Value | Meaning |
|---|---|
| `false` | Random weather rolls allowed (default) |
| `true` | Strip random weather; keep scheduled changes only |

Only applies in `Vanilla` mode.

Default: `false`

### `WeatherCyclePresets`

Ordered list of presets to rotate through when `WeatherMode` is `Cycle`. Comma-separated; order is preserved. Duplicate names are skipped. Unknown preset names are skipped with a warning. If the list ends up empty, cycling stops.

Example: `Sunny,Rain,HeavyRain`

Default: `Sunny,Rain`

### `WeatherCycleMinDelaySeconds`

Shortest wait before the next weather change in `Cycle` mode. Units: real seconds. Values below `0` reset to `0`. If max delay is below min, max is raised to match min.

Default: `300`

### `WeatherCycleMaxDelaySeconds`

Longest wait before the next weather change in `Cycle` mode. Units: real seconds. Must be ≥ `WeatherCycleMinDelaySeconds`; otherwise it is reset to the min value. When min equals max, every step uses that fixed delay.

Default: `600`

### `StartTimePreset`

Sets the **synced in-game clock** when a dungeon starts (tram alarm and outdoor lighting). The clock still advances during the shift until ~24:00 at time-over. **Real shift deadline is unchanged** — still based on dungeon duration in real time. Sunrise ~06:00, sunset ~18:00.

| Value | Clock at start | Lighting |
|---|---|---|
| `Vanilla` | ~10:00 (from dungeon data) | Bright daytime (default) |
| `Morning` | 08:00 | Bright morning |
| `Noon` | 12:00 | Bright midday |
| `Dusk` | 18:00 | Sunset / dim |
| `Night` | 21:00 | Dark (moonlit) |
| `Midnight` | 00:00 | Darkest at start |

Invalid values reset to `Vanilla`. Case-insensitive. Requires `EnableWeather`.

Default: `Vanilla`

### `EnableRealtimeTramClock`

Vanilla only updates the tram console clock when the in-game **hour** changes (~once per real minute at default time scale), so the display shows `HH:00` until the next hour. When enabled, the host syncs every in-game **minute** instead (~once per real second at default scale). Weather and lighting still change on hour boundaries only.

| Value | Meaning |
|---|---|
| `false` | Hourly tram clock updates (default) |
| `true` | Minute-level tram clock updates during dungeon runs |

Requires `EnableWeather`.

Default: `false`
