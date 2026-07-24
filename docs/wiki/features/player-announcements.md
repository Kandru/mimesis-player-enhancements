# Player Announcements

Only the host must enable this for the whole lobby to get the effect. Joiners do not need the mod. Shows bottom-left toasts for dungeon run settings when a shift starts, boss and special spawn alerts during the run, and a personal map-run stats recap when you die. These are extra hints on top of the game's own messages — they do not replace vanilla connect or system text.

## Configuration

### `ShowPlayerAnnouncements`

Master toggle for all Player Announcements toasts. Per-map death stats also require [Statistics](./statistics.md) enabled. Toast display time is controlled locally by `ModToastDurationSeconds` in [User Interface](./user-interface.md); it does not live in this section. The game reloads config while running; unset uses the default below. No restart needed for this setting.

| Value | Meaning |
|---|---|
| `true` | Show announcement toasts |
| `false` | Hide all Player Announcements toasts |

Default: `true`
