# Join Anytime

Normally, friends have to be in the lobby before a run starts. Join Anytime lets people connect after you've already begun — but only while the party is at the service station (maintenance room) or on the tram between dungeons. Players cannot connect during a dungeon. Only the host must enable this for the whole lobby to get the effect; joiners do not need the mod or matching config. When the lobby is public, the browse list shows whether you can join now or how long until the party is back at the tram (for example `[join now]` or `[join in ~12 min]`). Hosts can also toggle public matchmaking and edit the lobby title from the ESC menu in the tram or during a dungeon run; those settings are saved per save game.

## Configuration

### `EnableJoinAnytime`

Turns late join on or off. When on, new players can connect only while the host is in the maintenance room or on the tram between dungeons. Connection is blocked during an active dungeon run. Applies immediately when you save the config file; no game restart needed. If the key is missing from your config, the default below is used.

| Value | Meaning |
|---|---|
| `true` | Late join enabled for the lobby |
| `false` | Vanilla behavior — no late join |

Default: `true`

### `JoinConnectionGraceSeconds`

After a player connects, tram departure is blocked for this many seconds while they finish loading. Players who do not become ready in time are kicked; the host is never kicked. Applies immediately to new connections; changing the value does not reset timers already running for players mid-load. Minimum 1 second. If the key is missing, the default below is used.

| Value | Meaning |
|---|---|
| Integer ≥ `1` | Grace period length in seconds |

Default: `30`
