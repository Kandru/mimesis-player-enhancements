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

Based on community mods by [MorePlayers from NeoMimicry](https://github.com/NeoMimicry/MorePlayers), [MoreVoices from Risikus](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/), [MimesisPersistence from JoanR](https://github.com/JoanRLopez/MimesisPersistence), and [MimesisJoinAnytime from Shlygly](https://github.com/Shlygly/MimesisJoinAnytime). Please support the original authors instead of me :)

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
| `MimicSpawnMultiplier` | float | `1.0` | Mimic spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed mimics also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleMimicSpawnsByPlayerCount` | bool | `true` | When on, multiply mimic spawns by player count ÷ 4 above 4 players (stacks with `MimicSpawnMultiplier`). |
| `BossSpawnMultiplier` | float | `1.0` | Boss spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed bosses also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleBossSpawnsByPlayerCount` | bool | `true` | When on, multiply boss spawns by player count ÷ 4 above 4 players (stacks with `BossSpawnMultiplier`). |
| `JakoSpawnMultiplier` | float | `1.0` | Jako (normal monster) spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed jakos also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleJakoSpawnsByPlayerCount` | bool | `true` | When on, multiply jako spawns by player count ÷ 4 above 4 players (stacks with `JakoSpawnMultiplier`). |
| `SpecialSpawnMultiplier` | float | `1.0` | Special monster spawn budget multiplier (`1` = vanilla, `2` = double). Map-placed specials also use unused markers first, then respawn at the same marker. Minimum is `0`. |
| `AutoScaleSpecialSpawnsByPlayerCount` | bool | `true` | When on, multiply special spawns by player count ÷ 4 above 4 players (stacks with `SpecialSpawnMultiplier`). |
| `TrapSpawnMultiplier` | float | `1.0` | Trap/hazard spawn multiplier for map-placed spawns (`1` = vanilla, `2` = double). Uses unused map markers first, then respawns at the same marker when gone. Minimum is `0`. |
| `AutoScaleTrapSpawnsByPlayerCount` | bool | `true` | When on, multiply trap spawns by player count ÷ 4 above 4 players (stacks with `TrapSpawnMultiplier`). |
| `FixedSpawnRespawnDelayMinSeconds` | float | `5.0` | Minimum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. |
| `FixedSpawnRespawnDelayMaxSeconds` | float | `30.0` | Maximum random delay (seconds) before a map-placed monster or trap respawns at the same marker when all markers are in use. |
| `OtherSpawnMultiplier` | float | `1.0` | Spawn multiplier for other entities (not mimic/boss/jako/special/trap). Minimum is `0`. |
| `AutoScaleOtherSpawnsByPlayerCount` | bool | `true` | When on, multiply other spawns by player count ÷ 4 above 4 players (stacks with `OtherSpawnMultiplier`). |
| `EnableDebugLogging` | bool | `false` | Write extra detail to the MelonLoader console. Useful for troubleshooting; leave off for normal play. |

Example:

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
MimicSpawnMultiplier = 1.0
AutoScaleMimicSpawnsByPlayerCount = true
BossSpawnMultiplier = 1.0
AutoScaleBossSpawnsByPlayerCount = true
JakoSpawnMultiplier = 1.0
AutoScaleJakoSpawnsByPlayerCount = true
SpecialSpawnMultiplier = 1.0
AutoScaleSpecialSpawnsByPlayerCount = true
TrapSpawnMultiplier = 1.0
AutoScaleTrapSpawnsByPlayerCount = true
FixedSpawnRespawnDelayMinSeconds = 5.0
FixedSpawnRespawnDelayMaxSeconds = 30.0
OtherSpawnMultiplier = 1.0
AutoScaleOtherSpawnsByPlayerCount = true
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
