# Custom Assets

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Replace scene loading overlay art and the dungeon landing melody with your own embedded files. Both features are client-only — each player chooses their own themes and sounds.

## Loading screen themes

Custom loading screens replace the full-screen art behind the vanilla loading text (`STRING_LOADING`, `STRING_LOADING_WAIT`). The mod hides the game's built-in variant transforms and draws your PNG on a full-screen overlay instead.

### Context folders

Each scene transition type has its own folder under `Assets/CustomLoadingScreen/` in the mod source tree:

| Folder | When it appears |
|--------|-----------------|
| `DungeonStart/` | Entering a dungeon (generation + waiting for players) |
| `DungeonEnd/` | Returning from a dungeon to the tram scene |
| `TramScene/` | Entering the tram from the start room or maintenance |
| `Maintenance/` | Returning to maintenance between cycles |
| `FirstEnter/` | First connection to a session |
| `DeathMatch/` | Death match arena load |

Per-dungeon excel keys (for example `Dungeon_Forest`) are normalized to the `DungeonStart/` folder. The game uses the same internal key for both tram transitions; the mod distinguishes `DungeonEnd` from `TramScene` by whether the previous transition entered a dungeon.

### Theme folders

Each theme is a top-level folder. Context subfolders hold the images for each transition type:

```
src/MimesisPlayerEnhancement/Assets/CustomLoadingScreen/
  GTA/
    theme.json            # optional — theme-wide settings
    DungeonStart/
      loading.png         # optional — local generation phase
      wait.png            # optional — waiting-for-players phase
      background.png      # fallback for both phases
    DungeonEnd/
      background.png
    TramScene/
      background.png
    Maintenance/
      background.png
  neon/
    theme.json
    DungeonStart/
      loading.png
      wait.png
```

Use the **same theme folder name** across contexts when you want a consistent look through maintenance → tram → dungeon.

### Dungeon loading + wait coupling

Only dungeon entry (`DungeonStart`) has two text phases on the same overlay. Ship matching art in the same context folder:

| File | Phase |
|------|-------|
| `loading.png` | While the dungeon generates locally |
| `wait.png` | After generation, while waiting for other players (multiplayer only) |
| `background.png` | Fallback when `loading.png` is missing; also used by other contexts |

When the game switches to `STRING_LOADING_WAIT` and the lobby has **more than one player**, the overlay crossfades from the current loading art (`loading.png`, or `background.png` if that was the fallback) into `wait.png` when that file exists. Solo lobbies keep the loading image — `wait.png` is skipped. If a theme has no dedicated wait art, the loading image stays up for everyone.

Other contexts only need `background.png`.

### Frame sequences

Animated themes can use numbered frame files instead of a single PNG:

| Pattern | Example |
|---------|---------|
| `loading_01.png` … `loading_NN.png` | Dungeon generation phase (`DungeonStart`) |
| `wait_01.png` … `wait_NN.png` | Dungeon wait phase (`DungeonStart`) |
| `background_01.png` … `background_NN.png` | All other contexts or fallback |

Single-file names (`loading.png`, `wait.png`, `background.png`) still work and are treated as one-frame sequences.

### Optional `theme.json`

Each theme folder may include a `theme.json` to override animation and display settings. All fields are optional — missing files or fields fall back to defaults.

```json
{
  "frameRate": 8,
  "loop": "loop",
  "motion": { "mode": "panZoom", "zoom": 1.08, "cycleSeconds": 20 },
  "backgroundColor": "#000000",
  "phases": {
    "loading": { "frameRate": 12, "motion": { "mode": "none" } },
    "wait": { "images": ["wait_a.png", "wait_b.png"], "loop": "pingPong" }
  }
}
```

| Field | Description |
|-------|-------------|
| `frameRate` | Frames per second for sequences (1–30, default 4) |
| `loop` | `loop`, `pingPong`, or `once` |
| `motion.mode` | `none` or `panZoom` (Ken Burns on single-frame images) |
| `motion.zoom` | Zoom factor for pan/zoom (default 1.08) |
| `motion.cycleSeconds` | Pan/zoom cycle length (default 20) |
| `backgroundColor` | Hex color behind letterboxed images (default `#000000`) |
| `phases.loading` / `wait` / `background` | Per-phase overrides plus optional explicit `images` list |

Global `CustomLoadingScreenMotion` disables pan/zoom but frame sequences still play when authored.

### Image requirements

- **Format:** `.png` only
- **Recommended size:** 1920×1080 (16:9). The mod scales images to cover the full screen on standard displays (16:9, 16:10, 4:3, etc.); leave safe margins for vanilla loading text. On ultrawide monitors (21:9+), images fit to full height with `backgroundColor` filling the side bars.
- **Naming:** Lowercase filenames exactly as above (`loading.png`, `wait.png`, `background.png`).

Generate blank 1920×1080 templates locally with:

```bash
python3 scripts/generate-loading-screen-examples.py --output /path/to/output
```

### Build and embed

1. Add PNGs (and optional `theme.json`) under `src/MimesisPlayerEnhancement/Assets/CustomLoadingScreen/`.
2. Rebuild the mod (`make debug` or `make release`). Files are embedded into the DLL automatically.
3. Set `CustomLoadingScreenMode` in the web dashboard:
   - `Vanilla` — game art (default, feature off)
   - `Random` — pick a theme per transition from the matching context folder
   - `Specific` — always use `CustomLoadingScreenVariant` when that theme exists for the context
4. If a context folder has no themes (or the specific theme is missing there), vanilla art is used for that transition.

## Dungeon landing sounds

Replace the melody that plays right after the tram-stop sting (`Sound_UI_TramStopBGM_01`). Departure horns and end-of-run horns are unchanged.

### Asset folder

Place `.ogg` or `.wav` files in a **flat** folder (no subfolders):

```
src/MimesisPlayerEnhancement/Assets/RoundStartSound/
  my_melody_1.ogg
  my_melody_2.ogg
```

Use lowercase names with underscores (for example `cyberpunk_1.ogg`, `anime_2.ogg`). The filename (without extension) becomes the variant id in config.

### Exporting the vanilla reference

To hear or remix the original landing melody, export it from game assets:

```bash
./scripts/export-round-start-sound.sh
```

This writes `src/MimesisPlayerEnhancement/Assets/RoundStartSound/vanilla.wav` when UnityPy (or Docker) is available.

### Build and configure

1. Add audio files to `Assets/RoundStartSound/`.
2. Rebuild the mod.
3. Set `RoundStartSoundMode`:
   - `Vanilla` — original game melody
   - `Random` — pick a random embedded file each dungeon entry
   - `Specific` — play `RoundStartSoundVariant`
4. Replacement only applies during the short window after entering the dungeon (the tram-stop sting itself is not replaced).
5. Adjust `RoundStartSoundVolume` (default `0.8`) if custom melodies feel too loud relative to the tram-stop horn. Does not affect vanilla mode.

Check `Assets/RoundStartSound/LICENSE.md` before redistributing third-party audio with your mod build.

## Quick checklist

**Loading screens**

1. Create theme folders under `Assets/CustomLoadingScreen/<theme>/<Context>/`.
2. Add PNGs (and optional `theme.json` for animation settings).
3. Rebuild, set `CustomLoadingScreenMode` to `Random` or `Specific`.

**Sounds**

1. Add `.ogg`/`.wav` files to `Assets/RoundStartSound/`.
2. Rebuild, set `RoundStartSoundMode` to `Random` or `Specific`.

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
