# Mimesis Player Enhancement

> **Warning — use at your own risk.** Mods change how the game runs; things can break. Only download MelonLoader from [melonwiki.xyz](https://melonwiki.xyz/). If you do not trust a pre-built DLL, [build from source on GitHub](https://github.com/Kandru/mimesis-player-enhancements).

> **Alpha — under heavy development.** This plugin is not finished and things may not work as expected. Please report bugs and share feedback via [GitHub issues](https://github.com/Kandru/mimesis-player-enhancements/issues).

One plugin for MIMESIS multiplayer: more players, more mimic voices, voice persistence, join-anytime (letting friends join an active lobby - they wait in the tram until the current run ends), statistics, spawn/loot/money scaling, dungeon tweaks, a web dashboard, and more — with a single config file instead of juggling separate mods.

Tested with **MIMESIS 0.3.0** and **MelonLoader 0.7.3**.

## Install

### Mod manager (recommended)

Install through **r2modman**, **Gale**, or another Thunderstore client. The MelonLoader dependency is pulled in automatically.

### Manual

1. Install [MelonLoader 0.7.3+](https://melonwiki.xyz/) on your MIMESIS Steam copy.
2. Extract this package and copy `MimesisPlayerEnhancement.dll` and the `MimesisPlayerEnhancement/` folder into `<MIMESIS>/Mods/`.
3. Start the game once.

Remove older separate mods (MorePlayers, More Voices, MimesisPersistence, JoinAnytime, **MoreMimics**) if you still have them — this mod replaces them.

## Features

| Feature | What it does | Everyone needs the mod? |
|---------|--------------|-------------------------|
| **More Players** | Raise the 4-player cap (default: 32) | No — host only |
| **More Voices** | Record more mimic voice lines per context | No — host only |
| **Persistence** | Keep mimic voices after save/load | No — host only |
| **Join Anytime** | Let friends join an active lobby; they wait in the tram until the current dungeon ends | No — host only |
| **Statistics** | Session stats and leaderboard per save slot | No — host only |
| **Web Dashboard** | Browser UI with live config, stats, minimap, and host moderation | No — host only |
| **Player Announcements** | Toasts for dungeon settings, boss spawns, death stats | No — host only |
| **Spawn Scaling** | Scale mimic/monster spawn budgets by type and player count | No — host only |
| **Loot Multiplicator** | Scale loot quantity by source and item type | No — host only |
| **Money Multiplier** | Scale startup money, goals, scrap, shop prices, and more | No — host only |
| **Dungeon Time** | Extend shift length for larger lobbies | No — host only |
| **Dungeon Randomizer** | Randomize tram pick, layout, map variant, and seed | No — host only |
| **Spectator Transition** | Shorten downed/dead-camera time before spectator | Host + clients for camera timing |

Based on community mods by [NeoMimicry/MorePlayers](https://github.com/NeoMimicry/MorePlayers), [Risikus/More_Voices](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/), [JoanRLopez/MimesisPersistence](https://github.com/JoanRLopez/MimesisPersistence), and [Shlygly/MimesisJoinAnytime](https://github.com/Shlygly/MimesisJoinAnytime). Please support the original authors as well.

## Web dashboard

While you host a session, the mod can serve a **browser dashboard** — a companion web UI so you do not have to dig through config files for every tweak. Set `EnableWebDashboard = true` in config, start a session, then open `http://127.0.0.1:8001/` (default address and port).

The dashboard is not only for admin actions. From the same interface you can:

- **Change settings on the fly** — edit mod config values in real time; changes are saved to the host config file and apply as the game picks them up (some settings, such as player caps, take effect immediately).
- **View statistics** — session leaderboard and per-player stats when Statistics is enabled.
- **Use the minimap** — live dungeon layout and player positions during a run.
- **Moderate the lobby** — kick, ban, and unban players (host only).

By default the dashboard listens on loopback only (`127.0.0.1`). See the [GitHub README](https://github.com/Kandru/mimesis-player-enhancements#web-dashboard--mimesisplayerenhancement_webdashboard) for LAN binding and security notes.

## Config

After the first launch:

```
<MIMESIS>/UserData/MimesisPlayerEnhancement.cfg
```

Edit anytime; most changes apply fully after a restart. Each feature has its own TOML section and master toggle.

Full config reference, build instructions, and contribution guide: [GitHub README](https://github.com/Kandru/mimesis-player-enhancements#config).

## Support

This mod is not a quick patch — it merges several community mods into one maintained package and adds a lot of original work on top: Harmony patches across the game, a web dashboard, session statistics, spawn/loot/money scaling, dungeon tweaks, and ongoing compatibility testing with new MIMESIS releases. That takes a lot of dedicated time to build, debug, and keep working.

If you enjoy playing with it and want to say thanks, a small donation is genuinely appreciated (but never required):

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

The mod stays free and open source either way. Please also support the original mod authors linked above.

## Links

- [GitHub repository](https://github.com/Kandru/mimesis-player-enhancements)
- [Report issues](https://github.com/Kandru/mimesis-player-enhancements/issues)
- [Latest releases](https://github.com/Kandru/mimesis-player-enhancements/releases)
