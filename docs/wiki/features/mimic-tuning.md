# Mimic Tuning

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_MimicTuning`](../CONFIG.md#mimic-tuning--mimesisplayerenhancement_mimictuning)

Tune mimic voice, trust/follow behavior, social mimicry, inventory copy, possession, emotes/props, and horn imitation on the host. Off by default. Each subgroup uses **Vanilla / Custom** (or mode-specific dropdowns) and applies on the next relevant game action — no restart required.

With `EnableMimicTuning = true`, archived mimic voice is stopped when a mimic enters possessed state and blocked while possessed.

## Voice (`mimicVoice`)

`MimicVoiceTuningMode`: `Vanilla` | `Custom`

**Player replies** (when you speak):

- `PlayerVoiceResponseChancePercent`, cooldown, delay min/max, `PlayerVoiceResponseMaxDistance` (vanilla 20 m)

**Ambient speech** (Custom):

- `PeriodicVoiceIntervalMultiplier` — scales **periodic** table waits only (not post-reply delays)
- `SpeakAudienceRangeMeters` (vanilla 15 m) — a player must be within this range before a mimic picks an ambient line
- `ClipReuseCooldownSeconds` / `DeathMatchClipReuseCooldownSeconds` — how long before the same clip can play again
- `MinRequiredSpeechClips` — warmed clips required before mimics speak
- `HearOwnVoiceFromMimic`: `Vanilla` | `AlwaysHear` | `OnlyWhenSingleplayer` — `OnlyWhenSingleplayer` uses lobby player count, not proximity
- Table intervals (each `Vanilla` | `Random` with min/max): init (4–7 s), periodic (2–8 s), deathmatch (2–8 s)
- `PostReplyIntervalMode`: `Vanilla` | `Fixed` | `Random` — wait after a mimic answers you (vanilla 2–4 s; separate from ambient multiplier)

## Trust & chase (`mimicTrust`)

`MimicTrustMode`: `Vanilla` | `Custom`

- Trust **deltas** (indoor base) and `TrustOutdoorMultiplier` (vanilla ×2 outdoors)
- Friendly/distrust thresholds (90 / 10)
- `TrustScoreValueMode`: `Vanilla` | `Fixed` | `Random` — raw 0–100 points for initial trust (vanilla 50) and behavior-trust threshold (vanilla 70), not multipliers
- `ChaseActivationDistanceMeters` (8 m) / `ChaseForceRunDistanceMeters` (10 m) — follow leash during checkfriendly

## Social mimicry (`mimicSocial`)

`MimicSocialMode`: `Vanilla` | `Custom`

- `MimicRunawayChance` (0–1, default 0.5) — how often **other** creatures flee when they see a mimic (0 = never, 1 = always)
- `JumpCopyChancePercent` / `SlotFollowChangeChancePercent` — mimic copies player jumps / inventory slot changes

## Inventory copy

Unchanged: `MimicInventoryCopyMode` + `MimicInventoryCopyPickRule`.

## Possession (`mimicPossession`)

`EnableMimicPossessionTuning` (requires master toggle):

- Duration randomization + cooldown multiplier (unchanged)
- `PossessionRangeMeters` (default 10 m) — max E-possession distance
- `PossessionBtGateMode`: `Vanilla` | `Always` — `Always` allows possession on any alive, non-silenced mimic (skips BT whitelist)

## Emote & props (`mimicEmoteProps`)

`MimicEmotePropsMode`: `Vanilla` | `Custom` — per-action chance % (respond, suggest, sprinkler, trap switch, charger, transmitter, shutter).

## Horn imitation (`mimicHorn`)

`HornImitationMode`: `Vanilla` | `Custom`

- `AllowHornImitation` (Custom) — whether mimics may replay recorded tram horns
- `HornMaxRecordSeconds`, `HornRecordingGapSeconds`, `HornMaxStoredRecords`

**Full config keys →** [Mimic Tuning](../CONFIG.md#mimic-tuning--mimesisplayerenhancement_mimictuning)
