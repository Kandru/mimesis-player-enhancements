# Weather

**Scope:** host

Control dungeon weather (fixed, cycling, or vanilla) and optionally strip random weather rolls. All settings apply in real time during an active dungeon. For in-game start hour and tram-clock minute sync, see [Dungeon Time](./dungeon-time.md).

## Configuration

Settings live in `[MimesisPlayerEnhancement_Weather]` in `MimesisPlayerEnhancement.cfg`. No restart needed — changes apply while a dungeon is running.

### `EnableWeather`

Master toggle for the feature. When off, weather overrides are not applied.

| Value | Meaning |
|---|---|
| `false` | Feature disabled (default) |
| `true` | Host applies weather settings to the lobby |

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
