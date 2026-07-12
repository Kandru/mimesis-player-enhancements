# Weather

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Weather`](../CONFIG.md#weather--mimesisplayerenhancement_weather)

Control dungeon weather presets (fixed, cycling, or vanilla), optionally strip random weather rolls, and set the synced in-game start hour for outdoor lighting. **All settings apply in real time** during an active dungeon. Clients without the mod stay in sync via vanilla network messages.

## Weather modes

`WeatherMode`:

- `Vanilla` — game schedule; optional `DisableRandomWeather` removes procedural random blocks.
- `Fixed` — one preset for the entire run (`FixedWeatherPreset`: Sunny, Rain, HeavyRain, Squall).
- `Cycle` — rotate through `WeatherCyclePresets` on random real-time delays between min/max seconds.

## Start time and lighting

`StartTimePreset` sets the **synced in-game clock** when a dungeon starts (tram alarm and outdoor lighting). The clock advances during the shift until ~24:00 at time-over. **Real shift deadline is unchanged** — still based on dungeon duration in real time.

| Preset | Clock at start | Lighting |
|--------|----------------|----------|
| Vanilla | ~10:00 | Bright daytime (default) |
| Morning | 08:00 | Bright morning |
| Noon | 12:00 | Bright midday |
| Dusk | 18:00 | Sunset / dim |
| Night | 21:00 | Dark (moonlit) |
| Midnight | 00:00 | Darkest at start |

Reference: sunrise ~06:00, sunset ~18:00.

## Realtime tram clock

Vanilla only updates the tram console clock when the in-game **hour** changes. `EnableRealtimeTramClock` (requires `EnableWeather`) syncs every in-game **minute** instead (~once per real second at default scale). Weather and lighting still change on hour boundaries only.

**Full config keys →** [Weather](../CONFIG.md#weather--mimesisplayerenhancement_weather)
