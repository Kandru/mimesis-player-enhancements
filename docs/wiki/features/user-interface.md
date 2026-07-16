# User Interface

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Local presentation options that do not require other players to install the mod. Each player controls these on their own machine.

## Save picker

`EnableExtendedSaveSlots` replaces the vanilla New/Load Tram flow with a unified scrollable save picker supporting up to 99 manual slots. When off, vanilla Tram menus return.

## Spectator list

`EnableExtendedSpectatorPlayerList` replaces the four-player spectator death list with a two-column layout that scales to screen height. Living players are shown first, then dead; each group is sorted alphabetically. Independent of More Players.

## In-game menu player list

`EnableExtendedInGameMenuPlayerList` shows the ESC menu player list in a right-side overlay (join code on top, scrollable rows with scrollbar). Does not reshape vanilla lobby/public controls.

## Damage health outline

`EnableDamageHealthGlow` tints other players, mimics, and monsters with a health-colored glow for one second after they take damage, then fades out. Color shifts from green (full health) to red (low health); kills use a blood-red tint. Never shown on your own avatar.

## Floating damage

`EnableFloatingDamageNumbers` shows animated floating damage when other players, mimics, or monsters take damage — never on your own avatar. `FloatingDamageDurationSeconds` sets how long numbers stay visible (1–3 seconds).

## Detox indicators

`EnableFloatingDetoxIndicators` shows green floating toxicity reduction (e.g. -27%) when another player drinks detox juice.

## FPS UI

`EnableFpsUi` replaces the top-left health bar and conta gauge with a Counter-Strike-style numeric health readout and toxicity percentage, positioned left of the inventory hotbar. The full-screen conta vignette is unchanged.

## Toast duration

`ModToastDurationSeconds` controls how long mod messages stay visible in the bottom-left corner before fading (minimum 1 second). Vanilla join/leave connect messages are unchanged (~2 seconds).

## Mod version display

The mod version is always prepended to the version text on the main menu and in-game menu. This is not configurable.

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
