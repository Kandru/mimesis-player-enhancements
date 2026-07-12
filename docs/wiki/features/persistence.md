# Persistence

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_Persistence`](../CONFIG.md#persistence--mimesisplayerenhancement_persistence)

Without persistence, mimic voice recordings are lost when you quit or load a different save. This feature writes those recordings to disk when you save the game and restores them when you load that save — so mimics remember voices across play sessions.

## Save and restore

`EnablePersistence` controls whether mimic voice data is written on vanilla save (including auto-save) and loaded when you open that save slot. Works together with [More Voices](./more-voices.md) — persistence stores what More Voices allows the game to record in the first place.

**Full config keys →** [Persistence](../CONFIG.md#persistence--mimesisplayerenhancement_persistence)
