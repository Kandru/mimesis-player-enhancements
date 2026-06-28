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
| **More Voices** | Record more mimic voice lines (default: 3000) | No — host only |
| **Persistence** | Keep mimic voices after save/load | No — host only |
| **Join Anytime** | Join a session that already started | **Yes — every player** |
| **Statistics** | Session stats and leaderboard per save slot | No — host only |
| **Spawn Scaling** | Scale mimic/monster spawn budgets by type and player count | No — host only |
| **Loot Multiplicator** | Scale loot quantity by where it comes from and item type | No — host only |

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

### Options

| Key | Type | Default | What it does |
|-----|------|---------|--------------|
| `EnableMorePlayers` | bool | `true` | Turn the higher player cap on or off. When off, the game stays at 4 players. |
| `MaxPlayers` | int | `999` | Max players in a session, host included. `1` = solo, `2` = host + one friend, and so on. Minimum is `1`. |
| `EnableMoreVoices` | bool | `true` | Turn higher voice recording limits on or off. |
| `MaxVoiceEvents` | int | `3000` | How many mimic voice lines each player can store. The normal game limit is much lower. Minimum is `1`. |
| `EnablePersistence` | bool | `true` | Save mimic voices when you save the game and bring them back when you load. |
| `EnableStatistics` | bool | `true` | Track player stats (deaths, kills, voice events, play time, etc.) per save slot. Host only. |
| `SessionReconnectGraceMinutes` | int | `5` | If someone disconnects and rejoins within this many minutes, their stats session continues instead of starting fresh. Minimum is `1`. |
| `ShowStatisticsToasts` | bool | `true` | Show small join/leave/cycle messages in the bottom-left corner when statistics are enabled. |
| `EnableJoinAnytime` | bool | `true` | Let players join after a round has already started. Every player in the lobby needs the mod for this. |
| `EnableSpawnScaling` | bool | `true` | Scale dungeon monster spawn budgets by type. Host only. |
| `AutoScaleMimicSpawnsByPlayerCount` | bool | `true` | When on, multiply mimic spawns by player count ÷ 4 above 4 players (stacks with `MimicSpawnMultiplier`). |
| `MimicSpawnMultiplier` | float | `1.0` | Mimic spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed mimics also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleBossSpawnsByPlayerCount` | bool | `true` | When on, multiply boss spawns by player count ÷ 4 above 4 players (stacks with `BossSpawnMultiplier`). |
| `BossSpawnMultiplier` | float | `1.0` | Boss spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed bosses also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleJakoSpawnsByPlayerCount` | bool | `true` | When on, multiply jako spawns by player count ÷ 4 above 4 players (stacks with `JakoSpawnMultiplier`). |
| `JakoSpawnMultiplier` | float | `1.0` | Jako (normal monster) spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed jakos also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleSpecialSpawnsByPlayerCount` | bool | `true` | When on, multiply special spawns by player count ÷ 4 above 4 players (stacks with `SpecialSpawnMultiplier`). |
| `SpecialSpawnMultiplier` | float | `1.0` | Special monster spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed specials also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleTrapSpawnsByPlayerCount` | bool | `true` | When on, multiply trap spawns by player count ÷ 4 above 4 players (stacks with `TrapSpawnMultiplier`). |
| `TrapSpawnMultiplier` | float | `1.0` | Trap/hazard spawn multiplier for map-placed spawns (`1` = vanilla, `2` = double). Uses unused map markers first, then respawns at the same marker when gone. Minimum is `0`. |
| `FixedSpawnRespawnDelayMinSeconds` | float | `5.0` | Minimum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. |
| `FixedSpawnRespawnDelayMaxSeconds` | float | `30.0` | Maximum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. |
| `AutoScaleOtherSpawnsByPlayerCount` | bool | `true` | When on, multiply other spawns by player count ÷ 4 above 4 players (stacks with `OtherSpawnMultiplier`). |
| `OtherSpawnMultiplier` | float | `1.0` | Spawn multiplier for other entities (not mimic/boss/jako/special/trap). Minimum is `0`. |
| `EnableDebugLogging` | bool | `false` | Write extra detail to the MelonLoader console. Useful for troubleshooting; leave off for normal play. |

### Loot Multiplicator

Host-only. Each setting is a **source × item type** pair. The multiplier (`1` = vanilla, `2` = double) stacks with the matching **Auto Scale … By Player Count** toggle: above 4 players, effective loot is multiplied by player count ÷ 4 (e.g. 8 players → ×2 on top of your multiplier).

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

Each source has three multiplier + auto-scale pairs (Consumable, Equipment, Miscellany). Example keys:

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

Example (loot section only):

```toml
[MimesisPlayerEnhancement]
EnableLootMultiplicator = true
AutoScaleMapConsumableLootByPlayerCount = true
MapConsumableLootMultiplier = 1.5
AutoScaleDropEquipmentLootByPlayerCount = true
DropEquipmentLootMultiplier = 2.0
```

Example (full config):

```toml
[MimesisPlayerEnhancement]
EnableMorePlayers = true
MaxPlayers = 32
EnableMoreVoices = true
MaxVoiceEvents = 3000
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
AutoScaleOtherSpawnsByPlayerCount = true
OtherSpawnMultiplier = 1.0
EnableLootMultiplicator = true
AutoScaleMapConsumableLootByPlayerCount = true
MapConsumableLootMultiplier = 1.0
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
