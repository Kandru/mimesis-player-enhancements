# Player Tuning

**Scope:** Host only · Your game only (local) · **Config:** [`MimesisPlayerEnhancement_PlayerTuning`](../CONFIG.md#player-tuning--mimesisplayerenhancement_playertuning)

Tweak how players move and survive: walk/run speed, stamina pool, sprint drain, stamina recovery, and carry weight. Changes apply to everyone in the session from the host side — joining clients do not need the mod; stats sync automatically.

## Movement

- `MoveSpeedMultiplier` — scales walk and run base speed (`1` = vanilla, `2` = double).
- `NoClipSpeedMultiplier` — scales dashboard noclip fly speed relative to current walk/run speed. Only applies while noclip is active.

## Stamina

- `MaxStaminaMultiplier` — maximum stamina pool.
- `StaminaDrainMultiplier` — sprint stamina cost per tick (`0.5` = half drain).
- `StaminaRegenMultiplier` — stamina recovered per regen tick.
- `StaminaRegenDelayMultiplier` — wait before regen starts after sprinting (`0.5` = regen starts sooner).

## Carry weight

`MaxCarryWeightMultiplier` scales maximum carry weight and the encumbrance slowdown threshold (`1` = vanilla, `2` = double).

## Player collision

`DisablePlayerCollision` (local client effect, requires `EnablePlayerTuning`) disables capsule colliders on other players and mimics so you can walk through them — useful in a crowded tram. Regular monsters and walls remain solid.

**Full config keys →** [Player Tuning](../CONFIG.md#player-tuning--mimesisplayerenhancement_playertuning)
