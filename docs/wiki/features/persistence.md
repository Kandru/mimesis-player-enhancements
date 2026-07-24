# Persistence

Keeps mimic voice recordings across save and load. Only the host must enable this for the whole lobby to get the effect. On vanilla save (including auto-save), recordings go to `MMGameData{N}.mpe-speech.sav` and Steam ID → voice UUID mappings go into the slot document; both are restored when that save is loaded. Use it with [More Voices](./more-voices.md) — Persistence stores what More Voices lets the game keep in the first place.

## Configuration

### `EnablePersistence`

Turns save/load/restore of mimic voices on or off on the host. When off, the in-memory pool is cleared and no speech file is written or loaded; existing speech files stay on disk until the save slot is deleted. Config reloads while the game is running — no restart.

| Value | Meaning |
|---|---|
| `true` | Host saves voices on game save and restores them when the slot loads (lobby gets the effect) |
| `false` | Host does not load, restore, or write speech data |

Default: `true`
