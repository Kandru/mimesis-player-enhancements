# Web Dashboard

**Scope:** Host process · **Config:** [`MimesisPlayerEnhancement_WebDashboard`](../CONFIG.md#web-dashboard--mimesisplayerenhancement_webdashboard)

While the game is running, open a page in your web browser to see who is connected, watch player positions on a live minimap during dungeon runs, browse leaderboards, and use moderation tools. You can also change mod settings from the browser instead of editing the config file by hand.

Default URL: `http://127.0.0.1:8001/`

## Views

| View | Who can see it | What it shows |
|------|----------------|---------------|
| **Global Settings** | Host or idle (no session) | Global defaults; written to disk immediately |
| **Settings** | Host in an active save | Per-save overrides; applied live; written on vanilla save |
| **Players** | Anyone who can reach the URL | Connected players with avatars, host/local badges, network grade, ban status |
| **Minimap** | Anyone who can reach the URL | Live player positions during dungeon runs |
| **Leaderboard** | Host only | Per-save stats leaderboard (requires **Statistics**) |
| **Player stats** | Host only | Per-player statistics for the active save (requires **Statistics**) |
| **Moderation** | Host only | Kick, ban, unban, respawn, and heal actions |

## Blind mode

Header toggle (host only, on by default). Hides alive/dead status, session stats, vitals, and respawn actions to avoid spoilers during dungeon and deathmatch runs while you are alive. Automatically inactive in maintenance and tram. While dead, blind mode lifts temporarily so you can review others' stats.

## Moderation

Host-only actions on the Players page: kick, ban, unban, respawn, heal, and item spawn.

## Player cheats

Per-player buttons (host only): **Godmode** prevents death; **Noclip** lets that player fly with normal movement controls. Both turn off when blind mode is active or the player dies. Full noclip flight requires the target player to have this mod on their client.

## Management button

While the dashboard server runs, a yellow **Management** button appears on the main menu and ESC menu (between Settings and Quit). Opens the dashboard in the Steam overlay browser, falling back to the system browser.

## Security

Default bind is `127.0.0.1` (loopback) — only your machine can connect. Binding to `0.0.0.0` or a LAN IP exposes the dashboard to anyone on that network with no login. Listen address and port are cfg-file only (not editable from inside the dashboard).

**Full config keys →** [Web Dashboard](../CONFIG.md#web-dashboard--mimesisplayerenhancement_webdashboard)
