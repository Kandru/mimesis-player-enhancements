# User Interface

**Scope:** Your game only · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Each player chooses their own presentation on their install. There is no master toggle — turn individual options on or off in `[MimesisPlayerEnhancement_Ui]`. These keys are global-only (not in per-save overrides). The game reloads the config file while running; no restart needed.

The mod version is always prepended to the version text on the main menu and in-game menu (not configurable). When [More Players](more-players.md) is enabled on the **host** and more than four players finish a run, the survival-result screen uses an expanded grid layout instead of the vanilla four-slot dialog — that behavior is **Host only**, not a User Interface key.

## Configuration

### `ModToastDurationSeconds`

How long mod messages stay visible in the bottom-left corner before fading. Vanilla join/leave connect messages are unchanged (~2 seconds).

| Value | Meaning |
|-------|---------|
| `≥ 1` | Duration in seconds |

Default: `5.0`

### `EnableExtendedSaveSlots`

Replace the vanilla New/Load Tram flow with a unified scrollable save picker (up to 99 manual slots). When off, vanilla Tram menus return.

| Value | Meaning |
|-------|---------|
| `true` | Extended save picker |
| `false` | Vanilla Tram menus |

Default: `true`

### `EnableExtendedSpectatorPlayerList`

Replace the four-player spectator death list with a two-column layout that scales to screen height. Living players are shown first, then dead; each group is sorted alphabetically. Independent of More Players.

| Value | Meaning |
|-------|---------|
| `true` | Extended spectator list |
| `false` | Vanilla four-player list |

Default: `true`

### `EnableLoadingWaitPlayerList`

During multiplayer dungeon loading, show a comma-separated player list along the bottom of the screen while the game waits for other players (`STRING_LOADING_WAIT`). Loaded players are white; players still loading are red. Names wrap upward into extra rows inside the custom loading image bounds. The vanilla microphone animation appears next to each name when voice chat is active. The list fades out with the custom loading screen overlay. Works best with custom loading screen themes; see [Custom Assets](./custom-assets.md).

| Value | Meaning |
|-------|---------|
| `true` | Show wait-phase player roster |
| `false` | No roster |

Default: `false`

### `EnableExtendedInGameMenuPlayerList`

Show the ESC menu player list in a right-side overlay (join code on top, scrollable rows with scrollbar). Does not reshape vanilla lobby or public controls. Independent of More Players.

| Value | Meaning |
|-------|---------|
| `true` | Right-side overlay list |
| `false` | Vanilla in-menu list |

Default: `true`

### `EnableDamageHealthGlow`

Tint other players, mimics, and monsters with a health-colored glow for about one second after they take damage, then fade out. Color shifts from green (full health) to red (low health); kills use a blood-red tint. Never shown on your own avatar.

| Value | Meaning |
|-------|---------|
| `true` | Health glow after damage |
| `false` | No glow |

Default: `true`

### `EnableFloatingDamageNumbers`

Show animated floating damage when other players, mimics, or monsters take damage. Never shown on your own avatar. Use `FloatingDamageDurationSeconds` for how long numbers stay on screen.

| Value | Meaning |
|-------|---------|
| `true` | Floating damage numbers |
| `false` | No numbers |

Default: `true`

### `FloatingDamageDurationSeconds`

How long floating damage numbers remain visible. Only applies when `EnableFloatingDamageNumbers` is on.

| Value | Meaning |
|-------|---------|
| `1`–`3` | Duration in seconds |

Default: `2.0`

### `EnableFpsUi`

Replace the top-left health bar and conta gauge with a Counter-Strike-style numeric health readout and toxicity percentage, positioned left of the inventory hotbar. The full-screen conta vignette is unchanged.

| Value | Meaning |
|-------|---------|
| `true` | Numeric vitals HUD |
| `false` | Vanilla bars |

Default: `true`

### `EnableFpsUiInventoryNetWorth`

Show the total sell value of all items in your inventory above the hotbar, styled like the weight readout below it. Independent of `EnableFpsUi` — you can use net worth without the numeric vitals HUD.

| Value | Meaning |
|-------|---------|
| `true` | Inventory net-worth label |
| `false` | No net-worth label |

Default: `true`

### `RoundStartSoundMode`

Replace the dungeon landing melody (`Sound_UI_TramStopBGM_01`) after the tram-stop sting. The tram-stop horn and departure/end-of-run horns are unchanged. Your game only — other players hear their own choice. See [Custom Assets](./custom-assets.md) for adding `.ogg`/`.wav` files.

| Value | Meaning |
|-------|---------|
| `Vanilla` | Original game melody |
| `Random` | Pick from embedded sounds (optionally filtered by `RoundStartSoundRandomPool`) |
| `Specific` | Always use `RoundStartSoundVariant` |

Default: `Random`

### `RoundStartSoundVariant`

Which embedded sound plays when `RoundStartSoundMode` is `Specific`. Must match a file in the mod DLL (filename without extension). Supported formats: `.wav`, `.ogg`. Empty or invalid values reset to the first embedded variant.

Default: first embedded variant id (build-dependent)

### `RoundStartSoundRandomPool`

Limits which sounds `Random` mode can pick. Comma-separated variant ids (no extensions). When empty, any embedded sound may be chosen.

| Value | Meaning |
|-------|---------|
| *(empty)* | All embedded sounds eligible |
| `id1,id2,…` | Only listed ids eligible |

Default: *(empty)*

### `RoundStartSoundVolume`

Playback volume for custom dungeon landing sounds when mode is `Random` or `Specific`. Does not affect the tram-stop sting or `Vanilla` mode.

| Value | Meaning |
|-------|---------|
| `0`–`1` | Volume scale (`0` = silent, `1` = full) |

Default: `0.8`

### `CustomLoadingScreenMode`

Replace scene loading overlay art with embedded PNG themes. Dungeon entry can crossfade from `loading.png`/`background.png` to `wait.png` while waiting for other players (multiplayer only; skipped when solo or when `wait.png` is absent). Your game only — other players see their own themes. See [Custom Assets](./custom-assets.md) for folder layout and image sizes.

| Value | Meaning |
|-------|---------|
| `Vanilla` | Game loading art |
| `Random` | Pick a theme per transition (optionally filtered by `CustomLoadingScreenRandomPool`) |
| `Specific` | Always use `CustomLoadingScreenVariant` when that theme exists for the context |

Default: `Random`

### `CustomLoadingScreenVariant`

Which embedded theme folder to use when `CustomLoadingScreenMode` is `Specific`. Must match a theme that has assets for the current transition context (for example `GTA/DungeonStart/background.png`). Empty or invalid values reset to the first discovered theme.

Default: first embedded theme folder name (build-dependent)

### `CustomLoadingScreenRandomPool`

Limits which themes `Random` mode can pick for each transition context. Comma-separated theme folder names. When empty, any theme available for that context may be chosen.

| Value | Meaning |
|-------|---------|
| *(empty)* | All themes eligible per context |
| `theme1,theme2,…` | Only listed themes eligible |

Default: *(empty)*

### `CustomLoadingScreenMotion`

Enable slow pan/zoom (Ken Burns) motion on single-frame loading images. Frame sequences still animate when authored in the theme. Global `false` disables pan/zoom; per-theme `theme.json` can override further.

| Value | Meaning |
|-------|---------|
| `true` | Pan/zoom on single-frame images |
| `false` | Static single-frame images |

Default: `true`

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
