# Privacy

Blocks automatic outbound data from your local game install: Relu session and gameplay logs, replay capture and upload, Unity crash reports, and Krafton creator-code SDK login. **Your game only** — each player must enable it on their own install; it does not change the lobby for others. Settings are global-only (not per-save). Steam lobbies, invites, voice, and multiplayer sync are not blocked. Manual in-game feedback submission still works.

**Config:** [`MimesisPlayerEnhancement_Privacy`](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)

## Configuration

All keys live in `[MimesisPlayerEnhancement_Privacy]`. Sub-flags apply only when `EnablePrivacy` is `true`. The game reloads the cfg file while running; most toggles apply immediately. Restart the game if you change `BlockKraftonGppSdk` after startup.

### `EnablePrivacy`

Master switch for this feature. When `false`, every option below is ignored and vanilla outbound behavior returns.

| Value | Meaning |
|---|---|
| `false` | Privacy blocks are off (vanilla behavior). |
| `true` | Sub-flags below control what is blocked. |

Default: `false`

### `BlockReluTelemetry`

Blocks session lifecycle logs and gameplay event diaries sent to `mimesisapi.relugameservice.com` (login milestones, tram/lobby events, end-of-run stats). Applies live when config changes.

| Value | Meaning |
|---|---|
| `true` | Relu API requests are not sent. |
| `false` | Vanilla Relu telemetry runs (when `EnablePrivacy` is on). |

Default: `true`

### `BlockReplayUpload`

Blocks sending replay files to Relu storage (random sampling, quit/crash upload paths, feedback replay attachments). Does not delete replay files already on disk.

| Value | Meaning |
|---|---|
| `true` | Replay uploads are skipped. |
| `false` | Vanilla replay uploads can run (when `EnablePrivacy` is on). |

Default: `true`

### `BlockReplayRecording`

Prevents replay files from being created (no voice/network capture). Also stops feedback from attaching a replay file. Pair with `BlockReplayUpload` if old files remain on disk.

| Value | Meaning |
|---|---|
| `true` | No new replay files; recording hooks are blocked. |
| `false` | Vanilla replay recording can run (when `EnablePrivacy` is on). |

Default: `true`

### `BlockCrashReports`

Disables Unity crash report uploads from your install. Applies live when config changes.

| Value | Meaning |
|---|---|
| `true` | Unity crash reports are disabled. |
| `false` | Vanilla crash report uploads can run (when `EnablePrivacy` is on). |

Default: `true`

### `StripCrashReportMetadata`

When crash reports stay enabled (`BlockCrashReports` = `false`), ignores `CrashReportHandler.SetUserMetadata` calls so nickname, session info, dungeon seed, and similar fields are not attached.

| Value | Meaning |
|---|---|
| `true` | Crash metadata is stripped. |
| `false` | Vanilla metadata attachment runs (when `EnablePrivacy` is on). |

Default: `true`

### `BlockKraftonGppSdk`

Skips Krafton GPP SDK initialization on startup (creator-code login). Custom skins that depend on this path will not work. Changing this after the game has started may not take effect until restart.

| Value | Meaning |
|---|---|
| `true` | Krafton GPP login does not run. |
| `false` | Vanilla Krafton GPP init runs (when `EnablePrivacy` is on). |

Default: `true`

**Full config keys →** [Privacy](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)
