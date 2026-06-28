[![GitHub release](https://img.shields.io/github/release/Kandru/mimesis-player-enhancements?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/mimesis-player-enhancements/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - mimesis-player-enhancements](https://img.shields.io/github/issues/Kandru/mimesis-player-enhancements?color=darkgreen)](https://github.com/Kandru/mimesis-player-enhancements/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

# Mimesis Player Enhancement

> **Warning — use at your own risk.** I am not responsible for any damage, data loss, bans, or other problems that come from using this mod. Mods change how the game runs, and things can break.
>
> Only download software from official sources — for example the real [MelonLoader](https://melonwiki.xyz/) installer, not random repacks. Fake downloads can contain viruses or malware.
>
> If you do not trust a pre-built `.dll`, you can [build this mod yourself](#build-from-source) from the source code here on GitHub. That takes some basic dev setup, but you know exactly what you are running.

You want more from MIMESIS multiplayer — more players, more voice lines, voices that stick around after saving, joining friends mid-round, and stats that actually track who did what. This mod bundles those tweaks into **one plugin** with a single config file, instead of juggling several separate mods.

Tested with **MIMESIS 0.3.0** and **MelonLoader 0.7.3**.

## Features

| Feature | What it does | Everyone needs the mod? |
|---------|--------------|-------------------------|
| **More Players** | Raise the 4-player cap (default: 999) | No — host only |
| **More Voices** | Record more mimic voice lines per context (default: 3000 each for indoor, deathmatch, outdoor) | No — host only |
| **Persistence** | Keep mimic voices after save/load | No — host only |
| **Join Anytime** | Join a session that already started | **Yes — every player** |
| **Statistics** | Session stats and leaderboard per save slot | No — host only |
| **Spawn Scaling** | Scale mimic/monster spawn budgets by type and player count | No — host only |
| **Loot Multiplicator** | Scale loot quantity by where it comes from and item type | No — host only |
| **Money Multiplier** | Scale startup money, round goal, scrap/sell values, shop buy prices, shop item count, and reinforce costs | No — host only |
| **Dungeon Time** | Extend dungeon shift length by real seconds per player above a baseline (default: +10s per player above 4) | No — host only |
| **Spectator Transition** | Shorten downed time and dead-camera duration before spectator mode | No — host for down time; dead camera on each machine with the mod |

Based on community mods by [MorePlayers from NeoMimicry](https://github.com/NeoMimicry/MorePlayers), [MoreVoices from Risikus](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/), [MimesisPersistence from JoanR](https://github.com/JoanRLopez/MimesisPersistence), and [MimesisJoinAnytime from Shlygly](https://github.com/Shlygly/MimesisJoinAnytime). Please support the original authors as well :)

## Install

1. Install the latest [MelonLoader](https://melonwiki.xyz/) on your MIMESIS Steam copy.
2. Download `MimesisPlayerEnhancement.dll` from the [latest release](https://github.com/Kandru/mimesis-player-enhancements/releases).
3. Copy the file into your game folder:  
   `<Mimesis Steam folder>/Mods/MimesisPlayerEnhancement.dll`
4. Start the game once.

If you used the old separate mods (MorePlayers, More Voices, MimesisPersistence, JoinAnytime, **MoreMimics**), remove them so they do not fight with this one. Spawn scaling in this mod replaces MoreMimics.

## Config

After the first launch, the mod creates a config file here:

```
<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg
```

You can edit it anytime. The game reloads the file while running, but **most changes only fully apply after a restart**. Some settings may not update correctly until you quit and start again.

All settings live in one `[MimesisPlayerEnhancement]` section. Each feature below has its own master toggle plus feature-specific options.

**Upgrade note:** `MaxVoiceEvents` was replaced by `MaxIndoorVoiceEvents`, `MaxDeathMatchVoiceEvents`, and `MaxOutdoorVoiceEvents`. Set all three keys manually; there is no automatic migration from the old setting.

**Player-count scaling:** Several features offer an **Auto Scale … By Player Count** toggle. When enabled and the session has more than 4 players, the effective value is multiplied by player count ÷ 4 (e.g. 8 players → ×2 on top of your base multiplier).

### More Players

Host-only. Raise the vanilla 4-player session cap.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMorePlayers` | bool | `true` | Turn the higher player cap on or off. When off, the game stays at 4 players. |
| `MaxPlayers` | int | `999` | Max players in a session, host included. `1` = solo, `2` = host + one friend, and so on. Minimum is `1`. |

### More Voices

Host-only. Raise per-player mimic voice recording limits.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMoreVoices` | bool | `true` | Turn higher voice recording limits on or off. |
| `MaxIndoorVoiceEvents` | int | `3000` | How many mimic voice lines each player can store in indoor dungeon runs. Minimum is `1`. |
| `MaxDeathMatchVoiceEvents` | int | `3000` | How many mimic voice lines each player can store in deathmatch. Minimum is `1`. |
| `MaxOutdoorVoiceEvents` | int | `3000` | How many mimic voice lines each player can store outdoors. Minimum is `1`. |

### Persistence

Host-only. Keep mimic voice recordings across save and load.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnablePersistence` | bool | `true` | Save mimic voices when you save the game and bring them back when you load. |

### Statistics

Host-only. Track session stats and a per-save-slot leaderboard (deaths, kills, voice events, play time, and more).

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableStatistics` | bool | `true` | Track player stats per save slot. |
| `SessionReconnectGraceMinutes` | int | `5` | If someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh. Minimum is `1`. |
| `ShowStatisticsToasts` | bool | `true` | Show small join/leave/cycle messages in the bottom-left corner when statistics are enabled. Does not replace the game's own connect messages. |

### Join Anytime

**Every player in the lobby needs the mod** for this feature. Lets players join a session after a round has already started.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableJoinAnytime` | bool | `true` | Let players join after a round has already started. |

### Spawn Scaling

Host-only. Scale dungeon monster and trap spawn budgets by type. Map-placed mimics, bosses, jakos, specials, and traps use unused markers first, then respawn at the same marker when all markers are in use.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableSpawnScaling` | bool | `true` | Master toggle for all spawn scaling below. |
| `AutoScaleMimicSpawnsByPlayerCount` | bool | `true` | Player-count scaling for mimic spawns (stacks with `MimicSpawnMultiplier`). |
| `MimicSpawnMultiplier` | float | `1.0` | Mimic spawn budget multiplier (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleBossSpawnsByPlayerCount` | bool | `true` | Player-count scaling for boss spawns (stacks with `BossSpawnMultiplier`). |
| `BossSpawnMultiplier` | float | `1.0` | Boss spawn budget multiplier (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleJakoSpawnsByPlayerCount` | bool | `true` | Player-count scaling for jako spawns (stacks with `JakoSpawnMultiplier`). |
| `JakoSpawnMultiplier` | float | `1.0` | Jako (normal monster) spawn budget multiplier (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleSpecialSpawnsByPlayerCount` | bool | `true` | Player-count scaling for special spawns (stacks with `SpecialSpawnMultiplier`). |
| `SpecialSpawnMultiplier` | float | `1.0` | Special monster spawn budget multiplier (`1` = vanilla, `2` = double). Minimum is `0`. |
| `AutoScaleTrapSpawnsByPlayerCount` | bool | `true` | Player-count scaling for trap spawns (stacks with `TrapSpawnMultiplier`). |
| `TrapSpawnMultiplier` | float | `1.0` | Trap/hazard spawn multiplier for map-placed spawns (`1` = vanilla, `2` = double). Minimum is `0`. |
| `FixedSpawnRespawnDelayMinSeconds` | float | `5.0` | Minimum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. Minimum is `0`. |
| `FixedSpawnRespawnDelayMaxSeconds` | float | `30.0` | Maximum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. Must be ≥ `FixedSpawnRespawnDelayMinSeconds`. |
| `FixedSpawnRespawnMinPlayerDistanceMeters` | float | `10.0` | After the respawn delay, wait until no players are within this distance (meters) before spawning at the marker. Set to `0` to respawn immediately. Minimum is `0`. |
| `AutoScaleOtherSpawnsByPlayerCount` | bool | `true` | Player-count scaling for other spawns (stacks with `OtherSpawnMultiplier`). |
| `OtherSpawnMultiplier` | float | `1.0` | Spawn multiplier for other entities (not mimic/boss/jako/special/trap). Minimum is `0`. |

### Loot Multiplicator

Host-only. Each setting is a **source × item type** pair. The multiplier (`1` = vanilla, `2` = double) stacks with the matching **Auto Scale … By Player Count** toggle when player-count scaling is enabled.

**Loot sources** — where the item comes from:

| Prefix | Source | What it affects |
|--------|--------|-----------------|
| **Map** | Map spawn points | Loot placed when a dungeon room loads. **All** map loot slots: scales `StackCount` and `MaxRespawnCount`. **Fixed** loot (a specific item tied to a marker): may also activate unused loot markers of the same item and respawn at the same marker when picked up (uses `FixedSpawnRespawnDelay*` from Spawn Scaling). **Random** loot pools (weighted mix of items): only stack/respawn scaling; extra markers are not added. Random pools use the **dominant** item type in the pool to pick which multiplier applies. |
| **Drop** | Enemy death drops | Items from enemy death tables when killed. Duplicates extra item IDs in the drop list (more separate drops). Also tries to scale stack count when the item spawns (`ActorDying`); stack scaling is reliable for **consumables**, less so for equipment/miscellany. |
| **Trigger** | Map events / trigger volumes | Items spawned by map events (`EventAction` only). Tries to scale stack count when the item appears; stack scaling is reliable for **consumables**, less so for equipment/miscellany. |

**Item types** — from the game's item data (`Consumable`, `Equipment`, `Miscellany`):

| Type | Examples |
|------|----------|
| **Consumable** | Ammo, healing, and other used-up items |
| **Equipment** | Tools, weapons, and gear you equip |
| **Miscellany** | Other pickups — keys, misc objects, etc. Unknown items fall back to Miscellany. |

Each source has three multiplier + auto-scale pairs (Consumable, Equipment, Miscellany):

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableLootMultiplicator` | bool | `true` | Master toggle for all loot scaling below. |
| `AutoScaleMapConsumableLootByPlayerCount` | bool | `true` | Player-count scaling for map consumables (see tables above). |
| `MapConsumableLootMultiplier` | float | `1.0` | Base multiplier for map consumables. Minimum is `0`. |
| `AutoScaleMapEquipmentLootByPlayerCount` | bool | `true` | Player-count scaling for map equipment. |
| `MapEquipmentLootMultiplier` | float | `1.0` | Base multiplier for map equipment. Minimum is `0`. |
| `AutoScaleMapMiscellanyLootByPlayerCount` | bool | `true` | Player-count scaling for map miscellany. |
| `MapMiscellanyLootMultiplier` | float | `1.0` | Base multiplier for map miscellany. Minimum is `0`. |
| `AutoScaleDropConsumableLootByPlayerCount` | bool | `true` | Player-count scaling for consumables from enemy deaths. |
| `DropConsumableLootMultiplier` | float | `1.0` | Base multiplier for consumable death drops. Minimum is `0`. |
| `AutoScaleDropEquipmentLootByPlayerCount` | bool | `true` | Player-count scaling for equipment from enemy deaths. |
| `DropEquipmentLootMultiplier` | float | `1.0` | Base multiplier for equipment death drops. Minimum is `0`. |
| `AutoScaleDropMiscellanyLootByPlayerCount` | bool | `true` | Player-count scaling for miscellany from enemy deaths. |
| `DropMiscellanyLootMultiplier` | float | `1.0` | Base multiplier for miscellany death drops. Minimum is `0`. |
| `AutoScaleTriggerConsumableLootByPlayerCount` | bool | `true` | Player-count scaling for consumables from map events/triggers. |
| `TriggerConsumableLootMultiplier` | float | `1.0` | Base multiplier for event/trigger consumables. Minimum is `0`. |
| `AutoScaleTriggerEquipmentLootByPlayerCount` | bool | `true` | Player-count scaling for equipment from map events/triggers. |
| `TriggerEquipmentLootMultiplier` | float | `1.0` | Base multiplier for event/trigger equipment. Minimum is `0`. |
| `AutoScaleTriggerMiscellanyLootByPlayerCount` | bool | `true` | Player-count scaling for miscellany from map events/triggers. |
| `TriggerMiscellanyLootMultiplier` | float | `1.0` | Base multiplier for event/trigger miscellany. Minimum is `0`. |

Does **not** scale: items you release from inventory, shop purchases, admin/cheat spawns, or other spawn reasons (e.g. `Release`, `Buying`, `Admin`, `Skill`). Map loot is scaled once at room load — not again when it spawns in the world.

### Money Multiplier

Host-only. Scales six separate money values. Each has an **Auto Scale … By Player Count** toggle and a multiplier (`1` = vanilla, `2` = double). Minimum multiplier is `0`.

| Money type | What it affects |
|------------|-----------------|
| **Startup** | Starting currency on a new game or maintenance session reset |
| **Round goal** | Target currency (quota) required to finish a stage |
| **Scrap / sell value** | Currency from scrapping items and item value counted in the tram toward the quota |
| **Shop buy price** | Maintenance shop and vending-machine kiosk purchase cost (shown in UI and charged on buy) |
| **Shop items** | Number of unique items offered in the maintenance shop |
| **Reinforce price** | Maintenance item reinforcement cost |

Does **not** change saved player balances on load or mid-round currency pickups. Complements **Loot Multiplicator** (item quantity) — this mod scales currency amounts and prices, not how many items spawn.

**Shop discounts:** When `ShopDiscountChancePercent` is above `0`, each shop item independently rolls for a discount. Successful rolls pick a random percentage between `ShopDiscountMinPercent` and `ShopDiscountMaxPercent`, update the item's sale price, and sync vending-machine displays. At `0` chance, vanilla shop discount tables are used unchanged.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMoneyMultiplier` | bool | `true` | Master toggle for all money scaling below. |
| `AutoScaleStartupMoneyByPlayerCount` | bool | `true` | Player-count scaling for startup money. |
| `StartupMoneyMultiplier` | float | `1.0` | Startup money multiplier. Minimum is `0`. |
| `AutoScaleRoundGoalMoneyByPlayerCount` | bool | `true` | Player-count scaling for stage target currency. |
| `RoundGoalMoneyMultiplier` | float | `1.0` | Round goal (quota) multiplier. Minimum is `0`. |
| `AutoScaleScrapSellValueByPlayerCount` | bool | `true` | Player-count scaling for scrap/sell values. |
| `ScrapSellValueMultiplier` | float | `1.0` | Scrap/sell value multiplier. Minimum is `0`. |
| `AutoScaleShopBuyPriceByPlayerCount` | bool | `true` | Player-count scaling for shop buy prices. |
| `ShopBuyPriceMultiplier` | float | `1.0` | Maintenance shop and vending-machine kiosk buy price multiplier (`1` = vanilla, `0.1` = 10% of vanilla). Applied when shop items are initialized each maintenance round. Minimum is `0`. |
| `AutoScaleShopItemsByPlayerCount` | bool | `true` | Player-count scaling for shop item count. |
| `ShopItemsMultiplier` | float | `1.0` | Number of unique maintenance shop items (`1` = vanilla, `2` = double). Extra items are rolled from vending-machine shop groups on the map. Minimum is `0`. |
| `ShopDiscountMinPercent` | int | `0` | Minimum discount percentage when a shop discount is rolled (`0`–`100`). Only used when `ShopDiscountChancePercent` is above `0`. |
| `ShopDiscountMaxPercent` | int | `100` | Maximum discount percentage when a shop discount is rolled (`0`–`100`). Must be ≥ `ShopDiscountMinPercent`. |
| `ShopDiscountChancePercent` | int | `0` | Chance per shop item to receive a discount in the min–max range (`0` = vanilla shop discounts, `100` = every item discounted). |
| `AutoScaleReinforcePriceByPlayerCount` | bool | `true` | Player-count scaling for reinforce costs. |
| `ReinforcePriceMultiplier` | float | `1.0` | Reinforce price multiplier. Minimum is `0`. |

### Dungeon Time

Host-only. When a dungeon shift starts (all members entered), extends the real shift deadline by `ExtraShiftSecondsPerPlayerAboveBaseline` for each player above `DungeonTimeBaselinePlayerCount`. Applied once per dungeon room; late Join Anytime arrivals do not add more time.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableDungeonTime` | bool | `true` | Master toggle for shift extension. |
| `DungeonTimeBaselinePlayerCount` | int | `4` | No extra shift time at or below this player count. Minimum is `1`. |
| `ExtraShiftSecondsPerPlayerAboveBaseline` | float | `10.0` | Real seconds added to the shift deadline per player above the baseline. Minimum is `0`. |

### Spectator Transition

Shortens how long players stay downed before spectator mode and how long the local dead-camera transition runs. Multipliers use the same scale as spawn/loot settings: `1` = vanilla, `0.5` = half, `0` = instant.

`DyingWaitTimeMultiplier` is **host only** (server-authoritative down time). It also shortens the window teammates have to revive you. `DeadCameraDurationMultiplier` applies on each machine that has the mod loaded — for a consistent feel, use the same values on host and clients.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableSpectatorTransition` | bool | `true` | Master toggle for shorter death-to-spectator timing. |
| `DyingWaitTimeMultiplier` | float | `1.0` | Scales server down/dying time before spectator (`0` = instant). Host only. Also shortens the teammate revive window. Minimum is `0`. |
| `DeadCameraDurationMultiplier` | float | `1.0` | Scales local dead-camera blend and duration before spectator (`0` = instant). Minimum is `0`. |

### Debug Logging

Writes extra diagnostic detail to the MelonLoader console. Leave off for normal play.

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableDebugLogging` | bool | `false` | Emit verbose diagnostic lines to the MelonLoader console. Useful for troubleshooting. |

### Example config

Full default config with all features enabled:

```toml
[MimesisPlayerEnhancement]
EnableMorePlayers = true
MaxPlayers = 32
EnableMoreVoices = true
MaxIndoorVoiceEvents = 3000
MaxDeathMatchVoiceEvents = 3000
MaxOutdoorVoiceEvents = 3000
EnablePersistence = true
EnableStatistics = true
SessionReconnectGraceMinutes = 5
ShowStatisticsToasts = true
EnableJoinAnytime = true
EnableSpawnScaling = true
AutoScaleMimicSpawnsByPlayerCount = true
MimicSpawnMultiplier = 1.0
AutoScaleBossSpawnsByPlayerCount = true
BossSpawnMultiplier = 1.0
AutoScaleJakoSpawnsByPlayerCount = true
JakoSpawnMultiplier = 1.0
AutoScaleSpecialSpawnsByPlayerCount = true
SpecialSpawnMultiplier = 1.0
AutoScaleTrapSpawnsByPlayerCount = true
TrapSpawnMultiplier = 1.0
FixedSpawnRespawnDelayMinSeconds = 5.0
FixedSpawnRespawnDelayMaxSeconds = 30.0
FixedSpawnRespawnMinPlayerDistanceMeters = 10.0
AutoScaleOtherSpawnsByPlayerCount = true
OtherSpawnMultiplier = 1.0
EnableLootMultiplicator = true
AutoScaleMapConsumableLootByPlayerCount = true
MapConsumableLootMultiplier = 1.0
EnableMoneyMultiplier = true
AutoScaleStartupMoneyByPlayerCount = true
StartupMoneyMultiplier = 1.0
AutoScaleRoundGoalMoneyByPlayerCount = true
RoundGoalMoneyMultiplier = 1.0
AutoScaleScrapSellValueByPlayerCount = true
ScrapSellValueMultiplier = 1.0
AutoScaleShopBuyPriceByPlayerCount = true
ShopBuyPriceMultiplier = 1.0
AutoScaleShopItemsByPlayerCount = true
ShopItemsMultiplier = 1.0
ShopDiscountMinPercent = 0
ShopDiscountMaxPercent = 100
ShopDiscountChancePercent = 0
AutoScaleReinforcePriceByPlayerCount = true
ReinforcePriceMultiplier = 1.0
EnableDungeonTime = true
DungeonTimeBaselinePlayerCount = 4
ExtraShiftSecondsPerPlayerAboveBaseline = 10.0
EnableSpectatorTransition = true
DyingWaitTimeMultiplier = 1.0
DeadCameraDurationMultiplier = 1.0
EnableDebugLogging = false
```

## Build from source

You need [.NET SDK 8+](https://dotnet.microsoft.com/download). You do **not** need MIMESIS installed to compile.

```bash
chmod +x scripts/*.sh
./scripts/bootstrap-deps.sh   # first time only — downloads build dependencies
./scripts/build.sh            # → dist/debug/MimesisPlayerEnhancement.dll
./scripts/build.sh Release    # → dist/prod/MimesisPlayerEnhancement.dll
```

To copy the built DLL straight into your game for testing:

```bash
COPY_TO_MODS=true MIMESIS_PATH="/path/to/MIMESIS" ./scripts/build.sh
```

## Contribute

1. [Fork](https://github.com/Kandru/mimesis-player-enhancements/fork) this repo on GitHub.
2. Create a branch for your change (`git checkout -b my-fix`).
3. Make your edits and run `./scripts/build.sh` to check it compiles.
4. Push your branch and open a [pull request](https://github.com/Kandru/mimesis-player-enhancements/compare) against `main`.
5. Describe what you changed and why. CI will build your PR automatically.

Bug fixes and small improvements are welcome. For bigger features, open an issue first so we can agree on the approach.

## License

See [LICENSE](LICENSE). Persistence and More Players code derives from the original community mods — respect their licenses when sharing builds.
