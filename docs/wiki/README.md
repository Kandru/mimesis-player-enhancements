# Wiki

## Features

| Feature | What it does | Who needs the mod? |
|---------|--------------|-------------------|
| [More Players](./features/more-players.md) | Play with larger groups beyond the four-player limit | Host only |
| [More Voices](./features/more-voices.md) | Let mimics remember many more player voice lines | Host only |
| [Persistence](./features/persistence.md) | Keep mimic voice recordings across gaming sessions | Host only |
| [Join Anytime](./features/join-anytime.md) | Let friends join at the service station or tram between dungeons | Host only |
| [User Interface](./features/user-interface.md) | Save picker, HUD overlays, damage effects, FPS UI, loading screens, landing sounds | Your game only |
| [Custom Assets](./features/custom-assets.md) | Custom loading screen themes and dungeon landing sounds | Your game only |
| [Privacy](./features/privacy.md) | Block automatic telemetry, replay uploads, crash reports, and third-party SDK calls | Your game only |
| [Statistics](./features/statistics.md) | Track deaths, kills, play time, and more per save | Host only |
| [Web Dashboard](./features/web-dashboard.md) | Browser view for players, stats, settings, and moderation | Host only |
| [Player Announcements](./features/player-announcements.md) | On-screen tips for dungeon settings, bosses, and death stats | Host only |
| [Spawn Scaling](./features/spawn-scaling.md) | More or fewer enemies and traps in dungeons | Host only |
| [Loot Multiplicator](./features/loot-multiplicator.md) | Scale map loot and enemy drops; filter items; convert mimic decoys to real loot | Host only |
| [Economy](./features/economy.md) | Adjust starting cash, scrap value, shop prices, and currency retention | Host only |
| [Dungeon Time](./features/dungeon-time.md) | Extra time inside the dungeon when you have more players | Host only |
| [Mimic Tuning](./features/mimic-tuning.md) | Tune mimic voice frequency, inventory copy, and possession timing | Host only |
| [Player Tuning](./features/player-tuning.md) | Change movement speed, stamina, and carry weight (collision pass-through is local per client) | Host only |
| [Dungeon Randomizer](./features/dungeon-randomizer.md) | Randomize dungeons, map variants, and procedural map flavor (24 curated layout styles) | Only the host (lobby-wide) |
| [Weather](./features/weather.md) | Fixed, cycling, or vanilla weather | Only the host (lobby-wide) |

**Host only** — only the host must enable it (or needs the mod, if always-on) for the whole lobby to get the effect. Joiners do not need the mod. **Your game only** — applies on each player's own client; does not change the lobby for others.

## Configuration

Settings live in `<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg`. The game reloads the file while running. See the [full config reference](../CONFIG.md) for every key, default, and apply timing.
