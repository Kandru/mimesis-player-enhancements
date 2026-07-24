# Player Tuning

Scale how players move, use stamina, and carry loot. **Host only** means only the host must enable the feature — the whole lobby gets the effect. Joining clients do not need the mod; stats sync from the host. Changes apply at runtime when config is saved (no restart). The host reloads player stats after each save.

**Config:** [`MimesisPlayerEnhancement_PlayerTuning`](../CONFIG.md#player-tuning--mimesisplayerenhancement_playertuning)

## Configuration

### `EnablePlayerTuning`

Master toggle for movement, stamina, and carry weight scaling. When off, those multipliers behave as vanilla (`1`). Requires the host to enable it; everyone in the lobby gets the scaled stats.

| Value | Meaning |
|---|---|
| `false` | Vanilla move speed, stamina, and carry weight (default) |
| `true` | Apply the multiplier settings below |

Default: `false`

### `MoveSpeedMultiplier`

Scales walk and run base speed for all players. Only applies when `EnablePlayerTuning` is on. Values outside `0.1`–`5.0` are clamped on save.

| Value | Meaning |
|---|---|
| `1` | Vanilla speed |
| `2` | Double speed |
| `0.5` | Half speed |

Default: `1`

### `NoClipSpeedMultiplier`

Scales web dashboard noclip fly speed relative to the player's current walk/run speed. Only applies while noclip is active. **Not** gated by `EnablePlayerTuning` — it still affects noclip when the master toggle is off.

| Value | Meaning |
|---|---|
| `3` | Triple fly speed (default) |
| `1` | Same as normal movement speed |

Default: `3`

### `MaxStaminaMultiplier`

Scales maximum stamina pool. Only applies when `EnablePlayerTuning` is on.

| Value | Meaning |
|---|---|
| `1` | Vanilla max stamina |
| `2` | Double max stamina |

Default: `1`

### `StaminaDrainMultiplier`

Scales sprint stamina cost per tick. Only applies when `EnablePlayerTuning` is on. Lower values mean stamina lasts longer while sprinting.

| Value | Meaning |
|---|---|
| `1` | Vanilla drain |
| `0.5` | Half drain (sprint longer) |
| `2` | Double drain |

Default: `1`

### `StaminaRegenMultiplier`

Scales how much stamina is recovered per regen tick. Only applies when `EnablePlayerTuning` is on.

| Value | Meaning |
|---|---|
| `1` | Vanilla regen rate |
| `2` | Double regen rate |

Default: `1`

### `StaminaRegenDelayMultiplier`

Scales the wait before stamina regen starts after sprinting. Only applies when `EnablePlayerTuning` is on. Lower values mean regen starts sooner.

| Value | Meaning |
|---|---|
| `1` | Vanilla delay |
| `0.5` | Regen starts sooner |

Default: `1`

### `MaxCarryWeightMultiplier`

Scales maximum carry weight and the encumbrance slowdown threshold. Only applies when `EnablePlayerTuning` is on.

| Value | Meaning |
|---|---|
| `1` | Vanilla carry limit |
| `2` | Double carry limit |

Default: `1`

### `DisablePlayerCollision`

**Local effect only** — not shared across the lobby. On your client, disables capsule colliders on other players and mimics so you can walk through them (e.g. a crowded tram). Regular monsters and walls stay solid. Requires `EnablePlayerTuning` on the host and the mod installed on your machine for the pass-through to work.

| Value | Meaning |
|---|---|
| `true` | Walk through other players and mimics on your client (default) |
| `false` | Normal collision with other players and mimics |

Default: `true`
