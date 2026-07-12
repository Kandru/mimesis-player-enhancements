# More Players

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_MorePlayers`](../CONFIG.md#more-players--mimesisplayerenhancement_moreplayers)

The base game limits sessions to four players. This feature raises that cap so larger groups can play together — for example, up to 32 people in one lobby. Only the host needs the mod; everyone else joins as usual.

## Player cap

`EnableMorePlayers` turns the higher cap on or off. When off, the game stays at the vanilla four-player limit.

`MaxPlayers` sets the maximum players in a session, host included (`1` = solo, `2` = host + one friend, and so on). Joining clients do not need the mod — the host's session enforces the cap.

## Scaling round goals

When More Players is enabled, `EnableScalingRoundGoals` scales tram repair quotas beyond vanilla stage 5 instead of capping there.

- `RoundGoalBasePerZone` — base dollars multiplied by the zone curve (zone 1 at defaults ≈ $200 before spread and multiplier).
- `RoundGoalMoneyMultiplier` — global multiplier on the computed quota.
- `RoundGoalRandomSpreadPercent` — random ±% band around the computed center when departing maintenance.
- `RoundGoalCurveExponent` — zone growth curve (`1` = linear; below `1` = flatter late-game growth).

**Full config keys →** [More Players](../CONFIG.md#more-players--mimesisplayerenhancement_moreplayers)
