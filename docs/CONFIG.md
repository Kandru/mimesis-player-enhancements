# Configuration reference

Mimesis Player Enhancement stores settings in a TOML file separate from MelonLoader’s global preferences. After the first launch, edit:

```
<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg
```

The game reloads this file while running, but **most changes only fully apply after a restart**.

## Section layout

| Pattern | Purpose |
|---------|---------|
| `[MimesisPlayerEnhancement]` | Global options not tied to a single feature |
| `[MimesisPlayerEnhancement_FeatureName]` | One section per feature (e.g. `[MimesisPlayerEnhancement_MorePlayers]`) |

Each feature section has a master toggle (where applicable) plus feature-specific options.

**Per-save overrides:** The web dashboard can edit global defaults (written to `MimesisPlayerEnhancement.cfg` immediately) and per-save-game overrides (`MMGameData{N}.mpe-overrides.sav`, loaded at save load, applied live in memory, written on vanilla save).

## Quick reference

| Section | Feature | Scope |
|---------|---------|-------|
| [Global](#global--mimesisplayerenhancement) | Mod-wide settings | Local / all players |
| [More Players](#more-players--mimesisplayerenhancement_moreplayers) | Raise the 4-player session cap | Host |
| [More Voices](#more-voices--mimesisplayerenhancement_morevoices) | Raise mimic voice recording limits | Host |
| [Persistence](#persistence--mimesisplayerenhancement_persistence) | Save mimic voices across load | Host |
| [Statistics](#statistics--mimesisplayerenhancement_statistics) | Per-save-game stats and leaderboard | Host |
| [Player Announcements](#player-announcements--mimesisplayerenhancement_playerannouncements) | Dungeon/boss/death messages | Host |
| [Join Anytime](#join-anytime--mimesisplayerenhancement_joinanytime) | Late join after session start | Host |
| [Extended Save games](#extended-save-games--mimesisplayerenhancement_extendedsavegames) | Unified save picker (99 games) | Local UI |
| [Spawn Scaling](#spawn-scaling--mimesisplayerenhancement_spawnscaling) | Scale monster/trap spawn budgets | Host |
| [Loot Multiplicator](#loot-multiplicator--mimesisplayerenhancement_lootmultiplicator) | Scale map and drop loot | Host |
| [Money Multiplier](#money-multiplier--mimesisplayerenhancement_moneymultiplier) | Scale currency and shop prices | Host |
| [Dungeon Time](#dungeon-time--mimesisplayerenhancement_dungeontime) | Extend shift deadline by player count | Host |
| [Dead Player Features](#dead-player-features--mimesisplayerenhancement_deadplayerfeatures) | Mimic possession timing for dead players | Host |
| [Player Tuning](#player-tuning--mimesisplayerenhancement_playertuning) | Movement, stamina, carry weight | Host |
| [Dungeon Randomizer](#dungeon-randomizer--mimesisplayerenhancement_dungeonrandomizer) | Randomize dungeon selection layers | Host |
| [Web Dashboard](#web-dashboard--mimesisplayerenhancement_webdashboard) | Local HTTP dashboard | Host process |

---

## Global — `[MimesisPlayerEnhancement]`

Mod-wide settings that are not owned by a single feature.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `ModToastDurationSeconds` | float | `5.0` | ≥ `1` | How long mod messages stay visible in the bottom-left corner before fading. Vanilla join/leave connect messages are unchanged (~2 seconds). Each player controls this locally. |
| `EnableDebugLogging` | bool | `false` | — | Emit verbose diagnostic lines to the MelonLoader console. Useful for troubleshooting. |

## More Players — `[MimesisPlayerEnhancement_MorePlayers]`

**Host-only.** Raise the vanilla 4-player multiplayer session cap and optionally expand the spectator death list UI.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableMorePlayers` | bool | `false` | — | Turn the higher player cap on or off. When off, the game stays at 4 players. |
| `MaxPlayers` | int | `32` | ≥ `1` | Max players in a session, host included (`1` = solo, `2` = host + one friend, and so on). |
| `EnableExtendedSpectatorPlayerList` | bool | `false` | — | Replace the 4-player spectator death list with a two-column layout that scales to screen height. Requires Enable More Players. Living players are shown first when space is limited; among dead players, speakers are prioritized. |

## More Voices — `[MimesisPlayerEnhancement_MoreVoices]`

**Host-only.** Raise per-player mimic voice recording limits above vanilla caps.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableMoreVoices` | bool | `true` | — | Turn higher voice recording limits on or off. |
| `MaxIndoorVoiceEvents` | int | `3000` | ≥ `1` | Stored mimic voice lines per player in indoor dungeon runs (default game limit is much lower). |
| `MaxDeathMatchVoiceEvents` | int | `3000` | ≥ `1` | Stored mimic voice lines per player in deathmatch (default game limit is much lower). |
| `MaxOutdoorVoiceEvents` | int | `3000` | ≥ `1` | Stored mimic voice lines per player outdoors (default game limit is much lower). |

## Persistence — `[MimesisPlayerEnhancement_Persistence]`

**Host-only.** Keep mimic voice recordings across save and load.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnablePersistence` | bool | `true` | — | Save mimic voices when you save the game and restore them when you load. |

## Statistics — `[MimesisPlayerEnhancement_Statistics]`

**Host-only.** Track session stats and a per-save-game leaderboard (deaths, kills, voice events, play time, and more). Stats load from disk when a save is loaded, stay in memory during gameplay, and are written on vanilla save (including auto-save).

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableStatistics` | bool | `true` | — | Track player stats per save game. |
| `SessionReconnectGraceMinutes` | int | `5` | ≥ `1` | If someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh. |
| `ShowStatisticsToasts` | bool | `true` | — | Show statistics messages in the bottom-left corner (session intro for you, global stats on join/leave). Does not replace the game's own connect messages. |

## Player Announcements — `[MimesisPlayerEnhancement_PlayerAnnouncements]`

**Host-only.** Bottom-left messages for dungeon run settings at shift start, boss spawn alerts, and your per-map stats when you die.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `ShowPlayerAnnouncements` | bool | `true` | — | Show player messages in the bottom-left corner for dungeon run settings, boss spawns, and your per-map stats when you die. Does not replace the game's own messages. |

## Join Anytime — `[MimesisPlayerEnhancement_JoinAnytime]`

**Host-only.** Lets players join a lobby after a session has already started. **Joiners do not need this mod** — only the host does.

Late joiners cannot be dropped straight into an active dungeon. Instead, they wait on the tram map until the party finishes the current dungeon; when everyone returns to the tram, the next lever pull starts the next run together. While joiners are still loading, tram departure is blocked for `JoinConnectionGraceSeconds`; players who do not finish loading in time are kicked (host is never kicked).

**Limitations:**

- Joiners **do not** land mid-dungeon; they sit out the current run in the tram.
- Hosts can toggle public matchmaking and edit the lobby title from the ESC menu in the tram or during a dungeon run — not only in the maintenance room. Lobby title and public/private preference are saved with the game (per save game).

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableJoinAnytime` | bool | `true` | — | Let players join after a session has already started. |
| `JoinConnectionGraceSeconds` | int | `30` | ≥ `1` | After a player connects, block tram departure for this many seconds while they finish loading. Players who do not become ready in time are kicked (host is never kicked). |

## Extended Save games — `[MimesisPlayerEnhancement_ExtendedSavegames]`

**Local UI.** Replaces the separate New/Load Tram menus with a unified scrollable save picker on the main menu. game 0 remains autosave; manual games 1–99 are available when enabled (vanilla allows 1–3). The Host button opens the picker instead of the vanilla flow; press ESC to close. Existing saves in games 1–3 remain compatible. Other players in a session do not need the mod.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableExtendedSavegames` | bool | `true` | — | Replace vanilla New/Load Tram with the unified save picker and 99-game limit. When `false`, vanilla Tram menus and the 3-game limit return. |

## Spawn Scaling — `[MimesisPlayerEnhancement_SpawnScaling]`

**Host-only.** Scale dungeon monster and trap spawn budgets by type. Periodic jakos and mimics use native threat/count budgets plus faster spawn windows. Map-placed bosses, specials, and traps activate unused alternate markers for extra concurrent games, then schedule bonus encounters one-at-a-time after a kill (never duplicate spawns at load).

Each **Auto Scale … By Player Count** toggle stacks with its per-type multiplier using `SpawnScalingPlayerCountScaleRate` per player above 4 (`0.10` = +10% per extra player). Set `0.25` to approximate the old `players / 4` curve.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableSpawnScaling` | bool | `false` | — | Scale dungeon monster and trap spawn budgets by type. Host only. |
| `SpawnScalingPlayerCountScaleRate` | float | `0.10` | ≥ `0` | Extra multiplier per player above 4 when an Auto Scale … by Player Count toggle is enabled (0.10 = +10% per extra player, stacks with per-type multipliers). Minimum is 0. |
| `AutoScaleMimicSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for mimic spawns (stacks with `MimicSpawnMultiplier`). |
| `MimicSpawnMultiplier` | float | `1.0` | ≥ `0` | Total mimic spawn budget across the run, including periodic spawns (`1` = vanilla, `2` = double). |
| `AutoScaleBossSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for boss spawns (stacks with `BossSpawnMultiplier`). |
| `BossSpawnMultiplier` | float | `1.0` | ≥ `0` | Map-placed bosses: unused alternate markers plus bonus encounters after kill. |
| `AutoScaleJakoSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for jako spawns (stacks with `JakoSpawnMultiplier`). |
| `JakoSpawnMultiplier` | float | `1.0` | ≥ `0` | Total normal-monster threat budget for ambient dungeon spawns. |
| `AutoScaleSpecialSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for special spawns (stacks with `SpecialSpawnMultiplier`). |
| `SpecialSpawnMultiplier` | float | `1.0` | ≥ `0` | Special monster budget for periodic spawns and map-placed specials. |
| `AutoScaleTrapSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for trap spawns (stacks with `TrapSpawnMultiplier`). |
| `TrapSpawnMultiplier` | float | `1.0` | ≥ `0` | Map-placed traps: unused alternate markers plus bonus encounters after trigger/kill. |
| `MapPlacedEncounterDelayMinSeconds` | float | `5.0` | ≥ `0` | Shortest wait (seconds) after a map-placed enemy/trap is cleared before the next bonus encounter can spawn at that marker. |
| `MapPlacedEncounterDelayMaxSeconds` | float | `30.0` | ≥ `0` | Longest wait for that random delay. Actual delay is picked between min and max. Must be ≥ `MapPlacedEncounterDelayMinSeconds`. |
| `MapPlacedEncounterMinPlayerDistanceMeters` | float | `10.0` | ≥ `0` | After the delay, hold the spawn until no living players are within this radius (meters) of the marker. Set to `0` to spawn as soon as the delay elapses. |
| `AutoScaleOtherSpawnsByPlayerCount` | bool | `true` | — | Player-count scaling for other spawns (stacks with `OtherSpawnMultiplier`). |
| `OtherSpawnMultiplier` | float | `1.0` | ≥ `0` | Spawn multiplier for other entities (not mimic/boss/jako/special/trap). |

## Loot Multiplicator — `[MimesisPlayerEnhancement_LootMultiplicator]`

**Host-only.** Scale how much loot appears on the map and from enemy deaths, and optionally convert mimic fake drops to real pickup loot. Each multiplier (`1` = vanilla, `2` = double) stacks with its **Auto Scale … By Player Count** toggle using `LootMultiplicatorPlayerCountScaleRate` per player above 4.

**Loot sources:**

| Source | What it affects |
|--------|-----------------|
| **Map** | Loot placed when a dungeon room loads. **Fixed** loot: activates unused loot markers of the same item, scales consumable stack size and `MaxRespawnCount`, and may respawn at the same marker when picked up (uses `MapPlacedEncounterDelay*` and `MapPlacedEncounterMinPlayerDistanceMeters` from Spawn Scaling). **Random** loot pools: scales the dungeon misc budget so more markers fill with weighted random picks. |
| **Drop** | Items from enemy death tables when a monster is killed, plus inventory items dropped on death. Adds extra weighted re-rolls from the same drop table. Consumable stack count is also scaled when the item spawns. Mimics often drop **fake** decoy items from inventory — see `ConvertFakeActorDyingDropChancePercent`. |

Map events / trigger spawns are **not** scaled (vanilla). Does **not** scale: shop purchases, Crow Shop scrap exchange, admin/cheat spawns, or other spawn reasons (e.g. `Release`, `Buying`, `Admin`, `Skill`).

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableLootMultiplicator` | bool | `false` | — | Scale map loot and enemy death drops, and optionally convert mimic fake drops to real loot. Host only. |
| `LootMultiplicatorPlayerCountScaleRate` | float | `0.10` | ≥ `0` | Extra multiplier per player above 4 when an Auto Scale … by Player Count toggle is enabled (0.10 = +10% per extra player, stacks with loot multipliers). Minimum is 0. |
| `AutoScaleMapLootByPlayerCount` | bool | `true` | — | Player-count scaling for map loot. |
| `MapLootMultiplier` | float | `1.0` | ≥ `0` | Multiplier for all map-placed pickup loot. |
| `AutoScaleDropLootByPlayerCount` | bool | `true` | — | Player-count scaling for enemy death drops. |
| `DropLootMultiplier` | float | `1.0` | ≥ `0` | Multiplier for enemy death drops. |
| `LootItemFilterMode` | string | `All` | `All`, `AllowlistOnly`, `BlocklistOnly` | Restrict which item master IDs can spawn (map loot and enemy drops). Does not affect loot multipliers. |
| `LootAllowlist` | string | `""` | — | Comma-separated item master IDs (e.g. `12345,67890`). Used when `LootItemFilterMode` is `AllowlistOnly`. Off-rotation IDs are injected into random map pools. See [LOOT_ITEM_IDS.md](LOOT_ITEM_IDS.md). |
| `LootBlocklist` | string | `""` | — | Comma-separated item master IDs to exclude from spawning. Used when `LootItemFilterMode` is `BlocklistOnly`. See [LOOT_ITEM_IDS.md](LOOT_ITEM_IDS.md). |
| `ConvertFakeActorDyingDropChancePercent` | int | `30` | `0`–`100` | Chance that fake items dropped on enemy death (e.g. mimic inventory decoys) become real pickup loot. `0` = vanilla (vanish on grab), `100` = always real. Monster drop-table loot is already real. |

## Money Multiplier — `[MimesisPlayerEnhancement_MoneyMultiplier]`

**Host-only.** Scales five separate money values. Each has an **Auto Scale … By Player Count** toggle and a multiplier (`1` = vanilla, `2` = double). Player-count scaling uses `MoneyMultiplierPlayerCountScaleRate` per player above 4.

| Money type | What it affects |
|------------|-----------------|
| **Startup** | Starting currency on a new save game or session reset (not when loading a save) |
| **Round goal** | Target currency (quota) required to finish a stage |
| **Scrap / sell value** | Currency from scrapping items and item value counted in the tram toward the quota |
| **Shop buy price** | Maintenance shop and vending-machine kiosk purchase cost |
| **Reinforce price** | Maintenance item reinforcement cost |

Does **not** change saved player balances or shop prices on save load. Shop price multipliers and discount rolls apply on fresh `InitShopItems` rounds (e.g. returning from a dungeon). Complements **Loot Multiplicator** (item quantity) — this feature scales currency amounts and prices, not how many items spawn.

**Shop discounts:** When `ShopDiscountChancePercent` is above `0`, each shop item independently rolls for a discount. Successful rolls pick a random percentage between `ShopDiscountMinPercent` and `ShopDiscountMaxPercent`. At `0` chance, vanilla shop discount tables are used unchanged.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableMoneyMultiplier` | bool | `false` | — | Scale startup money, round goal quota, scrap/sell values, shop buy prices, and reinforce costs. Host only. |
| `MoneyMultiplierPlayerCountScaleRate` | float | `0.10` | ≥ `0` | Extra multiplier per player above 4 when an Auto Scale … by Player Count toggle is enabled (0.10 = +10% per extra player, stacks with money multipliers). Minimum is 0. |
| `AutoScaleStartupMoneyByPlayerCount` | bool | `true` | — | Player-count scaling for startup money. |
| `StartupMoneyMultiplier` | float | `1.0` | ≥ `0` | Startup money multiplier on new save or session reset. Does not apply when loading a save. |
| `AutoScaleRoundGoalMoneyByPlayerCount` | bool | `true` | — | Player-count scaling for stage target currency. |
| `RoundGoalMoneyMultiplier` | float | `1.0` | ≥ `0` | Round goal (quota) multiplier. |
| `AutoScaleScrapSellValueByPlayerCount` | bool | `true` | — | Player-count scaling for scrap/sell values. |
| `ScrapSellValueMultiplier` | float | `1.0` | ≥ `0` | Scrap/sell value multiplier. |
| `AutoScaleShopBuyPriceByPlayerCount` | bool | `true` | — | Player-count scaling for shop buy prices. |
| `ShopBuyPriceMultiplier` | float | `1.0` | ≥ `0` | Maintenance shop and vending-machine kiosk buy price multiplier (`1` = vanilla, `0.1` = 10% of vanilla). Applied when shop items are initialized each maintenance round. |
| `ShopDiscountMinPercent` | int | `0` | `0`–`100` | Minimum discount percentage when a shop discount is rolled. Only used when `ShopDiscountChancePercent` is above `0`. |
| `ShopDiscountMaxPercent` | int | `100` | `0`–`100` | Maximum discount percentage when a shop discount is rolled. Must be ≥ `ShopDiscountMinPercent`. |
| `ShopDiscountChancePercent` | int | `0` | `0`–`100` | Chance per shop item to receive a discount in the min–max range (`0` = vanilla shop discounts, `100` = every item discounted). |
| `AutoScaleReinforcePriceByPlayerCount` | bool | `true` | — | Player-count scaling for reinforce costs. |
| `ReinforcePriceMultiplier` | float | `1.0` | ≥ `0` | Reinforce price multiplier. |

## Dungeon Time — `[MimesisPlayerEnhancement_DungeonTime]`

**Host-only.** When a dungeon shift starts (all members entered), extends the real shift deadline by `ExtraShiftSecondsPerPlayerAboveBaseline` for each player above `DungeonTimeBaselinePlayerCount`. Applied once per dungeon room; late Join Anytime arrivals do not add more time.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableDungeonTime` | bool | `false` | — | Extend dungeon shift length when player count exceeds the baseline. Host only. |
| `DungeonTimeBaselinePlayerCount` | int | `4` | ≥ `1` | No extra shift time at or below this player count (vanilla is 4). Minimum is 1. |
| `ExtraShiftSecondsPerPlayerAboveBaseline` | float | `10.0` | ≥ `0` | Real seconds added to the shift deadline for each player above the baseline. Minimum is 0. |

## Dead Player Features — `[MimesisPlayerEnhancement_DeadPlayerFeatures]`

**Host-only.** Tune mimic possession for dead players. Off by default — set `EnableDeadPlayerFeatures = true` to turn it on.

When you are dead and press **E** to speak through a nearby mimic, vanilla uses a fixed speak window (12 seconds) and a fixed cooldown before the next possession. This feature can randomize the speak window per possession and/or scale the cooldown.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableDeadPlayerFeatures` | bool | `false` | — | Master toggle for dead-spectator mimic possession tuning (speak duration and cooldown). Host only. |
| `EnableMimicPossessionTuning` | bool | `false` | — | Sub-toggle for mimic possession timing tweaks. |
| `RandomizeMimicPossessionDuration` | bool | `false` | — | Roll a random speak duration per E-possession between the min and max seconds below. |
| `MimicPossessionMinTimeSeconds` | float | `12.0` | `0.1`–`120` | Minimum rolled speak duration in seconds (vanilla is 12). |
| `MimicPossessionMaxTimeSeconds` | float | `12.0` | `0.1`–`120` | Maximum rolled speak duration in seconds (vanilla is 12). Must be ≥ `MimicPossessionMinTimeSeconds`. |
| `MimicPossessionCooltimeMultiplier` | float | `1.0` | `0.1`–`10.0` | Post-possession cooldown multiplier (`1` = vanilla, `2` = double). Independent of the random duration toggle. |

## Player Tuning — `[MimesisPlayerEnhancement_PlayerTuning]`

**Host-only.** Scales player movement and stamina on the server. Joining clients do not need the mod — stats sync from the host automatically. Changes apply at runtime when config is saved (host reloads player stats).

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnablePlayerTuning` | bool | `false` | — | Scale player move speed, stamina, and carry weight. Joining clients do not need the mod. Host only. |
| `MoveSpeedMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales walk and run base speed (`1` = vanilla, `2` = double). |
| `MaxStaminaMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales maximum stamina. |
| `StaminaDrainMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales sprint stamina cost per tick (`0.5` = half drain). |
| `StaminaRegenMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales stamina recovered per regen tick. |
| `StaminaRegenDelayMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales wait before regen starts after sprinting (`0.5` = regen starts sooner). |
| `MaxCarryWeightMultiplier` | float | `1.0` | `0.1`–`5.0` | Scales carry capacity before encumbrance slows movement. |

## Dungeon Randomizer — `[MimesisPlayerEnhancement_DungeonRandomizer]`

**Host-only.** Randomizes dungeon selection at four independent layers when enabled. Off by default — set `EnableDungeonRandomizer = true` to turn it on. Each layer has its own toggle so you can randomize only what you want.

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

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableDungeonRandomizer` | bool | `false` | — | Randomize dungeon selection: tram dungeon pick, layout flow, map variant, and procedural seed. Host only. |
| `RandomizeDungeonPick` | bool | `true` | — | Override tram dungeon master ID selection. |
| `DungeonPickPoolMode` | string | `WidenVanilla` | `WidenVanilla`, `AllActiveUniform` | How the tram dungeon pool is built (see table above). |
| `DungeonAllowlist` | string | `""` | — | Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible. |
| `DungeonBlocklist` | string | `""` | — | Comma-separated dungeon master IDs to exclude. |
| `IgnoreDungeonExcludeList` | bool | `true` | — | With `WidenVanilla`, do not exclude recently played dungeons from the tram roll. |
| `RandomizeLayoutFlow` | bool | `true` | — | Pick DunGen layout flows uniformly instead of weighted vanilla rolls. |
| `RandomizeMapVariant` | bool | `true` | — | Pick map variants uniformly from each dungeon's `MapIDs`. |
| `RandomizeDungeonSeed` | bool | `true` | — | Replace the procedural dungeon seed when a dungeon is chosen. |

## Web Dashboard — `[MimesisPlayerEnhancement_WebDashboard]`

**Host process.** Serves a local HTTP dashboard from the game process. Open `http://<ListenAddress>:<ListenPort>/` in a browser (default: `http://127.0.0.1:8001/`). On by default — set `EnableWebDashboard = false` to turn it off. Available whenever the game is running with the web dashboard enabled (not only during an active session).

**Views:**

| View | Who can see it | What it shows |
|------|----------------|---------------|
| **Global Settings** | Host or idle (no session) | Global defaults; written to disk immediately |
| **Settings** | Host in an active save | Per-save-game overrides; applied live in memory; written on vanilla save |
| **Players** | Anyone who can reach the URL | Connected players with avatars, host/local badges, network grade, and ban status |
| **Minimap** | Anyone who can reach the URL | Live player positions during dungeon runs |
| **Leaderboard** | Host only | Per-save-game stats leaderboard (requires **Statistics** enabled) |
| **Player stats** | Host only | Per-player statistics for the active save game (requires **Statistics** enabled) |
| **Moderation** | Host only | Kick, ban, unban, respawn, and heal actions |

**Blind mode** (header toggle, host only): on by default for fairness. Hides alive/dead status, session stats, vitals, and respawn actions to avoid spoilers. While you are dead, blind mode is temporarily lifted so you can review others' stats; it restores automatically on revive unless you turned it off manually.

**Security:** Default bind is `127.0.0.1` (loopback) so only your machine can connect. Binding to another address (e.g. `0.0.0.0` or your LAN IP) exposes the dashboard to anyone on that network — there is no login.

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `EnableWebDashboard` | bool | `true` | — | Turn the local web dashboard on or off. |
| `WebDashboardListenAddress` | string | `127.0.0.1` | — | HTTP bind address. Use `127.0.0.1` for local-only access. |
| `WebDashboardListenPort` | int | `8001` | `1`–`65535` | TCP port for the web dashboard. Listen address and port changes take effect when the config reloads (the HTTP server restarts). |

---

## Legacy key migration

On first load after an upgrade, the mod automatically migrates these obsolete keys into their replacements. **Do not set legacy keys in new configs** — they are ignored or copied once and superseded.

| Legacy key / section | Migrated to |
|----------------------|-------------|
| `[MimesisPlayerEnhancement_MimicTuning]` / `EnableMimicTuning` | `[MimesisPlayerEnhancement_DeadPlayerFeatures]` |
| `MimicPossessionMinTimeMultiplier` / `MimicPossessionMaxTimeMultiplier` | `MimicPossessionMinTimeSeconds` / `MimicPossessionMaxTimeSeconds` (`multiplier × 12`) |
| `FixedSpawnRespawnDelayMinSeconds` / `FixedSpawnRespawnDelayMaxSeconds` / `FixedSpawnRespawnMinPlayerDistanceMeters` | `MapPlacedEncounterDelayMinSeconds` / `MapPlacedEncounterDelayMaxSeconds` / `MapPlacedEncounterMinPlayerDistanceMeters` |
| Per-item loot keys (`MapConsumableLootMultiplier`, `Trigger*`, etc.) | `MapLootMultiplier` and `DropLootMultiplier` (old keys are ignored) |

---

## Example config

Abbreviated example showing section layout (not every key is listed):

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

[MimesisPlayerEnhancement_ExtendedSavegames]
EnableExtendedSavegames = true

[MimesisPlayerEnhancement_SpawnScaling]
EnableSpawnScaling = false
SpawnScalingPlayerCountScaleRate = 0.10
MimicSpawnMultiplier = 1.0

[MimesisPlayerEnhancement_LootMultiplicator]
EnableLootMultiplicator = false
LootMultiplicatorPlayerCountScaleRate = 0.10
MapLootMultiplier = 1.0
DropLootMultiplier = 1.0
ConvertFakeActorDyingDropChancePercent = 30

[MimesisPlayerEnhancement_MoneyMultiplier]
EnableMoneyMultiplier = false
MoneyMultiplierPlayerCountScaleRate = 0.10
StartupMoneyMultiplier = 1.0

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
