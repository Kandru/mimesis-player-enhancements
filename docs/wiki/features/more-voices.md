# More Voices

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_MoreVoices`](../CONFIG.md#more-voices--mimesisplayerenhancement_morevoices)

Mimics copy things players say and replay them later. The base game only keeps a small number of voice lines in memory. This feature stores many more recordings so mimics can build a richer library over a long session.

## Voice limits

Per-player caps control how many mimic voice lines are stored:

- `MaxIndoorVoiceEvents` — indoor dungeon runs.
- `MaxOutdoorVoiceEvents` — outdoor areas.
- `MaxDeathMatchVoiceEvents` — deathmatch (always separate).

When `UnifyIndoorOutdoorVoices` is on, indoor and outdoor share one combined cap (deathmatch stays separate). Cross-area playback is allowed.

## Hub recording

Vanilla only records voices inside dungeons. These toggles extend recording to hub scenes:

- `RecordVoiceInMaintenance` — maintenance room.
- `RecordVoiceInTram` — tram waiting scene.

## Possession recording

`RecordVoiceDuringMimicPossession` keeps recording while you possess a mimic and resumes after possession ends.

## Performance cache

Large voice pools can cause lag. `EnableVoicePerformanceCache` caches warmed voice lists, decoded audio clips, mimic host selection, and player lookups (only when `EnableMoreVoices` is on). `VoiceClipCacheMaxEntries` limits decoded clips kept in memory (LRU eviction).

With `EnableDebugLogging`, slow or large `PickBestMatch` runs log event count and elapsed time for playtest profiling.

**Full config keys →** [More Voices](../CONFIG.md#more-voices--mimesisplayerenhancement_morevoices)
