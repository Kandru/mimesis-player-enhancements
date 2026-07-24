# More Voices

Only the host must enable it for the whole lobby to get the effect. Joining clients do not need the mod. Mimics copy things players say and replay them later; the base game keeps only a small number of voice lines per player. This feature raises those limits and can record in hub scenes or during mimic possession so mimics build a richer library over a long session.

Config section: [`MimesisPlayerEnhancement_MoreVoices`](../CONFIG.md#more-voices--mimesisplayerenhancement_morevoices). Changes apply when you save the config file while the game is running — no restart required.

## Configuration

### `EnableMoreVoices`

Master switch for higher voice pool limits, hub/possession recording, and performance caching. When off, active archives revert to vanilla caps (indoor ≈ 78, deathmatch 20, outdoor 30).

| Value | Meaning |
|---|---|
| `true` | Use the settings below (default). |
| `false` | Vanilla voice limits and recording behavior. |

Default: `true`

### `UnifyIndoorOutdoorVoices`

Merges indoor and outdoor voice storage into one shared pool and lets mimics pick lines recorded in either area. Deathmatch stays on its own cap.

| Value | Meaning |
|---|---|
| `true` | Shared non-deathmatch cap = `MaxIndoorVoiceEvents` + `MaxOutdoorVoiceEvents` (default). |
| `false` | Separate indoor and outdoor caps. |

Default: `true`

### `MaxIndoorVoiceEvents`

Maximum stored mimic voice lines per player for indoor dungeon areas. When `UnifyIndoorOutdoorVoices` is on, this value is added to `MaxOutdoorVoiceEvents` for the shared cap instead of applying alone. Vanilla indoor cap is about 78 (128 total minus 20 deathmatch minus 30 outdoor).

| Value | Meaning |
|---|---|
| Integer ≥ `1` | Event count (default `3000`). |

Default: `3000`

### `MaxDeathMatchVoiceEvents`

Maximum stored mimic voice lines per player in deathmatch. Always separate from indoor/outdoor, even when unification is on. Vanilla default is 20.

| Value | Meaning |
|---|---|
| Integer ≥ `1` | Event count (default `3000`). |

Default: `3000`

### `MaxOutdoorVoiceEvents`

Maximum stored mimic voice lines per player for outdoor areas. When `UnifyIndoorOutdoorVoices` is on, this value is added to `MaxIndoorVoiceEvents` for the shared cap. Vanilla default is 30.

| Value | Meaning |
|---|---|
| Integer ≥ `1` | Event count (default `3000`). |

Default: `3000`

### `RecordVoiceInMaintenance`

Record mimic voice lines in the maintenance room. Vanilla only records inside dungeons. Requires `EnableMoreVoices` and applies in PreGame hub state.

| Value | Meaning |
|---|---|
| `true` | Record in maintenance (default). |
| `false` | Do not record in maintenance. |

Default: `true`

### `RecordVoiceInTram`

Record mimic voice lines in the tram waiting scene (not the return-to-maintenance tram). Vanilla only records inside dungeons. Requires `EnableMoreVoices` and applies in PreGame hub state.

| Value | Meaning |
|---|---|
| `true` | Record in tram waiting (default). |
| `false` | Do not record in tram waiting. |

Default: `true`

### `RecordVoiceDuringMimicPossession`

Keep voice recording active while you possess a mimic and resume recording after possession ends.

| Value | Meaning |
|---|---|
| `true` | Record through possession (default). |
| `false` | Vanilla possession recording behavior. |

Default: `true`

### `EnableVoicePerformanceCache`

Caches warmed voice lists, decoded audio clips, mimic host selection, and player lookups to reduce lag with large voice pools. Requires `EnableMoreVoices`. Turning it off clears caches immediately.

| Value | Meaning |
|---|---|
| `true` | Use performance caches (default). |
| `false` | No caching; vanilla lookup paths. |

Default: `true`

### `VoiceClipCacheMaxEntries`

Maximum decoded mimic voice `AudioClip` objects kept in memory when performance caching is on. Oldest clips are evicted first (LRU). Requires `EnableVoicePerformanceCache`.

| Value | Meaning |
|---|---|
| Integer ≥ `1` | Cache entry count (default `128`). |

Default: `128`
