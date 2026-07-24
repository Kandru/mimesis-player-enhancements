# Mimic Tuning

Only the host needs this mod enabled for the whole lobby to get the effect. Joining clients do not need it.

Tune mimic voice, trust/chase, social mimicry, inventory copy, possession, emotes/props, and horn imitation. Off by default (`EnableMimicTuning = false`). Each subgroup uses **Vanilla** or **Custom**; Custom keys apply only when that subgroup mode is Custom (or the listed dependency). Changes apply live on the next relevant game action — no restart. Trust, social, emote, and horn settings sync to active mimics when you save config; voice replies and possession sessions already in progress are not reverted.

Config section: `[MimesisPlayerEnhancement_MimicTuning]`

## Configuration

### Master toggle

#### `EnableMimicTuning`

Master toggle for all Mimic Tuning subgroups. Only the host needs this mod enabled for the whole lobby to get the effect.

| Value | Meaning |
|---|---|
| `false` | Vanilla mimic behavior (default). |
| `true` | Custom subgroup settings apply when their mode is Custom. Stops archived mimic voice on E-possession start and blocks it while possessed. |

Default: `false`

### Voice

#### `MimicVoiceTuningMode`

Voice subgroup mode. Custom voice keys below apply only when this is `Custom` and the master toggle is on.

| Value | Meaning |
|---|---|
| `Vanilla` | Game voice tables and distances (default). |
| `Custom` | Use the voice keys in this section. |

Default: `Vanilla`

#### `PeriodicVoiceIntervalMultiplier`

Scales **periodic** ambient voice wait times only. Does not affect post-reply waits after a mimic answers you. Requires `MimicVoiceTuningMode = Custom`. Range `0.05`–`10`; `1` = vanilla speed.

Default: `1`

#### `PlayerVoiceResponseChancePercent`

Chance (0–100) a nearby mimic answers when you speak. Requires `MimicVoiceTuningMode = Custom`.

Default: `100`

#### `PlayerVoiceResponseCooldownSeconds`

Minimum seconds between mimic answers to player speech. Requires `MimicVoiceTuningMode = Custom`. Range ≥ `0` (clamped to `120` in code).

Default: `3` (seconds)

#### `PlayerVoiceResponseDelayMinSeconds`

Lower bound of reply delay after you speak. Requires `MimicVoiceTuningMode = Custom`. If max is below min, values are swapped.

Default: `0.2` (seconds)

#### `PlayerVoiceResponseDelayMaxSeconds`

Upper bound of reply delay after you speak. Requires `MimicVoiceTuningMode = Custom`.

Default: `0.2` (seconds)

#### `PlayerVoiceResponseMaxDistance`

Max distance (meters) from you when you speak for a mimic to consider answering. Separate from ambient `SpeakAudienceRangeMeters`. Requires `MimicVoiceTuningMode = Custom`. Range `1`–`200`.

Default: `20` (meters; vanilla)

#### `ClipReuseCooldownSeconds`

Seconds before the same speech clip can play again in normal play. Requires `MimicVoiceTuningMode = Custom`. Range `0`–`600`.

Default: `60` (seconds; vanilla)

#### `DeathMatchClipReuseCooldownSeconds`

Same as clip reuse, for deathmatch voice context. Requires `MimicVoiceTuningMode = Custom`. Range `0`–`600`.

Default: `3` (seconds; vanilla)

#### `SpeakAudienceRangeMeters`

For **ambient** mimic speech: a player must be within this range before a mimic picks a line. Does not gate replies when you speak (see `PlayerVoiceResponseMaxDistance`). Requires `MimicVoiceTuningMode = Custom`. Range `1`–`200`.

Default: `15` (meters; vanilla)

#### `PostReplyIntervalMode`

Wait time after a mimic answers you, before its next ambient attempt. Requires `MimicVoiceTuningMode = Custom`. Not scaled by `PeriodicVoiceIntervalMultiplier`.

| Value | Meaning |
|---|---|
| `Vanilla` | Game table (roughly 2–4 s; default). |
| `Fixed` | Use `PostReplyIntervalFixedSeconds`. |
| `Random` | Roll between min/max seconds. |

Default: `Vanilla`

#### `PostReplyIntervalFixedSeconds`

Fixed post-reply wait. Requires `PostReplyIntervalMode = Fixed` and `MimicVoiceTuningMode = Custom`.

