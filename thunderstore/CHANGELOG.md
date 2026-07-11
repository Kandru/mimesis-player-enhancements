Hint: this mod is still in Alpha - use at your own risk! These updates can break things that worked before. Still needs extensive testing. In case of any errors please create an issue: https://github.com/Kandru/mimesis-player-enhancements/issues

## 26.7.8
- switched the web-dashboard to Svelte for a single-page application (looks and feels way better)
- when the game gets updates this mod will be disabled automatically to avoid incompatibilities or bugs/crashes until the user updates it (when any update is available)
- the dead player UI sort the players by alphabet (but living people first)
- new feature to disable player collisions (each player needs the mod for it to work)
- custom menu structure to fix new features inside the main menu or esc menu
- show health bars above players/mimics/monsters when they got hit and also show a floating damage indicator (each player needs the mod for it to work)
- fix: godmode/noclip being auto-disabled by dashboard snapshots and multi-tab blind-mode desync
- fix: late-joiners not always joining the moving tram
- fix: show "In dungeon" without room name in player list
- fix: properly show information that should not be hidden by the blind-mode
- fix: deathmatch winner is excluded from loot multiplier (only give one item when a player has won)
- fix: webinterface wrongly showed "loading tram scene" but where properly spawned already
- draft: allow to properly record voice lines during maintenance, during the tram scene and when possessing a mimic (there seems to be a bug which stops recording a possesed mimic after the first time lol)
- draft: possible fix for small lags when a lot of voices have been recorded (proper caching)
- draft: unlimited zones and money increases (with flatter curve when having a high zone)
- draft: maybe fix to allow clients with the mod installed to use noclip (unfortunately no host-only feature)
- draft: possibility to unify ingame voices (no more difference between indoor/outdoor voices so chatter without area-specific sounds are played everywhere)

## 26.7.7
- improved the loot allow/deny list to be searchable
- properly scale the max scrap value in case of high-value loot due to allow/deny list
- allow the host to delete (offline) users from a savegame
- only add fully connected players to the savegame
- webinterface now uses dropdown for settings which only allow a few possible values instead of text boxes
- better translations to point the player into the right direction
- better debug messages
- removed a testing-value for RouteRetryInterval and set it to a fixed value
- webinterface now uses the browsers language instead of the game language
- renaming MoneyMultiplier feature to Economy
- adding the ability to retain unspend currency between maintenance visits
- draft: add quick-presets to the configuration (still needs tuning and extensive testing!)
- draft: configurable periodic spawn wait (Vanilla / Fixed / Random seconds) for initial delay and wave interval; spawn multipliers no longer shorten wave periods (still needs tuning and extensive testing!)
- draft: host-only Weather feature — fixed/cycle presets, random-roll stripping, start-time presets, real-time config apply (still needs tuning and extensive testing!)
- draft: added godmode and noclip (still needs proper testing!)
- draft: add the ability to change the mimic voice behaviour
- draft: webinterface & features are now refactored properly (still needs tuning and extensive testing!)

## 26.7.6
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
- player-count auto scaling (Spawn Scaling, Loot Multiplicator, Economy) now uses a configurable rate per extra player above 4 (default +10% instead of the old +25% / `players / 4` curve); set `SpawnScalingPlayerCountScaleRate`, `LootMultiplicatorPlayerCountScaleRate`, or `EconomyPlayerCountScaleRate` to `0.25` to restore the previous curve
- draft: properly rename lobby when saved via the save button (and properly load when savegame is being loaded). Same for the lobby public type
- draft: extended spectator death player list (just for testing currently. May gets removed).

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