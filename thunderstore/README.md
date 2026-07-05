# Mimesis Player Enhancement

> **Note — AI disclosure.** This project is being built with help of AI.

> **Alpha — under heavy development.** This plugin is not finished and things may not work as expected. Please report bugs and share feedback via [GitHub issues](https://github.com/Kandru/mimesis-player-enhancements/issues).

> **Warning — use at your own risk.** I am not responsible for any damage, data loss, bans, or other problems that come from using this mod. Mods change how the game runs, and things can break. Only download MelonLoader from trusted links!

Mimesis Player Enhancement is a mod for Mimesis that consolidates and extends a lot of tweaks into one maintained package. Hosts can raise the player limits, expand mimic voice recording and persistence (across game sessions), allow players to join at any time, scale spawns/loot/money to match their needs, randomize dungeons, tune player and mimic behavior, and track session statistics — all from one config file. Clients do not need the mod; only the host does. It also enables the player to use up to 99 different save games within a new UI.

Additionally, there is also a webinterface listening on http://127.0.0.1:8001 per default after the game has been started. It allows you to manage bigger lobbies (kick, ban, respawn) as well as change settings per savegame or globally.

## Features

| Feature | What it does | Everyone needs the mod? |
|---------|--------------|-------------------------|
| **More Players** | Raise the 4-player limit (default: 32) | No — host only |
| **More Voices** | Record more player voice lines per context (default: 3000 instead of ~150) | No — host only |
| **Persistence** | Save player voice lines to disk | No — host only |
| **Join Anytime** | Let friends join an active lobby whenever players are not inside a dungeon | No — host only |
| **Extended Save Slots** | Unified main-menu save picker with up to 99 manual slots (vanilla: 3) | No — local UI only |
| **Statistics** | Session stats and leaderboards per save slot | No — host only |
| **Web Dashboard** | Browser UI for players, stats, and moderation | No — host only |
| **Player Announcements** | In-game notifications for dungeon settings, boss spawns, and per-map death stats | No — host only |
| **Spawn Scaling** | Scale mimic/monster spawns by type and player count | No — host only |
| **Loot Multiplicator** | Scale loot quantity and limit item types | No — host only |
| **Money Multiplier** | Scale startup money, round goal, and shop buy prices | No — host only |
| **Dungeon Time** | Extend dungeon shift length by X seconds per player above a baseline (default: +10s per player above 4) | No — host only |
| **Dead Player Features** | Mimic possession tuning — randomize speak duration and scale cooldown after E-possess | No — host only |
| **Player Tuning** | Scale player move speed, stamina (max/drain/regen/delay), and max carry weight | No — host only |
| **Dungeon Randomizer** | Randomize tram dungeon pick, layout flow, map variant, and procedural seed | No — host only |

Inspired by community mods like [MorePlayers from NeoMimicry](https://github.com/NeoMimicry/MorePlayers), [MoreVoices from Risikus](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/), [MimesisPersistence from JoanR](https://github.com/JoanRLopez/MimesisPersistence), and [MimesisJoinAnytime from Shlygly](https://github.com/Shlygly/MimesisJoinAnytime). Thanks for your ideas and initial work :)

## Screenshot(s)

### Intuitive savegame UI

![Feature: Savegame UI](https://github.com/Kandru/mimesis-player-enhancements/blob/main/images/savegames.jpg?raw=true)

### Webinterface

#### Webinterface (Blind Mode on)
![Feature: Savegame UI](https://github.com/Kandru/mimesis-player-enhancements/blob/main/images/webinterface_players_blind_mode_on.png?raw=true)

#### Webinterface (Blind Mode off)
![Feature: Savegame UI](https://github.com/Kandru/mimesis-player-enhancements/blob/main/images/webinterface_players_blind_mode_off.png?raw=true)
