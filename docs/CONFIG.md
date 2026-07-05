# Config reference

After the first launch, the mod creates a config file here:

```
<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg
```

You can edit it anytime. The game reloads the file while running, but **most changes only fully apply after a restart**. Some settings may not update correctly until you quit and start again.

Settings are grouped into TOML sections in the config file:

- **`[MimesisPlayerEnhancement]`** — global options not tied to a single feature
- **`[MimesisPlayerEnhancement_FeatureName]`** — one section per feature (e.g. `[MimesisPlayerEnhancement_MorePlayers]`)

Each feature section has its own master toggle plus feature-specific options. The web dashboard can edit global defaults (written to the main config file immediately) and per-save-slot overrides (`MMGameData{N}.mpe-overrides.sav`, loaded at save load, applied live in memory, written on vanilla save).

## Contents

- [Global](#global--mimesisplayerenhancement)
- [More Players](#more-players--mimesisplayerenhancement_moreplayers)
- [More Voices](#more-voices--mimesisplayerenhancement_morevoices)
- [Persistence](#persistence--mimesisplayerenhancement_persistence)
- [Statistics](#statistics--mimesisplayerenhancement_statistics)
- [Player Announcements](#player-announcements--mimesisplayerenhancement_playerannouncements)
- [Join Anytime](#join-anytime--mimesisplayerenhancement_joinanytime)
- [Extended Save Slots](#extended-save-slots--mimesisplayerenhancement_extendedsaveslots)
- [Spawn Scaling](#spawn-scaling--mimesisplayerenhancement_spawnscaling)
- [Loot Multiplicator](#loot-multiplicator--mimesisplayerenhancement_lootmultiplicator)
- [Money Multiplier](#money-multiplier--mimesisplayerenhancement_moneymultiplier)
- [Dungeon Time](#dungeon-time--mimesisplayerenhancement_dungeontime)
- [Dead Player Features](#dead-player-features--mimesisplayerenhancement_deadplayerfeatures)
- [Player Tuning](#player-tuning--mimesisplayerenhancement_playertuning)
- [Dungeon Randomizer](#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer)
- [Web Dashboard](#web-dashboard--mimesisplayerenhancement_webdashboard)
- [Example config](#example-config)
### Global — `[MimesisPlayerEnhancement]`

Mod-wide settings that are not owned by a single feature.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `ModToastDurationSeconds` | float | `5.0` | How long `[PlayerEnhancements]` toasts stay visible before fading. Vanilla join/leave toasts are unchanged (~2 seconds). Each player controls this locally. Minimum is `1`. |
| `EnableDebugLogging` | bool | `false` | Emit verbose diagnostic lines to the MelonLoader console. Useful for troubleshooting. |

### More Players — `[MimesisPlayerEnhancement_MorePlayers]`

Host-only. Raise the vanilla 4-player session cap.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMorePlayers` | bool | `false` | Turn the higher player cap on or off. When off, the game stays at 4 players. |
| `MaxPlayers` | int | `32` | Max players in a session, host included. `1` = solo, `2` = host + one friend, and so on. Minimum is `1`. |
| `EnableExtendedSpectatorPlayerList` | bool | `false` | Replace the 4-player spectator death list with a two-column layout that scales to screen height. Requires `EnableMorePlayers`. Living players are shown first when space is limited; among dead players, speakers are prioritized. |

### More Voices — `[MimesisPlayerEnhancement_MoreVoices]`

Host-only. Raise per-player mimic voice recording limits.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMoreVoices` | bool | `true` | Turn higher voice recording limits on or off. |
| `MaxIndoorVoiceEvents` | int | `3000` | How many mimic voice lines each player can store in indoor dungeon runs. Minimum is `1`. |
| `MaxDeathMatchVoiceEvents` | int | `3000` | How many mimic voice lines each player can store in deathmatch. Minimum is `1`. |
| `MaxOutdoorVoiceEvents` | int | `3000` | How many mimic voice lines each player can store outdoors. Minimum is `1`. |

### Persistence — `[MimesisPlayerEnhancement_Persistence]`

Host-only. Keep mimic voice recordings across save and load.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnablePersistence` | bool | `true` | Save mimic voices when you save the game and bring them back when you load. |

### Statistics — `[MimesisPlayerEnhancement_Statistics]`

Host-only. Track session stats and a per-save-slot leaderboard (deaths, kills, voice events, play time, and more). Stats load from disk when a save is loaded, stay in memory during gameplay, and are written on vanilla save (including auto-save).

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableStatistics` | bool | `true` | Track player stats per save slot. |
| `SessionReconnectGraceMinutes` | int | `5` | If someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh. Minimum is `1`. |
| `ShowStatisticsToasts` | bool | `true` | Show small join/leave/cycle messages in the bottom-left corner when statistics are enabled. Does not replace the game's own connect messages. |

### Player Announcements — `[MimesisPlayerEnhancement_PlayerAnnouncements]`

Host-only. In-game toasts for dungeon run settings at shift start, boss spawn alerts, and your per-map stats when you die.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `ShowPlayerAnnouncements` | bool | `true` | Show dungeon settings, boss spawn, and death-stat toasts. Does not replace the game's own messages. |

### Join Anytime — `[MimesisPlayerEnhancement_JoinAnytime]`

Host-only. Lets players join a lobby after a session has already started. **Joiners do not need this mod** — only the host does.

Late joiners cannot be dropped straight into an active dungeon (the game has no stock path for that). Instead, the host server sends the player into a waiting room. They wait on the tram map until the party finishes the current dungeon; when everyone returns to the tram, the next lever pull starts the next run together.

**What it does:**

1. Allows login while a session is already running (`CanEnterSession`).
2. When a joiner appears in `MaintenanceRoom`, ensures a `VWaitingRoom` exists on the server (creates one with `InitWaitingRoom` if the original was wiped when the dungeon started).
3. Sends unicast `MakeRoomCompleteSig` (`roomType = Waiting`) and `MoveToWaitingRoomSig` so the stock client loads `InTramWaitingScene` and completes `EnterWaitingRoomReq`.
4. Blocks the tram start lever on the server (`VWaitingRoom.OnRequestStartGame`) while players are split — e.g. some still in the dungeon, or not everyone is in the waiting room yet (`CantStartGame`).
5. While a joiner is still loading, blocks tram departure for `JoinConnectionGraceSeconds`; players who do not finish loading in time are kicked (host is never kicked).

**Limitations:**

- Joiners **do not** land mid-dungeon; they sit out the current run in the tram.
- If the host is still in maintenance or already in the tram (pre-lever), joiners are routed to the same waiting room via the same packets.
- Public lobby visibility helpers still run on the host when this feature is enabled.
- Hosts can toggle public matchmaking and edit the lobby title from the ESC menu in the tram or during a dungeon run — not only in the maintenance room. Lobby title and public/private preference are saved with the game (per save slot).

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableJoinAnytime` | bool | `true` | Let players join after a session has already started. |
| `JoinConnectionGraceSeconds` | int | `30` | After a player connects, block tram departure for this many seconds while they finish loading. Players who do not become ready in time are kicked (host is never kicked). Minimum is `1`. |
| `JoinTramRouteRetrySeconds` | float | `0.5` | How often the host retries routing a late joiner from maintenance to the tram (packet resend and release). Minimum is `0.1`. Retries continue until the joiner reaches the waiting room or `JoinConnectionGraceSeconds` expires. |

### Extended Save Slots — `[MimesisPlayerEnhancement_ExtendedSaveSlots]`

Local UI. Replaces the separate New/Load Tram menus with a unified scrollable save picker on the main menu. Slot 0 remains autosave; manual slots 1–99 are available when enabled (vanilla allows 1–3). The Host button opens the picker instead of the vanilla flow; press ESC to close. Existing saves in slots 1–3 remain compatible. Other players in a session do not need the mod.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableExtendedSaveSlots` | bool | `true` | Replace vanilla New/Load Tram with the unified save picker and 99-slot limit. When `false`, vanilla Tram menus and the 3-slot limit return. |

### Spawn Scaling — `[MimesisPlayerEnhancement_SpawnScaling]`

Host-only. Scale dungeon monster and trap spawn budgets by type. Periodic jakos and mimics use native threat/count budgets plus faster spawn windows. Map-placed bosses, specials, and traps activate unused alternate markers for extra concurrent slots, then schedule bonus encounters one-at-a-time after a kill (never duplicate spawns at load). Each **Auto Scale … By Player Count** toggle stacks with its per-type multiplier using `SpawnScalingPlayerCountScaleRate` per player above 4 (default `0.10` = +10% per extra player).

Upgrading from older configs: legacy `FixedSpawnRespawn*` keys are copied once automatically to the `MapPlacedEncounter*` keys below.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableSpawnScaling` | bool | `false` | Master toggle for all spawn scaling below. |
| `SpawnScalingPlayerCountScaleRate` | float | `0.10` | Extra multiplier per player above 4 when an Auto Scale toggle is on (`0.10` = +10% per extra player). Set `0.25` to match the old `players / 4` curve. Minimum is `0`. |
| `AutoScaleMimicSpawnsByPlayerCount` | bool | `true` | Player-count scaling for mimic spawns (stacks with `MimicSpawnMultiplier`). |
| `MimicSpawnMultiplier` | float | `1.0` | Total mimic spawn budget across the run, including periodic spawns (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleBossSpawnsByPlayerCount` | bool | `true` | Player-count scaling for boss spawns (stacks with `BossSpawnMultiplier`). |
| `BossSpawnMultiplier` | float | `1.0` | Map-placed bosses: unused alternate markers plus bonus encounters after kill (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleJakoSpawnsByPlayerCount` | bool | `true` | Player-count scaling for jako spawns (stacks with `JakoSpawnMultiplier`). |
| `JakoSpawnMultiplier` | float | `1.0` | Total normal-monster threat budget for ambient dungeon spawns (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleSpecialSpawnsByPlayerCount` | bool | `true` | Player-count scaling for special spawns (stacks with `SpecialSpawnMultiplier`). |
| `SpecialSpawnMultiplier` | float | `1.0` | Special monster budget for periodic spawns and map-placed specials (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleTrapSpawnsByPlayerCount` | bool | `true` | Player-count scaling for trap spawns (stacks with `TrapSpawnMultiplier`). |
| `TrapSpawnMultiplier` | float | `1.0` | Map-placed traps: unused alternate markers plus bonus encounters after trigger/kill (`1` = vanilla, `2` = double). Minimum is `0`. |
| `MapPlacedEncounterDelayMinSeconds` | float | `5.0` | Shortest wait (seconds) after a map-placed enemy/trap dies before the next bonus encounter can spawn at that marker. Minimum is `0`. |
| `MapPlacedEncounterDelayMaxSeconds` | float | `30.0` | Longest wait for that random delay. Actual delay is picked between min and max. Must be ≥ `MapPlacedEncounterDelayMinSeconds`. |
| `MapPlacedEncounterMinPlayerDistanceMeters` | float | `10.0` | After the delay, hold the spawn until no living players are within this radius (meters) of the marker. Set to `0` to spawn as soon as the delay elapses. Minimum is `0`. |
| `AutoScaleOtherSpawnsByPlayerCount` | bool | `true` | Player-count scaling for other spawns (stacks with `OtherSpawnMultiplier`). |
| `OtherSpawnMultiplier` | float | `1.0` | Spawn multiplier for other entities (not mimic/boss/jako/special/trap). Minimum is `0`. |

### Loot Multiplicator — `[MimesisPlayerEnhancement_LootMultiplicator]`

Host-only. Scale how much loot appears on the map and from enemy deaths, and optionally convert mimic fake drops to real pickup loot. Each multiplier (`1` = vanilla, `2` = double) stacks with its **Auto Scale … By Player Count** toggle using `LootMultiplicatorPlayerCountScaleRate` per player above 4 (default `0.10` = +10% per extra player).

**Breaking change:** Per-item-type keys (`MapConsumableLootMultiplier`, `Trigger*`, etc.) were removed. Set `MapLootMultiplier` and `DropLootMultiplier` instead; old TOML keys are ignored.

**Loot sources:**

| Source | What it affects |
|--------|-----------------|
| **Map** | Loot placed when a dungeon room loads. **Fixed** loot (specific item at a marker): activates unused loot markers of the same item, scales consumable stack size and `MaxRespawnCount`, and may respawn at the same marker when picked up (uses `MapPlacedEncounterDelay*` and `MapPlacedEncounterMinPlayerDistanceMeters` from Spawn Scaling). **Random** loot pools: scales the dungeon misc budget so more markers fill with weighted random picks — not clones of the same item. |
| **Drop** | Items from enemy death tables when a monster is killed, plus inventory items dropped on death. Adds extra **weighted re-rolls** from the same drop table. Consumable stack count is also scaled when the item spawns (`ActorDying`). Mimics often drop **fake** decoy items from inventory — see `ConvertFakeActorDyingDropChancePercent`. Monster drop-table loot is already real. |

Map events / trigger spawns are **not** scaled (vanilla).

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableLootMultiplicator` | bool | `false` | Master toggle for all loot scaling below. |
| `LootMultiplicatorPlayerCountScaleRate` | float | `0.10` | Extra multiplier per player above 4 when an Auto Scale toggle is on (`0.10` = +10% per extra player). Set `0.25` to match the old `players / 4` curve. Minimum is `0`. |
| `AutoScaleMapLootByPlayerCount` | bool | `true` | Player-count scaling for map loot. |
| `MapLootMultiplier` | float | `1.0` | Multiplier for all map-placed pickup loot. Minimum is `0`. |
| `AutoScaleDropLootByPlayerCount` | bool | `true` | Player-count scaling for enemy death drops. |
| `DropLootMultiplier` | float | `1.0` | Multiplier for enemy death drops. Minimum is `0`. |
| `LootItemFilterMode` | string | `All` | `All`, `AllowlistOnly`, or `BlocklistOnly` — restrict which item master IDs are scaled. |
| `LootAllowlist` | string | `""` | Comma-separated item master IDs (e.g. `12345,67890`). Used when `LootItemFilterMode` is `AllowlistOnly`. See [LOOT_ITEM_IDS.md](LOOT_ITEM_IDS.md) for all IDs. |
| `LootBlocklist` | string | `""` | Comma-separated item master IDs to exclude. Used when `LootItemFilterMode` is `BlocklistOnly`. See [LOOT_ITEM_IDS.md](LOOT_ITEM_IDS.md) for all IDs. |
| `ConvertFakeActorDyingDropChancePercent` | int | `30` | Chance (0–100) that fake items dropped on enemy death (`ActorDying`, e.g. mimic inventory decoys) become real pickup loot. `0` = vanilla (vanish on grab), `100` = always real. |

Does **not** scale: map event/trigger spawns, items you release from inventory, shop purchases, Crow Shop scrap exchange (barter), admin/cheat spawns, creature/monster spawns, or other spawn reasons (e.g. `Release`, `Buying`, `Admin`, `Skill`). Map loot budgets and spawn data are scaled once at room load; drop extras use table re-rolls at spawn time.

### Money Multiplier — `[MimesisPlayerEnhancement_MoneyMultiplier]`

Host-only. Scales five separate money values. Each has an **Auto Scale … By Player Count** toggle and a multiplier (`1` = vanilla, `2` = double). Player-count scaling uses `MoneyMultiplierPlayerCountScaleRate` per player above 4 (default `0.10` = +10% per extra player). Minimum multiplier is `0`.

| Money type | What it affects |
|------------|-----------------|
| **Startup** | Starting currency on a new save slot or session reset (not when loading a save) |
| **Round goal** | Target currency (quota) required to finish a stage |
| **Scrap / sell value** | Currency from scrapping items and item value counted in the tram toward the quota |
| **Shop buy price** | Maintenance shop and vending-machine kiosk purchase cost (shown in UI and charged on buy) |
| **Reinforce price** | Maintenance item reinforcement cost |

Does **not** change saved player balances or shop prices on save load. Shop price multipliers and discount rolls apply on fresh `InitShopItems` rounds (e.g. returning from a dungeon). Complements **Loot Multiplicator** (item quantity) — this mod scales currency amounts and prices, not how many items spawn.

**Shop discounts:** When `ShopDiscountChancePercent` is above `0`, each shop item independently rolls for a discount. Successful rolls pick a random percentage between `ShopDiscountMinPercent` and `ShopDiscountMaxPercent`, update the item's sale price, and sync vending-machine displays. At `0` chance, vanilla shop discount tables are used unchanged.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMoneyMultiplier` | bool | `false` | Master toggle for all money scaling below. |
| `MoneyMultiplierPlayerCountScaleRate` | float | `0.10` | Extra multiplier per player above 4 when an Auto Scale toggle is on (`0.10` = +10% per extra player). Set `0.25` to match the old `players / 4` curve. Minimum is `0`. |
| `AutoScaleStartupMoneyByPlayerCount` | bool | `true` | Player-count scaling for startup money. |
| `StartupMoneyMultiplier` | float | `1.0` | Startup money multiplier on new save or session reset. Minimum is `0`. Does not apply when loading a save. |
| `AutoScaleRoundGoalMoneyByPlayerCount` | bool | `true` | Player-count scaling for stage target currency. |
| `RoundGoalMoneyMultiplier` | float | `1.0` | Round goal (quota) multiplier. Minimum is `0`. |
| `AutoScaleScrapSellValueByPlayerCount` | bool | `true` | Player-count scaling for scrap/sell values. |
| `ScrapSellValueMultiplier` | float | `1.0` | Scrap/sell value multiplier. Minimum is `0`. |
| `AutoScaleShopBuyPriceByPlayerCount` | bool | `true` | Player-count scaling for shop buy prices. |
| `ShopBuyPriceMultiplier` | float | `1.0` | Maintenance shop and vending-machine kiosk buy price multiplier (`1` = vanilla, `0.1` = 10% of vanilla). Applied when shop items are initialized each maintenance round. Minimum is `0`. |
| `ShopDiscountMinPercent` | int | `0` | Minimum discount percentage when a shop discount is rolled (`0`–`100`). Only used when `ShopDiscountChancePercent` is above `0`. |
| `ShopDiscountMaxPercent` | int | `100` | Maximum discount percentage when a shop discount is rolled (`0`–`100`). Must be ≥ `ShopDiscountMinPercent`. |
| `ShopDiscountChancePercent` | int | `0` | Chance per shop item to receive a discount in the min–max range (`0` = vanilla shop discounts, `100` = every item discounted). |
| `AutoScaleReinforcePriceByPlayerCount` | bool | `true` | Player-count scaling for reinforce costs. |
| `ReinforcePriceMultiplier` | float | `1.0` | Reinforce price multiplier. Minimum is `0`. |

### Dungeon Time — `[MimesisPlayerEnhancement_DungeonTime]`

Host-only. When a dungeon shift starts (all members entered), extends the real shift deadline by `ExtraShiftSecondsPerPlayerAboveBaseline` for each player above `DungeonTimeBaselinePlayerCount`. Applied once per dungeon room; late Join Anytime arrivals do not add more time.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableDungeonTime` | bool | `false` | Master toggle for shift extension. |
| `DungeonTimeBaselinePlayerCount` | int | `4` | No extra shift time at or below this player count. Minimum is `1`. |
| `ExtraShiftSecondsPerPlayerAboveBaseline` | float | `10.0` | Real seconds added to the shift deadline per player above the baseline. Minimum is `0`. |

### Dead Player Features — `[MimesisPlayerEnhancement_DeadPlayerFeatures]`

Host-only mimic possession tuning for dead players. Off by default — set `EnableDeadPlayerFeatures = true` to turn it on.

When you are dead and press **E** to speak through a nearby mimic, vanilla uses a fixed speak window (`C_PossessionDuration`, 12 seconds) and a fixed cooldown before the next possession (`C_PossessionCooltime`). This feature can randomize the speak window per possession and/or scale the cooldown.

**Speak duration:** When `RandomizeMimicPossessionDuration` is enabled, each possession rolls a random duration between `MimicPossessionMinTimeSeconds` and `MimicPossessionMaxTimeSeconds`. At `12.0` / `12.0` the roll equals vanilla length.

**Cooldown:** `MimicPossessionCooltimeMultiplier` scales the wait after possession ends (`1` = vanilla, `2` = double). Independent of the random duration toggle.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableDeadPlayerFeatures` | bool | `false` | Master toggle for mimic possession tuning in this section. |
| `EnableMimicPossessionTuning` | bool | `false` | Sub-toggle for mimic possession timing tweaks (host only). |
| `RandomizeMimicPossessionDuration` | bool | `false` | Roll a random speak duration per E-possession between the min and max seconds below. |
| `MimicPossessionMinTimeSeconds` | float | `12.0` | Minimum rolled speak duration in seconds (vanilla is 12). Valid range is `0.1`–`120`. |
| `MimicPossessionMaxTimeSeconds` | float | `12.0` | Maximum rolled speak duration in seconds (vanilla is 12). Valid range is `0.1`–`120`. |
| `MimicPossessionCooltimeMultiplier` | float | `1.0` | Post-possession cooldown multiplier (`1` = vanilla, `2` = double). Valid range is `0.1`–`10.0`. |

Upgrading from older configs: the legacy section `[MimesisPlayerEnhancement_MimicTuning]` and `EnableMimicTuning` are migrated once automatically into this section. Legacy `MimicPossessionMinTimeMultiplier` / `MimicPossessionMaxTimeMultiplier` keys are converted to seconds (`multiplier × 12`).

### Player Tuning — `[MimesisPlayerEnhancement_PlayerTuning]`

Host-only. Scales player movement and stamina on the server. Joining clients do not need the mod — stats sync from the host automatically. Multipliers use `1.0` for vanilla; valid range is `0.1`–`5.0`. Changes apply at runtime when config is saved (host reloads player stats).

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnablePlayerTuning` | bool | `false` | Master toggle for all player tuning below. |
| `MoveSpeedMultiplier` | float | `1.0` | Scales walk and run base speed. |
| `MaxStaminaMultiplier` | float | `1.0` | Scales maximum stamina. |
| `StaminaDrainMultiplier` | float | `1.0` | Scales sprint stamina cost per tick (`0.5` = half drain). |
| `StaminaRegenMultiplier` | float | `1.0` | Scales stamina recovered per regen tick. |
| `StaminaRegenDelayMultiplier` | float | `1.0` | Scales wait before regen starts after sprinting (`0.5` = regen starts sooner). |
| `MaxCarryWeightMultiplier` | float | `1.0` | Scales carry capacity before encumbrance slows movement. |

### Dungeon Randomizer — `[MimesisPlayerEnhancement_DungeonRandomizer]`

Host-only. Randomizes dungeon selection at four independent layers when enabled. Off by default — set `EnableDungeonRandomizer = true` to turn it on. Each layer has its own toggle so you can randomize only what you want.

**Layers:**

| Layer | What it affects |
|-------|-----------------|
| **Dungeon pick** | Which dungeon master ID appears on the tram roll |
| **Layout flow** | DunGen procedural layout variant within a dungeon |
| **Map variant** | Which map ID is chosen from the dungeon's `MapIDs` |
| **Seed** | Procedural `RandomDungeonSeed` used for room generation |

**Pool modes** (`DungeonPickPoolMode`):

| Value | Behavior |
|-------|----------|
| `WidenVanilla` | Keep vanilla cycle weights; optionally allow repeats sooner via `IgnoreDungeonExcludeList` |
| `AllActiveUniform` | Pick uniformly from all active dungeons (ignores the cycle table) |

`DungeonAllowlist` and `DungeonBlocklist` filter the pool regardless of mode. Allowlist wins when non-empty: only listed IDs are eligible.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableDungeonRandomizer` | bool | `false` | Master toggle for all dungeon randomization below. |
| `RandomizeDungeonPick` | bool | `true` | Override tram dungeon master ID selection. |
| `DungeonPickPoolMode` | string | `WidenVanilla` | `WidenVanilla` or `AllActiveUniform` (see table above). |
| `DungeonAllowlist` | string | `""` | Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible. |
| `DungeonBlocklist` | string | `""` | Comma-separated dungeon master IDs to exclude. |
| `IgnoreDungeonExcludeList` | bool | `true` | With `WidenVanilla`, do not exclude recently played dungeons from the tram roll. |
| `RandomizeLayoutFlow` | bool | `true` | Pick DunGen layout flows uniformly instead of weighted vanilla rolls. |
| `RandomizeMapVariant` | bool | `true` | Pick map variants uniformly from each dungeon's `MapIDs`. |
| `RandomizeDungeonSeed` | bool | `true` | Replace the procedural dungeon seed when a dungeon is chosen. |

### Web Dashboard — `[MimesisPlayerEnhancement_WebDashboard]`

Host-only. Serves a local HTTP dashboard from the game process. Open `http://<ListenAddress>:<ListenPort>/` in a browser (default: `http://127.0.0.1:8001/`). On by default — set `EnableWebDashboard = false` to turn it off. The dashboard is available whenever the game is running with the web dashboard enabled (not only during an active session).

**What you get:**

| View | Who can see it | What it shows |
|------|----------------|---------------|
| **Global Settings** | Host or idle (no session) | Global defaults (`MimesisPlayerEnhancement.cfg`); written to disk immediately |
| **Settings** | Host in an active save | Per-save-slot overrides (`MMGameData{N}.mpe-overrides.sav`); applied live in memory immediately; written to disk only via the in-game save button; keys matching global are omitted automatically |
| **Players** | Anyone who can reach the URL | Connected players with avatars, host/local badges, network grade, and ban status |
| **Minimap** | Anyone who can reach the URL | Live player positions during dungeon runs; focus player and area selectors |
| **Leaderboard** | Host only | Per-save-slot stats leaderboard (requires **Statistics** enabled) |
| **Player stats** | Host only | Per-player statistics for the active save slot (requires **Statistics** enabled) |
| **Moderation** | Host only | Kick, ban, unban, respawn, and heal actions queued on the game thread |

**Blind mode** (header toggle, host only): on by default for fairness. Hides alive/dead status, session stats, vitals, and respawn actions to avoid spoilers. While you are dead, blind mode is temporarily lifted so you can review others' stats; it restores automatically on revive unless you turned it off manually. The toggle preference is not saved across browser reloads.

**Security:** Default bind is `127.0.0.1` (loopback) so only your machine can connect. Binding to another address (e.g. `0.0.0.0` or your LAN IP) exposes the dashboard to anyone on that network — there is no login. Only use a non-loopback address on a network you trust.

Listen address and port changes take effect when the config reloads (the HTTP server restarts). Port must be between `1` and `65535`.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableWebDashboard` | bool | `true` | Turn the local web dashboard on or off. |
| `WebDashboardListenAddress` | string | `127.0.0.1` | HTTP bind address. Use `127.0.0.1` for local-only access. |
| `WebDashboardListenPort` | int | `8001` | TCP port for the web dashboard. Must be `1`–`65535`. |

### Example config

Abbreviated example showing section layout (not every loot/money key is listed):

```toml
[MimesisPlayerEnhancement]
ModToastDurationSeconds = 5.0
EnableDebugLogging = false

[MimesisPlayerEnhancement_MorePlayers]
EnableMorePlayers = false
MaxPlayers = 32
EnableExtendedSpectatorPlayerList = false

[MimesisPlayerEnhancement_MoreVoices]
EnableMoreVoices = true
MaxIndoorVoiceEvents = 3000
MaxDeathMatchVoiceEvents = 3000
MaxOutdoorVoiceEvents = 3000

[MimesisPlayerEnhancement_Persistence]
EnablePersistence = true

[MimesisPlayerEnhancement_Statistics]
EnableStatistics = true
SessionReconnectGraceMinutes = 5
ShowStatisticsToasts = true

[MimesisPlayerEnhancement_PlayerAnnouncements]
ShowPlayerAnnouncements = true

[MimesisPlayerEnhancement_JoinAnytime]
EnableJoinAnytime = true
JoinConnectionGraceSeconds = 30

[MimesisPlayerEnhancement_ExtendedSaveSlots]
EnableExtendedSaveSlots = true

[MimesisPlayerEnhancement_SpawnScaling]
EnableSpawnScaling = false
SpawnScalingPlayerCountScaleRate = 0.10
MimicSpawnMultiplier = 1.0
# … other spawn keys …

[MimesisPlayerEnhancement_LootMultiplicator]
EnableLootMultiplicator = false
LootMultiplicatorPlayerCountScaleRate = 0.10
MapLootMultiplier = 1.0
DropLootMultiplier = 1.0
ConvertFakeActorDyingDropChancePercent = 30
# … other loot keys …

[MimesisPlayerEnhancement_MoneyMultiplier]
EnableMoneyMultiplier = false
MoneyMultiplierPlayerCountScaleRate = 0.10
StartupMoneyMultiplier = 1.0
# … other money keys …

[MimesisPlayerEnhancement_DungeonTime]
EnableDungeonTime = false
DungeonTimeBaselinePlayerCount = 4
ExtraShiftSecondsPerPlayerAboveBaseline = 10.0

[MimesisPlayerEnhancement_DeadPlayerFeatures]
EnableDeadPlayerFeatures = false
EnableMimicPossessionTuning = false
RandomizeMimicPossessionDuration = false
MimicPossessionMinTimeSeconds = 12.0
MimicPossessionMaxTimeSeconds = 12.0
MimicPossessionCooltimeMultiplier = 1.0

[MimesisPlayerEnhancement_PlayerTuning]
EnablePlayerTuning = false
MoveSpeedMultiplier = 1.0
MaxStaminaMultiplier = 1.0
StaminaDrainMultiplier = 1.0
StaminaRegenMultiplier = 1.0
StaminaRegenDelayMultiplier = 1.0
MaxCarryWeightMultiplier = 1.0

[MimesisPlayerEnhancement_DungeonRandomizer]
EnableDungeonRandomizer = false
RandomizeDungeonPick = true
DungeonPickPoolMode = "WidenVanilla"

[MimesisPlayerEnhancement_WebDashboard]
EnableWebDashboard = true
WebDashboardListenAddress = "127.0.0.1"
WebDashboardListenPort = 8001
```
