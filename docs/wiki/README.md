# Wiki

## Features

| Feature | What it does | Scope |
|---------|--------------|-------|
| [More Players](./features/more-players.md) | Play with larger groups beyond the four-player limit | Host enables for lobby |
| [More Voices](./features/more-voices.md) | Let mimics remember many more player voice lines | Host enables for lobby |
| [Persistence](./features/persistence.md) | Keep mimic voice recordings across gaming sessions | Host enables for lobby |
| [Join Anytime](./features/join-anytime.md) | Let friends join at the service station or tram between dungeons | Host enables for lobby |
| [User Interface](./features/user-interface.md) | Extended save picker, HUD overlays, toast duration, and more | Your game only |
| [Custom Assets](./features/custom-assets.md) | Custom loading screen themes and dungeon landing sounds | Your game only |
| [Privacy](./features/privacy.md) | Block automatic telemetry, replay uploads, crash reports, and third-party SDK calls | Your game only |
| [Statistics](./features/statistics.md) | Track deaths, kills, play time, and more per save | Host enables for lobby |
| [Web Dashboard](./features/web-dashboard.md) | Browser view for players, stats, settings, and moderation | Host enables for lobby |
| [Player Announcements](./features/player-announcements.md) | On-screen tips for dungeon settings, bosses, and death stats | Host enables for lobby |
| [Spawn Scaling](./features/spawn-scaling.md) | More or fewer enemies and traps in dungeons | Host enables for lobby |
| [Loot Multiplicator](./features/loot-multiplicator.md) | More or less loot on the map | Host enables for lobby |
| [Economy](./features/economy.md) | Adjust starting cash, scrap value, shop prices, and currency retention | Host enables for lobby |
| [Dungeon Time](./features/dungeon-time.md) | Extra time inside the dungeon when you have more players | Host enables for lobby |
| [Mimic Tuning](./features/mimic-tuning.md) | Tune mimic voice frequency, inventory copy, and possession timing | Host enables for lobby |
| [Player Tuning](./features/player-tuning.md) | Change movement speed, stamina, and carry weight | Host enables for lobby |
| [Dungeon Randomizer](./features/dungeon-randomizer.md) | Randomize dungeons, map variants, and procedural map flavor (18 layout styles) | Host enables for lobby |
| [Weather](./features/weather.md) | Fixed, cycling, or vanilla weather | Host enables for lobby |

**Host enables for lobby:** only the host must enable this for the whole lobby to get the effect. **Your game only:** only affects your own game client; each player who wants it must enable it on their machine.

## Configuration

Settings live in `<Mimesis Steam folder>/UserData/MimesisPlayerEnhancement.cfg`. The game reloads the file while running. See the [full config reference](../CONFIG.md) for every key, default, and apply timing.