Default: `3` (seconds)

#### `PostReplyIntervalMinSeconds`

Random post-reply wait lower bound. Requires `PostReplyIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `2` (seconds)

#### `PostReplyIntervalMaxSeconds`

Random post-reply wait upper bound. Requires `PostReplyIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `4` (seconds)

#### `MinRequiredSpeechClips`

Warmed speech clips a mimic needs before it speaks. Requires `MimicVoiceTuningMode = Custom`. Range `0`–`50`.

Default: `3` (vanilla)

#### `HearOwnVoiceFromMimic`

Whether you hear your own voice played back from mimics. Requires `MimicVoiceTuningMode = Custom`.

| Value | Meaning |
|---|---|
| `Vanilla` | Game table behavior (default). |
| `AlwaysHear` | Never mute your voice from mimics. |
| `OnlyWhenSingleplayer` | Mute unless lobby player count is 1 (uses lobby count, not proximity). |

Default: `Vanilla`

#### `VoiceInitIntervalMode`

First ambient voice wait after a mimic gains voice context. Requires `MimicVoiceTuningMode = Custom`.

| Value | Meaning |
|---|---|
| `Vanilla` | Game table 4–7 s (default). |
| `Random` | Roll between min/max seconds. |

Default: `Vanilla`

#### `VoiceInitIntervalMin`

Random init wait lower bound. Requires `VoiceInitIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `4` (seconds)

#### `VoiceInitIntervalMax`

Random init wait upper bound. Requires `VoiceInitIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `7` (seconds)

#### `VoicePeriodicIntervalMode`

Wait between periodic ambient voice lines. Requires `MimicVoiceTuningMode = Custom`. Rolled values are then scaled by `PeriodicVoiceIntervalMultiplier` when periodic.

| Value | Meaning |
|---|---|
| `Vanilla` | Game table 2–8 s (default). |
| `Random` | Roll between min/max seconds. |

Default: `Vanilla`

#### `VoicePeriodicIntervalMin`

Random periodic wait lower bound. Requires `VoicePeriodicIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `2` (seconds)

#### `VoicePeriodicIntervalMax`

Random periodic wait upper bound. Requires `VoicePeriodicIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `8` (seconds)

#### `VoiceDeathMatchIntervalMode`

Ambient voice wait in deathmatch context. Requires `MimicVoiceTuningMode = Custom`.

| Value | Meaning |
|---|---|
| `Vanilla` | Game table 2–8 s (default). |
| `Random` | Roll between min/max seconds. |

Default: `Vanilla`

#### `VoiceDeathMatchIntervalMin`

