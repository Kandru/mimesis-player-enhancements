# Persistence

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Persistence`](../CONFIG.md#persistence--mimesisplayerenhancement_persistence)

Without persistence, mimic voice recordings are lost when you quit or load a different save. This feature writes those recordings to disk when you save the game and restores them when you load that save — so mimics remember voices across play sessions.

## Save and restore

`EnablePersistence` controls whether mimic voice data is written on vanilla save (including auto-save) and loaded when you open that save slot. Works together with [More Voices](./more-voices.md) — persistence stores what More Voices allows the game to record in the first place.

Each save slot uses two mod files for voice data:

| File | Contents |
|------|----------|
| `MMGameData{N}.mpe-speech.sav` | Binary voice recordings (MPEV format) |
| `MMGameData{N}.mpe-slot.sav` | Player roster including `voiceId` (Dissonance player UUID per Steam ID) |

Speech events and voice UUID mappings are written together on vanilla save (including auto-save): recordings go to the speech file; Steam ID → `voiceId` mappings are stored in the slot document’s `players` section and flushed with other per-save sidecar data.

Voice event counts shown in the save slot picker are read from the binary speech file header — no separate metadata sidecar.

**Full config keys →** [Persistence](../CONFIG.md#persistence--mimesisplayerenhancement_persistence)
