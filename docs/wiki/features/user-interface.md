# User Interface

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Local presentation options that do not require other players to install the mod. Each player controls these on their own machine.

## Save picker

`EnableExtendedSaveSlots` replaces the vanilla New/Load Tram flow with a unified scrollable save picker supporting up to 99 manual slots. When off, vanilla Tram menus return.

## Spectator list

`EnableExtendedSpectatorPlayerList` replaces the four-player spectator death list with a two-column layout that scales to screen height. Living players are shown first, then dead; each group is sorted alphabetically. Independent of More Players.

## Loading wait player list

`EnableLoadingWaitPlayerList` shows a spectator-style roster on the scene loading screen while the game waits for other players (`STRING_LOADING_WAIT`) in multiplayer. Loaded players are shown in white; players still loading are red. The list includes the vanilla microphone animation when voice chat is active, and fades out with the custom loading screen overlay.

## In-game menu player list

`EnableExtendedInGameMenuPlayerList` shows the ESC menu player list in a right-side overlay (join code on top, scrollable rows with scrollbar). Does not reshape vanilla lobby/public controls.

## Survival result

When [More Players](more-players.md) is enabled and more than four players finish a run, the survival-result dialog uses a custom 6-per-row grid (up to ~90% of screen width) under a centered **DAY X RESULTS** header instead of the vanilla four-slot layout. Up to **24 players** are shown; additional players are omitted. Extra rows stack downward with spacing for name, status, and award text; the whole block (title, grid, and scrap loss) is vertically centered on screen. Scrap loss stays centered under the grid; the dungeon seed is shown in the bottom-left corner. Four players or fewer keep the vanilla dialog.

## Damage health outline

`EnableDamageHealthGlow` tints other players, mimics, and monsters with a health-colored glow for one second after they take damage, then fades out. Color shifts from green (full health) to red (low health); kills use a blood-red tint. Never shown on your own avatar.

## Floating damage

`EnableFloatingDamageNumbers` shows animated floating damage when other players, mimics, or monsters take damage — never on your own avatar. `FloatingDamageDurationSeconds` sets how long numbers stay visible (1–3 seconds).

## FPS UI

`EnableFpsUi` replaces the top-left health bar and conta gauge with a Counter-Strike-style numeric health readout and toxicity percentage, positioned left of the inventory hotbar. The full-screen conta vignette is unchanged.

## Toast duration

`ModToastDurationSeconds` controls how long mod messages stay visible in the bottom-left corner before fading (minimum 1 second). Vanilla join/leave connect messages are unchanged (~2 seconds).

## Mod version display

The mod version is always prepended to the version text on the main menu and in-game menu. This is not configurable.

## Custom loading screens and landing sounds

`CustomLoadingScreenMode` / `CustomLoadingScreenVariant` replace scene loading overlay art with embedded PNG themes. `RoundStartSoundMode` / `RoundStartSoundVariant` replace the dungeon landing melody after the tram stop sting. Both are client-only.

See [Custom Assets](./custom-assets.md) for folder layout, image sizes, and how to add your own themes.

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
