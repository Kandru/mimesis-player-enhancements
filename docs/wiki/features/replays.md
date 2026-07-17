# Replays

**Scope:** Local client (host records) · **Config:** [`MimesisPlayerEnhancement_Replays`](../CONFIG.md#replays--mimesisplayerenhancement_replays)

The game ships a full ReluReplay recorder but deletes files after each run unless they are uploaded to Relu. Playback code exists in the client but is stubbed out. This feature:

- Copies finished **host** dungeon recordings into a local library under `persistentDataPath/Replay/ModLibrary/`
- Adds a **Replays** button on the main menu (below Join Tram) to play or delete saved files
- Reimplements the stripped playback path: spectator view, pause, seek, voice, and an in-game HUD

## Enabling

Set `EnableReplays = true` in `[MimesisPlayerEnhancement_Replays]`. Recording still requires hosting a dungeon run (vanilla behavior).

## Privacy interaction

When `EnableReplays` is on, `BlockReplayRecording` from the Privacy feature is **not** applied — local capture is allowed so you can build a library. `BlockReplayUpload` still blocks outbound uploads to Relu when Privacy is enabled.

## Playback limits (current)

- **InGame** dungeon replays only (DeathMatch files are rejected)
- Seek backward silently rebuilds the dungeon from the recorded seed and fast-forwards to the target time (no loading screen; the mod HUD shows "Rewinding…" briefly)
- Seek forward fast-forwards in place without reloading
- Level-object IDs are remapped using the header's stable-ID table after regenerating the dungeon from the recorded seed

## HUD controls

| Control | Action |
|---------|--------|
| Slider | Seek (release to apply) |
| Pause / Resume | Freeze or resume playback clock |
| Speed | Cycle 0.5× / 1× / 1.5× / 2× |
| Prev / Next | Cycle spectator camera target |
| Exit | Return to main menu |
| Escape | Exit playback |
| F10 | Cycle UI visibility: mod HUD + player UI → player UI only → all hidden |

### F10 visibility modes

| Press | Mod HUD | Player UI (stats, inventory, spectator bar) |
|-------|---------|-----------------------------------------------|
| Default | shown | shown |
| 1st F10 | hidden | shown |
| 2nd F10 | hidden | hidden |
