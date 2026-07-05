## 26.7.6
Hint: this mod is still in Alpha - use at your own risk! These updates can break things that worked before. Still needs extensive testing. In case of any errors please create an issue: https://github.com/Kandru/mimesis-player-enhancements/issues
- added translations
- proper lobby name updates in any case
- improved logging
- improved statistics
- optimized webinterface for larger player counts (to avoid lags)
- webinterface now includes a reset button for config options
- webinterface now automatically toggles the blind-mode
- webinterface now correctly enforces settings value limits
- optimized savegame lifecycle (saves mod-data only when auto-save or manual save button is being pressed)
- possible fix for late joiner who are not always being transported to the train scene between dungeons.
- exclude crow shop detox from loot duplication
- player-count auto scaling (Spawn Scaling, Loot Multiplicator, Money Multiplier) now uses a configurable rate per extra player above 4 (default +10% instead of the old +25% / `players / 4` curve); set `SpawnScalingPlayerCountScaleRate`, `LootMultiplicatorPlayerCountScaleRate`, or `MoneyMultiplierPlayerCountScaleRate` to `0.25` to restore the previous curve
- draft: ability for a dead player to ring the phone to talk to another player directly (just for testing currently. May gets removed).
- draft: allow dead players to spectate monsters in addition to players (just for testing currently. May gets removed).
- draft: extended spectator death player list (just for testing currently. May gets removed).
- draft: properly rename lobby when saved via the save button (and properly load when savegame is being loaded). Same for the lobby public type

## 26.7.5
- fixed an issue with the lobby naming and not showing
- fixed an issue which caused problems with more then 4 players
- fixed an issue which caused problems with more then 10 players as well
- renamed "open" to "join now" to make it clear what the user can do

## 26.7.4
- embedded assets into DLL to avoid issues when copying the DLL via Mod loaders

## 26.7.3
- code optimization (thx Claude Fable5)
- allow Host to heal players
- optimized webinterface & redesigned user list

## 26.7.2
- first working version without major bugs (at least non that I've encountered)
- better webinterface player UI

## Older versions:
- experimenting with the design of the mod, the features and stuff