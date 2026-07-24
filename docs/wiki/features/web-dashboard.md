# Web Dashboard

Serves a local browser page for connected players, a live minimap, leaderboards, moderation, host cheats, and mod settings. **Host only** — only the host needs this mod for the lobby to get the dashboard; joiners do not. It always tries to start (no on/off toggle). Default URL: `http://127.0.0.1:8001/` — or use the yellow Management button on the main/ESC menu (opens the real bound URL).

## Configuration

Keys live in `[MimesisPlayerEnhancement_WebDashboard]` inside `MimesisPlayerEnhancement.cfg` (global only). They are not shown in the dashboard settings UI and cannot be changed through the dashboard API. Changes apply when the config reloads (HTTP server restarts). Unset keys use the defaults below.

### `WebDashboardListenAddress`

IP or hostname the HTTP server binds to. Keep loopback for local-only access; non-loopback binds log a network-exposure warning and anyone on that network can reach the dashboard with no login.

| Value | Meaning |
|---|---|
| `127.0.0.1` | Local machine only (recommended) |
| `0.0.0.0` or a LAN IP | Reachable on the network — no authentication |

Default: `127.0.0.1`

### `WebDashboardListenPort`

Preferred TCP port. Valid range `1`–`65535`; out of range resets to `8001`. If the preferred port is busy, the mod tries the next 20 ports and keeps listening there, but does not rewrite this saved preferred port. If none are free, the dashboard stays off and a red error is logged. The Management button always opens the port actually in use.

| Value | Meaning |
|---|---|
| `1`–`65535` | Preferred starting port |
| Busy preferred port | Try preferred … preferred+20; bind first free |
| Invalid / out of range | Reset to `8001` |

Default: `8001`
