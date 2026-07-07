[![GitHub release](https://img.shields.io/github/release/Kandru/mimesis-player-enhancements?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/mimesis-player-enhancements/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - mimesis-player-enhancements](https://img.shields.io/github/issues/Kandru/mimesis-player-enhancements?color=darkgreen)](https://github.com/Kandru/mimesis-player-enhancements/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

# Mimesis Player Enhancement

![Mimesis Player Enhancement Logo](images/logo.png)

> [!NOTE]  
> Disclosure: this project is being build with help of AI!

> [!CAUTION]  
> **Alpha — under heavy development.** This plugin is not finished and things may not work as expected. Please report bugs and share feedback via [GitHub issues](https://github.com/Kandru/mimesis-player-enhancements/issues).  
> I am not responsible for any damage, data loss, bans, or other problems that come from using this mod. Mods change how the game runs, and things can break.

Mimesis Player Enhancement is a mod for Mimesis that consolidates and extends a lot of tweaks into one maintained package. Hosts can raise the player limits, expand mimic voice recording and persistence (across game sessions), allow players to join at any time, scale spawns/loot/money to match their needs, randomize dungeons, tune player and mimic behavior, control weather, and track session statistics — all from one config file. Clients do not need the mod; only the host does. It also replaces the save-game UI with a scrollable picker (up to 99 manual slots) and always shows the installed mod version in the main and in-game menus.

Tested with **MIMESIS 0.3.0** and **MelonLoader 0.7.3**.

## Features

Most features only need to be installed on the **host** — friends can join without the mod.

| Feature | What it does | Who needs the mod? |
|---------|--------------|-------------------|
| **More Players** | Play with larger groups beyond the four-player limit | Host only |
| **More Voices** | Let mimics remember many more player voice lines | Host only |
| **Persistence** | Keep mimic voice recordings when you save and load | Host only |
| **Join Anytime** | Let friends join after you've already started | Host only |
| **User Interface** | Extended save picker, spectator death list layout, toast duration | Your game only |
| **Statistics** | Track deaths, kills, play time, and more per save | Host only |
| **Web Dashboard** | Browser view for players, stats, settings, and moderation | Host only |
| **Player Announcements** | On-screen tips for dungeon settings, bosses, and death stats | Host only |
| **Spawn Scaling** | More or fewer enemies and traps in dungeons | Host only |
| **Loot Multiplicator** | More or less loot on the map and from enemy drops | Host only |
| **Economy** | Adjust starting cash, quotas, scrap value, shop prices, and optional currency retention | Host only |
| **Dungeon Time** | Extra shift time when you have more players | Host only |
| **Mimic Tuning** | Tune mimic voice frequency, inventory-copy bias, and dead-player possession timing | Host only |
| **Player Tuning** | Change movement speed, stamina, and carry weight | Host only |
| **Dungeon Randomizer** | Randomize which dungeons appear and how they are laid out | Host only |
| **Weather** | Fixed, cycling, or vanilla weather plus synced start time for lighting | Host only |

### More Players

The base game limits sessions to four players. This feature raises that cap so larger groups can play together — for example, up to 32 people in one lobby. Only the host needs the mod; everyone else joins as usual.

### More Voices

Mimics copy things players say and replay them later. The base game only keeps a small number of these voice lines in memory. This feature stores many more recordings so mimics can build a richer library of player voices over a long session.

### Persistence

Without persistence, mimic voice recordings are lost when you quit or load a different save. This feature writes those recordings to disk when you save the game and brings them back the next time you load that save — so your mimics remember voices across play sessions.

### Join Anytime

Normally, friends have to be in the lobby before a run starts. Join Anytime lets people connect after you've already begun. They can join whenever you're not inside a dungeon, then play the next run with the group.

### User Interface

Local presentation options: replace the vanilla New/Load Tram flow with a scrollable save picker (up to 99 manual slots), optionally expand the spectator death list beyond four players, and set how long mod toast messages stay on screen. The mod version is always shown in the main and in-game menu version text.

### Statistics

Keeps track of how each player is doing over time: deaths, kills, mimic voice events, play time, and more. Stats are stored per save game and can be viewed on leaderboards. Useful for friendly competition or just seeing who survived the longest.

### Web Dashboard

While the game is running, open a page in your web browser to see who is connected, watch player positions on a live minimap during dungeon runs, browse leaderboards, and use moderation tools like kick or ban. You can also change mod settings from the browser instead of editing the config file by hand.

### Player Announcements

Shows small on-screen messages to keep everyone informed: a summary of dungeon settings when a shift starts, an alert when a boss spawns, and a recap of your personal stats when you die. These are extra hints on top of the game's own messages.

### Spawn Scaling

Lets you control how busy dungeons feel by changing how many enemies and traps appear. You can set a fixed multiplier for the whole run, scale up automatically when more players are in the session, or mix both. Handy for making large groups feel appropriately challenging.

### Loot Multiplicator

Adjusts how much loot you find on the map and what enemies drop when defeated. You can increase or decrease quantities, scale with player count, and optionally limit which item types are affected. Can also turn some mimic decoy drops into real pickup loot.

### Economy

Changes money-related values for your run: starting cash, the quota you need to hit each round, how much scrapped items are worth, and prices in the maintenance shop. Like other scaling features, amounts can grow automatically when more players join. Optionally keep unspent maintenance currency when departing for the next dungeon instead of losing it.

### Dungeon Time

Adds extra time to each dungeon shift when you have more players than a baseline count. For example, with the default settings, every player above four adds ten seconds to the clock. Gives bigger groups a fairer window to finish a run.

### Mimic Tuning

Tune mimic behavior on the host: how often mimics replay archived player voices, which player inventory they copy for decoy loadouts, and — when you are dead — how long each **E** possession through a mimic lasts and how long you wait before the next one. Voice and inventory subfeatures use Vanilla or Custom modes; possession timing has its own sub-toggle.

### Player Tuning

Tweak how players move and survive: walk/run speed, stamina pool, sprint drain, stamina recovery, and carry weight. Changes apply to everyone in the session from the host side, so joining players pick up the tuned stats automatically.

### Dungeon Randomizer

Shakes up repeat runs by randomizing which dungeon the tram picks, the layout inside that dungeon, which map variant loads, and the procedural seed that shapes rooms. Turn on only the layers you want if you prefer partial randomization over full chaos.

### Weather

Control dungeon weather presets (fixed, cycling, or vanilla), optionally strip random weather rolls, and set the synced in-game start hour for outdoor lighting. Changes apply live during an active dungeon run.

Inspired by community mods like [MorePlayers from NeoMimicry](https://github.com/NeoMimicry/MorePlayers), [MoreVoices from Risikus](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/), [MimesisPersistence from JoanR](https://github.com/JoanRLopez/MimesisPersistence), and [MimesisJoinAnytime from Shlygly](https://github.com/Shlygly/MimesisJoinAnytime). Thanks for your ideas and initial work :)

## Install

### Mod manager (recommended)

Install through [Thunderstore](https://thunderstore.io/c/mimesis/p/Kandru/MimesisPlayerEnhancement/) using **r2modman**, **Gale**, or another Thunderstore client. The MelonLoader dependency is pulled in automatically.

### Manual

1. Install the latest [MelonLoader](https://melonwiki.xyz/) on your MIMESIS Steam copy.
2. Download the [latest release](https://github.com/Kandru/mimesis-player-enhancements/releases).
3. Copy the file into your game folder:  
   `<Mimesis Steam folder>/Mods/MimesisPlayerEnhancement.dll`  
4. Start the game and open http://127.0.0.1:8001

If you used the old separate mods (MorePlayers, More Voices, MimesisPersistence, JoinAnytime, MoreMimics), remove them so they do not fight with this one or disable the feature inside this modification.

If you do not trust a pre-built `.dll`, you can [build this mod yourself](docs/BUILD.md) from the source code here on GitHub.

## Screenshot(s)

### Intuitive savegame UI

![Feature: Savegame UI](images/savegames.jpg)

### Webinterface

#### Webinterface (Blind Mode on)
![Feature: Savegame UI](images/webinterface_players_blind_mode_on.png)

#### Webinterface (Blind Mode off)
![Feature: Savegame UI](images/webinterface_players_blind_mode_off.png)


## Config

After the first launch, the mod creates a config file here:

```
<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg
```

You can edit it anytime. The game reloads the file while running; most settings apply immediately or on the next relevant game event (see [docs/CONFIG.md](docs/CONFIG.md) for apply timing). Unknown sections and keys from older mod versions are removed on load — they are not migrated.

Settings are grouped into TOML sections:

- **`[MimesisPlayerEnhancement]`** — global debug logging
- **`[MimesisPlayerEnhancement_Ui]`** — local UI preferences (save picker, spectator list, toast duration)
- **`[MimesisPlayerEnhancement_FeatureName]`** — one section per gameplay feature (e.g. `[MimesisPlayerEnhancement_MorePlayers]`)

Each gameplay feature section has its own master toggle plus feature-specific options. The web dashboard can edit global defaults and per-save-slot overrides; Web Dashboard listen settings are cfg-file only.

**Full config reference:** [docs/CONFIG.md](docs/CONFIG.md)

## Build from source

See [docs/BUILD.md](docs/BUILD.md).

## Contribute

1. [Fork](https://github.com/Kandru/mimesis-player-enhancements/fork) this repo on GitHub.
2. Create a branch for your change (`git checkout -b my-fix`).
3. Make your edits and run `./scripts/build.sh` to check it compiles (see [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for build and formatting commands).
4. Push your branch and open a [pull request](https://github.com/Kandru/mimesis-player-enhancements/compare) against `main`.
5. Describe what you changed and why. Confirm `./scripts/build.sh` passes locally before opening the PR.

For architecture, feature scaffolding, and agent-oriented guidance, see [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) and [AGENTS.md](AGENTS.md).

Bug fixes and small improvements are welcome. For bigger features, open an issue first so we can agree on the approach.

## License

See [LICENSE](LICENSE). Persistence and More Players code derives from the original community mods — respect their licenses when sharing builds.
