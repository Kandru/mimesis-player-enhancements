# User Interface

**Local only** — each player needs to enable this in the settings for this to take effect.

Adjusts the user interface in different parts to make the game more appealing or usable with more players.

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

During multiplayer dungeon loading, show a spaced player list centered in a bottom roster strip on `wait.png` (~**70px** tall at 1080p, **10px** from bottom/left/right) while the game waits for other players (`STRING_LOADING_WAIT`). Loaded players are white; players still loading are red; names turn green while that player is talking. Names wrap into at most **2** rows centered in that strip. The list fades in with the wait-image crossfade and fades out with the custom loading screen overlay. Works best with custom loading screen themes; see [Custom Assets](./custom-assets.md).

| Value | Meaning |
|-------|---------|
| `true` | Show wait-phase player roster |
| `false` | No roster |

Default: `true`

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

### `DiscoBallSoundMode`

Replace the loop that plays when the tram disco ball is turned on (`Sound_LevelObject_Tram_Discoball_Partymusic_Loop`). The mirror-ball button click stays vanilla. Your game only — other players hear their own choice. See [Custom Assets](./custom-assets.md) for adding `.ogg`/`.wav` files. When no tracks are embedded, `Random` and `Specific` fall back to vanilla.

| Value | Meaning |
|-------|---------|
| `Vanilla` | Original game loop |
| `Random` | Pick one track per dungeon (optionally filtered by `DiscoBallSoundRandomPool`); same track if you toggle the ball off and on |
| `Specific` | Always use `DiscoBallSoundVariant` |

Default: `Random`

### `DiscoBallSoundVariant`

Which embedded track plays when `DiscoBallSoundMode` is `Specific`. Must match a file in the mod DLL (filename without extension). Supported formats: `.wav`, `.ogg`. Empty or invalid values reset to the first embedded variant.

Default: first embedded variant id (build-dependent)

### `DiscoBallSoundRandomPool`

Limits which tracks `Random` mode can pick. Comma-separated variant ids (no extensions). When empty, any embedded track may be chosen. The pick is held for the whole dungeon.

| Value | Meaning |
|-------|---------|
| *(empty)* | All embedded tracks eligible |
| `id1,id2,…` | Only listed ids eligible |

Default: *(empty)*

### `DiscoBallSoundVolume`

Playback volume for custom disco ball music when mode is `Random` or `Specific`. Internally scaled to roughly match vanilla MasterAudio bus loudness (custom clips that look equally loud in an editor still sounded hotter through a raw `AudioSource`). Does not affect `Vanilla` mode.

| Value | Meaning |
|-------|---------|
| `0`–`1` | Volume scale (`0` = silent, `1` = loudest custom level) |

Default: `0.8`

### `SpectatorVoiceBalanceMode`

While you are dead and spectating, rebalance remote player voice chat on your client only. Other players are unaffected. Mimic possession is not handled here — the game already mutes dead VoIP on that channel.

| Value | Meaning |
|-------|---------|
| `Vanilla` | No changes |
| `SpeechDucking` | Duck living players to `SpectatorVoiceDuckLevel` after a dead player has spoken continuously for more than 0.2 s |
| `StaticAttenuation` | Keep living players at `SpectatorVoiceAttenuation` of their preferred volume |

Dead players are priority; living players are attenuated or ducked so dead chat is easier to hear.

Default: `Vanilla`

### `SpectatorVoiceAttenuation`

Volume fraction kept for the non-priority group in `StaticAttenuation` mode.

| Value | Meaning |
|-------|---------|
| `0`–`1` | Fraction of each player's ESC-menu preferred volume |

Default: `0.8`

### `SpectatorVoiceDuckLevel`

Volume fraction kept for the non-priority group in `SpeechDucking` mode while the priority group is talking.

| Value | Meaning |
|-------|---------|
| `0`–`1` | Fraction of each player's ESC-menu preferred volume |

Default: `0.2`

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
