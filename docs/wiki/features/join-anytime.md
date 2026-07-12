# Join Anytime

**Scope:** Host only · **Config:** [`MimesisPlayerEnhancement_JoinAnytime`](../CONFIG.md#join-anytime--mimesisplayerenhancement_joinanytime)

Normally, friends have to be in the lobby before a run starts. Join Anytime lets people connect after you've already begun. **Joiners do not need this mod** — only the host does.

## Late join flow

Late joiners cannot be dropped straight into an active dungeon. They wait on the tram map until the party finishes the current dungeon; when everyone returns to the tram, the next lever pull starts the next run together.

Joiners can connect whenever you are not inside a dungeon (maintenance, tram, etc.).

## Connection grace period

`JoinConnectionGraceSeconds` blocks tram departure after a player connects while they finish loading. Players who do not become ready in time are kicked — the host is never kicked.

Hosts can toggle public matchmaking and edit the lobby title from the ESC menu in the tram or during a dungeon run. Lobby title and public/private preference are saved with the game (per save).

**Full config keys →** [Join Anytime](../CONFIG.md#join-anytime--mimesisplayerenhancement_joinanytime)