Random deathmatch ambient wait lower bound. Requires `VoiceDeathMatchIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `2` (seconds)

#### `VoiceDeathMatchIntervalMax`

Random deathmatch ambient wait upper bound. Requires `VoiceDeathMatchIntervalMode = Random` and `MimicVoiceTuningMode = Custom`.

Default: `8` (seconds)

### Trust & chase

#### `MimicTrustMode`

Trust, chase, and friendly-check tuning. Custom trust keys apply only when this is `Custom` and the master toggle is on.

| Value | Meaning |
|---|---|
| `Vanilla` | Game AI trust values (default). |
| `Custom` | Use trust keys below. |

Default: `Vanilla`

#### `TrustOutdoorMultiplier`

Multiplier on indoor trust deltas when the mimic is outdoors. Requires `MimicTrustMode = Custom`. Range `0`–`10`.

Default: `2` (vanilla)

#### `TrustLookingDelta`

Trust change per tick when the player is looking at the mimic (indoor base). Requires `MimicTrustMode = Custom`. Range `-100`–`100`.

Default: `-3` (vanilla)

#### `TrustNotLookingDelta`

Trust change when the player is not looking. Requires `MimicTrustMode = Custom`.

Default: `3` (vanilla)

#### `TrustApproachDelta`

Trust change when the player approaches. Requires `MimicTrustMode = Custom`.

Default: `0.5` (vanilla)

#### `TrustMaintainDelta`

Trust change when distance is maintained. Requires `MimicTrustMode = Custom`.

Default: `0` (vanilla)

#### `TrustWalkAwayDelta`

Trust change when the player walks away. Requires `MimicTrustMode = Custom`.

Default: `5` (vanilla)

#### `TrustSprintAwayDelta`

Trust change when the player sprints away. Requires `MimicTrustMode = Custom`.

Default: `-11` (vanilla)

#### `TrustHitDamageMultiplier`

Trust change multiplier when the mimic is hit. Requires `MimicTrustMode = Custom`.

Default: `-0.5` (vanilla)

#### `TrustFriendlyThreshold`

Trust score at or above which the mimic is treated as friendly. Requires `MimicTrustMode = Custom`. Range `0`–`100`.

Default: `90` (vanilla)

#### `TrustDistrustThreshold`

Trust score at or below which the mimic is distrusted. Requires `MimicTrustMode = Custom`. Range `0`–`100`.

Default: `10` (vanilla)

#### `TrustScoreValueMode`

How initial trust and behavior-trust threshold are chosen when a mimic activates. Raw 0–100 scores, not multipliers. Requires `MimicTrustMode = Custom`.

| Value | Meaning |
|---|---|
| `Vanilla` | Keep prefab/game values (default). |
| `Fixed` | Use fixed keys below. |
| `Random` | Roll between random min/max per mimic. |

Default: `Vanilla`

#### `TrustInitialFixed`

Fixed initial trust score. Requires `TrustScoreValueMode = Fixed` and `MimicTrustMode = Custom`. Range `0`–`100`.

Default: `50` (vanilla)

#### `TrustInitialRandomMin`

Random initial trust lower bound. Requires `TrustScoreValueMode = Random` and `MimicTrustMode = Custom`.

Default: `50` (vanilla)

#### `TrustInitialRandomMax`

Random initial trust upper bound. Requires `TrustScoreValueMode = Random` and `MimicTrustMode = Custom`. Swapped with min if max &lt; min.

Default: `50` (vanilla)

#### `TrustBehaviorFixed`

Fixed behavior-trust threshold. Requires `TrustScoreValueMode = Fixed` and `MimicTrustMode = Custom`. Range `0`–`100`.

Default: `70` (vanilla)

#### `TrustBehaviorRandomMin`

Random behavior-trust lower bound. Requires `TrustScoreValueMode = Random` and `MimicTrustMode = Custom`.

Default: `70` (vanilla)

#### `TrustBehaviorRandomMax`

Random behavior-trust upper bound. Requires `TrustScoreValueMode = Random` and `MimicTrustMode = Custom`.

Default: `70` (vanilla)

#### `ChaseActivationDistanceMeters`

Distance at which chase/follow activates during friendly checks. Requires `MimicTrustMode = Custom`. Range `0.1`–`200` m.

Default: `8` (meters; vanilla)

#### `ChaseForceRunDistanceMeters`

Distance at which the mimic forces run during trust evaluation. Requires `MimicTrustMode = Custom`. Range `0.1`–`200` m.

Default: `10` (meters; vanilla)

### Social mimicry

#### `MimicSocialMode`

Social mimicry (runaway reaction, jump copy, slot follow). Custom keys apply only when this is `Custom` and the master toggle is on.

| Value | Meaning |
|---|---|
| `Vanilla` | Game probabilities (default). |
| `Custom` | Use social keys below. |

Default: `Vanilla`

#### `MimicRunawayChance`

How often **other creatures** flee when they see a mimic — not mimics running away. Requires `MimicSocialMode = Custom`. Range `0`–`1` (`0` = never, `1` = always).

Default: `0.5` (vanilla)

#### `JumpCopyChancePercent`

Chance mimics copy player jumps. Requires `MimicSocialMode = Custom`. Range `0`–`100`.

Default: `80` (vanilla)

#### `SlotFollowChangeChancePercent`

Chance mimics follow inventory slot changes. Requires `MimicSocialMode = Custom`. Range `0`–`100`.

Default: `80` (vanilla)

### Mimic possession

#### `EnableMimicPossessionTuning`

Sub-toggle for E-possession tuning. Requires `EnableMimicTuning = true`.

| Value | Meaning |
|---|---|
| `false` | Vanilla possession timing and gates (default). |
| `true` | Apply possession keys below. |

Default: `false`

#### `RandomizeMimicPossessionDuration`

Roll speak-window duration per possession. Requires `EnableMimicPossessionTuning = true`.

| Value | Meaning |
|---|---|
| `false` | Fixed duration from min/max when both equal default (12 s). |
| `true` | Roll between min and max each possession. |

Default: `false`

#### `MimicPossessionMinTimeSeconds`

Minimum possessed speak duration. Requires `RandomizeMimicPossessionDuration = true` and possession tuning enabled. Range `0.1`–`120` s.

Default: `12` (seconds; vanilla)

#### `MimicPossessionMaxTimeSeconds`

Maximum possessed speak duration. Requires `RandomizeMimicPossessionDuration = true` and possession tuning enabled. Synced to min if max &lt; min.

Default: `12` (seconds; vanilla)

#### `MimicPossessionCooltimeMultiplier`

Multiplier on post-possession cooldown. Requires `EnableMimicPossessionTuning = true`. Range `0.1`–`10`; `1` = vanilla.

Default: `1`

#### `PossessionRangeMeters`

Max distance (meters) for E-possession. Requires `EnableMimicPossessionTuning = true`. Range `1`–`200`.

Default: `10` (meters; vanilla)

#### `PossessionBtGateMode`

Whether behavior-tree whitelist gates possession.

| Value | Meaning |
|---|---|
| `Vanilla` | Game BT whitelist (default). |
| `Always` | Any alive, non-silenced mimic can be possessed. |

Requires `EnableMimicPossessionTuning = true`.

Default: `Vanilla`

### Emotes & props

#### `MimicEmotePropsMode`

Emote responses and prop interactions (sprinkler, traps, chargers, etc.). Custom chance keys apply only when this is `Custom` and the master toggle is on.

| Value | Meaning |
|---|---|
| `Vanilla` | Game probabilities (default). |
| `Custom` | Use emote/prop chance keys below. |

Default: `Vanilla`

#### `EmoteRespondChancePercent`

Chance mimics respond to player emotes. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

#### `EmoteSuggestChancePercent`

Chance mimics suggest emotes. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `30` (vanilla)

#### `ReactToSprinklerChancePercent`

Chance mimics react to sprinklers. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

#### `UseTrapSwitchChancePercent`

Chance mimics use trap switches. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

#### `UseChargerChancePercent`

Chance mimics use chargers. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

#### `UseTransmitterChancePercent`

Chance mimics use transmitters. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

#### `UseShutterSwitchChancePercent`

Chance mimics use shutter switches. Requires `MimicEmotePropsMode = Custom`. Range `0`–`100`.

Default: `100` (vanilla)

### Horn imitation

#### `HornImitationMode`

Tram horn recording and mimic replay. Custom horn keys apply only when this is `Custom` and the master toggle is on. Horn recorder limits apply at `VoiceManager` Awake; changing Custom→Vanilla mid-session may not restore limits until recreate.

| Value | Meaning |
|---|---|
| `Vanilla` | Game horn recorder settings (default). |
| `Custom` | Use horn keys below. |

Default: `Vanilla`

#### `AllowHornImitation`

Whether mimics may replay recorded tram horns. Requires `HornImitationMode = Custom`.

| Value | Meaning |
|---|---|
| `true` | Mimics can play stored horn patterns (default). |
| `false` | Block mimic horn replay. |

Default: `true`

#### `HornMaxRecordSeconds`

Max length of one horn recording. Requires `HornImitationMode = Custom`. Range `0.1`–`60` s.

Default: `5` (seconds; vanilla)

#### `HornRecordingGapSeconds`

Idle time before a new horn recording starts. Requires `HornImitationMode = Custom`. Range `0.1`–`30` s.

Default: `1` (second; vanilla)

#### `HornMaxStoredRecords`

Number of horn patterns kept in memory. Requires `HornImitationMode = Custom`. Range `1`–`100`.

Default: `10` (vanilla)

### Inventory copy

#### `MimicInventoryCopyMode`

Which player a mimic copies inventory from when it mirrors loadout.

| Value | Meaning |
|---|---|
| `Vanilla` | Game pick rule (default). |
| `Custom` | Use `MimicInventoryCopyPickRule`. |

Default: `Vanilla`

#### `MimicInventoryCopyPickRule`

Target selection when copying inventory. Requires `MimicInventoryCopyMode = Custom`.

| Value | Meaning |
|---|---|
| `MinDistance` | Nearest player (default). |
| `MaxDistance` | Farthest player. |
| `Random` | Random eligible player. |

Default: `MinDistance`
