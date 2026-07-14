# Privacy

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Privacy`](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)

Mimesis seems to send some data to servers that are not required to play with friends. The in-game privacy policy do not seem to not turn these automatic uploads off either. This feature lets you block them for your piece of mind.

## What the vanilla game sends (without this mod)

### Relu game servers (`mimesisapi.relugameservice.com`)

| When | What leaves your PC |
|------|---------------------|
| Login | Small “entered lobby” log with device ID and session ID |
| Host creates a tram / join room / enter tram / open public lobby | Similar milestone logs tied to your session |
| End of dungeon or maintenance transitions | Detailed gameplay diary: deaths, loot, HP, contamination, scraps collected, shop purchases, session economics |

### Replay storage (`mimesisapi.relugameservice.com`)

| When | What leaves your PC |
|------|---------------------|
| ~1 in 400 runs with 4+ players | Full session replay (network packets, voice lines, dungeon context) |
| Quit or crash while recording | Replay upload may run synchronously |
| Feedback with replay attached | Replay files uploaded with your report (only if recording was active) |

### Bug and feedback reports (`userreport.relugameservice.com`)

Only when **you** open the in-game feedback form and submit it:

- Title and description you write
- Steam name and ID, country, PC specs (CPU, GPU, RAM, OS)
- Dungeon seed and your world position
- Zipped `player.log` files (the game scans for mod loaders)
- Optional replay file attachment

**This mod does NOT block feedback submission.**

### Unity crash reports

Only when the game crashes:

- Crash dump to Unity’s backend
- Extra metadata: nickname, session info, voice address, dungeon seed, death reason, build version

### Krafton creator-code service

On every launch the game logs into Krafton GPP over Steam (for creator codes). If you disable this you cannot use the custom skins.

### What is NOT covered here

- **Steam login, lobbies, and invites** — needed to find and join friends
- **In-game voice and multiplayer sync** — needed to play together

## What this mod can block

| Config key | Blocks |
|------------|--------|
| `EnablePrivacy` | Master switch — when off, everything below is ignored and vanilla behavior returns |
| `BlockReluTelemetry` | Relu session logs and gameplay event diary |
| `BlockReplayUpload` | Sending replay files to Relu storage |
| `BlockReplayRecording` | Creating replay files at all (prevents feedback replay attachments too) |
| `BlockCrashReports` | Unity crash report uploads |
| `StripCrashReportMetadata` | Metadata attached to crash reports (only relevant if crash reports stay on) |
| `BlockKraftonGppSdk` | Krafton GPP login on startup |

## Feedback and bug reports (your choice)

If `BlockReplayRecording` is on, no replay file exists to attach to the feedback report. If `BlockReplayUpload` is on, replay uploads are blocked even when old replay files remain on disk. If you experience issues with the game first disable this (and other) mods, check again and submit feedback.

**Full config keys →** [Privacy](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)
