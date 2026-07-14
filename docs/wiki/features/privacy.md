# Privacy

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Privacy`](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)

Mimesis sends some data to servers that are not required to play with friends. The in-game privacy policy do not seem to not turn these automatic uploads off. This feature lets you block them.

User-initiated feedback and bug reports are **never** blocked — the wiki explains what they send so you can decide before submitting.

## What the vanilla game sends (without this mod)

### Relu game servers (`mimesisapi.relugameservice.com`)

| When | What leaves your PC |
|------|---------------------|
| Login | Small “entered lobby” log with device ID and session ID |
| Host creates a tram / join room / enter tram / open public lobby | Similar milestone logs tied to your session |
| End of dungeon or maintenance transitions | Detailed gameplay diary: deaths, loot, HP, contamination, scraps collected, shop purchases, session economics |

### Replay storage (same company, port 22443)

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

**This mod never blocks feedback submission.**

### Unity crash reports

Only when the game crashes:

- Crash dump to Unity’s backend
- Extra metadata: nickname, session info, voice address, dungeon seed, death reason, build version

### Krafton creator-code service

On every launch the game logs into Krafton GPP over Steam (for creator codes).

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

The feedback UI is intentional: nothing is sent until you submit. Privacy toggles do not block text, logs, or metadata in a feedback report.

If `BlockReplayRecording` is on, no replay file exists to attach. If `BlockReplayUpload` is on, replay uploads are blocked even when old replay files remain on disk.

## What accepting the in-game privacy policy does

It only stores a local yes/no preference. It does **not** stop the automatic uploads listed above.

**Full config keys →** [Privacy](../CONFIG.md#privacy--mimesisplayerenhancement_privacy)
