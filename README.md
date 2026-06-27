# Mimesis Player Enhancement

A unified [MelonLoader](https://melonwiki.xyz/) mod for [MIMESIS](https://store.steampowered.com/app/2827200/MIMESIS/) that combines three community enhancements into one configurable package:

| Feature | What it does |
|---------|--------------|------------|
| [**More Players**](https://github.com/NeoMimicry/MorePlayers/tree/main) | Raises the 4-player multiplayer cap (default: 999) |
| (**More Voices**)[https://github.com/DanEvenSegler/mimesis_more_voices_mod] | Raises per-player voice recording limits (default: 3000) |
| (**Persistence**)[https://github.com/JoanRLopez/MimesisPersistence] | Saves mimic voice recordings across save/load |
| **Statistics** | Per-player session and global stats per save slot with leaderboard |

Tested against **MIMESIS 0.3.0** with **MelonLoader 0.7.3** and **MimicAPI 0.3.0**.

Please support the original authors. This is basically just an AI-Improvement of those plug-ins without my own "intelligence". I simply wanted a single configurable plug-in which works with the latest Mimesis-Release.

## Requirements

- MIMESIS (Steam) with MelonLoader 0.7.3+
- [MimicAPI 0.3.0](https://thunderstore.io/c/mimesis/p/NeoMimicry/MimicAPI/) installed in `Mods/` (runtime dependency)
- Windows or Linux game install

## Installation (players)

1. Install MelonLoader and MimicAPI if you have not already.
2. Download `MimesisPlayerEnhancement.dll` from [Releases](https://github.com/kalle/mimesis-player-enhancement/releases).
3. Copy the DLL into your game folder: `<Mimesis-Steam-Folder>/Mods/MimesisPlayerEnhancement.dll`
4. Launch the game once — a config file is created automatically (see below).
5. **Remove** the old separate mods (`MorePlayers`, `More Voices`, `MimesisPersistence`) to avoid duplicate patches.

## Configuration

Settings use MelonLoader's [MelonPreferences](https://melonwiki.xyz/#/modders/preferences) system and are stored in:

```
<Mimesis-Steam-Folder>/UserData/MimesisPlayerEnhancement.cfg
```

Edit the file while the game is **closed**, or use any MelonPreferences editor mod.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableMorePlayers` | bool | `true` | Raise the multiplayer player cap |
| `MaxPlayers` | int | `999` | Player limit when More Players is enabled |
| `EnableMoreVoices` | bool | `true` | Raise voice recording limits |
| `MaxVoiceEvents` | int | `3000` | Max stored voice events per player |
| `EnablePersistence` | bool | `true` | Persist mimic voices across saves |
| `EnableStatistics` | bool | `true` | Track per-player session and global statistics |
| `SessionReconnectGraceMinutes` | int | `5` | Reuse prior session if player reconnects within this window |
| `ShowStatisticsToasts` | bool | `true` | Show join/leave/cycle messages in bottom-left player info UI |
| `EnableDebugLogging` | bool | `false` | Verbose diagnostic lines in the MelonLoader console |

Example:

```toml
[MimesisPlayerEnhancement]
EnableMorePlayers = true
MaxPlayers = 32
EnableMoreVoices = true
MaxVoiceEvents = 3000
EnablePersistence = true
EnableStatistics = true
SessionReconnectGraceMinutes = 5
ShowStatisticsToasts = true
EnableDebugLogging = false
```

### Logging

With default settings, the mod prints **operational feedback** when something meaningful happens:

- Feature enabled at startup (player cap, voice limits, persistence)
- Player join/leave and voice event counts (`voiceEvents=N`)
- Voice restore success/failure on connect
- Save/load of persisted voices
- Multiplayer join allowed/denied (More Players)

Set `EnableDebugLogging = true` for extra detail (patch internals, SteamID resolution, pool matching, room `_maxPlayers` updates, etc.). Debug lines are prefixed with `[Feature:debug]`.

Example normal output:

```
[MorePlayers] Join allowed — uid=12345 in VWaitingRoom (3/999 players).
[MoreVoices] Voice archive started — maxCap=3000, limitsPatched=3, player=abc uid=12345 role=client voiceEvents=12
[Persistence] Player connected — voice restore OK. player=abc uid=12345 role=client voiceEvents=47 | injected=35 (pool=35, reconnect=0), before=12 after=47
```

### How the features work

**More Players** patches server-side room and network validation so more than four clients can join.

**More Voices** increases `SpeechEventArchive` capacity fields when each archive starts, so players can record far more mimic lines per session.

**Persistence** writes voice data beside your save slots under `Save/{SteamID}/MimesisData/Slot{N}/`. On load, recordings are matched back to players via SteamID even if voice IDs change between sessions.

**Statistics** (host-only) tracks native play-report metrics plus voice events, deaths, revives, kills, currency, and connected time. Data is stored per save slot under:

```
Save/{SteamID}/MimesisPlayerEnhancement/Slot{N}/statistics/
  players/{steamId}.json
  leaderboard.json
```

Each map cycle flushes stats to disk. Sessions span a player's connection window and are reused when they reconnect within `SessionReconnectGraceMinutes`. Join/leave and cycle-save messages use the same bottom-left UI as native player enter/leave notifications (`UIPrefab_PlayerEnterInfo`).

## Development setup

### Prerequisites

- [.NET SDK 8+](https://dotnet.microsoft.com/download)
- **No game install required to compile** — reference DLLs are bootstrapped into `deps/reference/`

### First-time bootstrap

```bash
chmod +x scripts/*.sh
./scripts/bootstrap-deps.sh
```

This downloads **MimicAPI** from Thunderstore and **reference assemblies** (compile-only stubs) used instead of a full game path.

Reference libs are resolved in order:

1. Already present in `deps/reference/`
2. `REFERENCE_LIBS_URL` (direct zip URL)
3. GitHub release asset `reference-libs-1.0.0` / `mimesis-reference-libs-1.0.0.zip` on this repo
4. Local game install (`MIMESIS_PATH` or `PathConfig.props`) — runs `pack-reference-libs.sh` automatically

#### One-time CI setup (maintainer)

From a machine with MIMESIS installed:

```bash
export MIMESIS_PATH="/path/to/MIMESIS"
./scripts/pack-reference-libs.sh
```

Upload `deps/mimesis-reference-libs-1.0.0.zip` to a GitHub release tagged **`reference-libs-1.0.0`**. After that, GitHub Actions builds without any game path or runner secrets.

Alternatively set a repository variable / `REFERENCE_LIBS_URL` in the workflow to any direct download URL for that zip.

### Build

Output goes to **`dist/debug`** (Debug) or **`dist/prod`** (Release):

```bash
./scripts/build.sh          # → dist/debug/MimesisPlayerEnhancement.dll
./scripts/build.sh Release  # → dist/prod/MimesisPlayerEnhancement.dll
```

#### Optional: copy into game Mods/ (local testing)

```bash
COPY_TO_MODS=true MIMESIS_PATH="$HOME/.steam/steamapps/common/MIMESIS" ./scripts/build.sh
```

Or use `PathConfig.props` with `GamePath` and `COPY_TO_MODS=true`.

### Project layout

```
dist/
  debug/                              # Debug build output
  prod/                               # Release build output
deps/
  reference/Managed/                  # Compile-only game refs (bootstrapped)
  reference/MelonLoader/net35/
  MimicAPI/
src/
  Version.cs                          # Release version (CI watches this file)
  MimesisPlayerEnhancement/
    Mod.cs                            # MelonMod entry point
    Config/ModConfig.cs               # MelonPreferences
    Features/
      MorePlayers/                    # Player cap patches (0.3.0-compatible)
      MoreVoices/                     # Voice limit patches
      Persistence/                    # Save/load voice data
      Statistics/                     # Player stats and leaderboard
scripts/
  bootstrap-deps.sh                   # Download pinned MimicAPI
  build.sh                            # Local dev build
  check-nuget-updates.sh              # Report newer dependency versions
```

### Pinned dependencies

Versions are fixed in `Directory.Packages.props` and must be bumped manually:

```xml
<PackageVersion Include="HarmonyX" Version="2.10.2" />
<PackageVersion Include="MimicAPI" Version="0.3.0" />
```

| Package | Source | Role |
|---------|--------|------|
| **HarmonyX** 2.10.2 | [NuGet](https://www.nuget.org/packages/HarmonyX) | Compile-time only — matches MelonLoader's bundled `0Harmony.dll` |
| **MimicAPI** 0.3.0 | Thunderstore (via `bootstrap-deps.sh`) | Build + runtime dependency in `Mods/` |

Patching uses [Harmony](https://github.com/pardeike/Harmony) ([docs](https://harmony.pardeike.net/)). MelonLoader ships the **HarmonyX** fork at runtime; we reference the same version via NuGet for builds so you do not need `0Harmony.dll` on the HintPath. **Never** ship a separate Harmony DLL with your mod.

Check for updates:

```bash
./scripts/check-nuget-updates.sh
```

Game assemblies are **compile-only references** in `deps/reference/` (not NuGet). Harmony uses **HarmonyX** from NuGet; MelonLoader/game DLLs come from the reference pack.

## CI / releases

| Workflow | Trigger | Output |
|----------|---------|--------|
| `build.yml` | Push / PR to `main` | Artifacts from `dist/debug` and `dist/prod` |
| `release.yml` | Change to `src/Version.cs` on `main` | GitHub Release zip |

No `MIMESIS_PATH` or game install on the runner. CI only needs the **reference-libs** release asset (see above).

To cut a mod release:

1. Edit `src/Version.cs`
2. Commit and push to `main`

## Credits

Built from and replaces:

- [MorePlayers](https://github.com/NeoMimicry/MorePlayers) by NeoMimicry / Rxflex
- [More Voices](https://thunderstore.io/c/mimesis/p/Risikus/More_Voices/) by Risikus
- [MimesisPersistence](https://github.com/JoanRLopez/MimesisPersistence) by JoanR

Uses [MimicAPI](https://github.com/NeoMimicry/MimicAPI) and [MelonLoader](https://github.com/LavaGang/MelonLoader).

## License

Persistence code derives from MimesisPersistence (see `old/` reference). MorePlayers logic derives from NeoMimicry/MorePlayers. Respect original mod licenses when redistributing.
