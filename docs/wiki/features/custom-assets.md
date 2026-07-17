# Custom Assets

**Scope:** Your game only (local) · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Replace scene loading overlay art and the dungeon landing melody with your own embedded files. Both features are client-only — each player chooses their own themes and sounds.

After `make debug` or `make release`, example loading-screen PNG templates (1920×1080) are copied next to the built mod DLL in `dist/debug/` or `dist/prod/`. Use them as size and naming references when authoring your own assets.

## Loading screen themes

Custom loading screens replace the full-screen art behind the vanilla loading text (`STRING_LOADING`, `STRING_LOADING_WAIT`). The mod hides the game's built-in variant transforms and draws your PNG on a full-screen overlay instead.

### Context folders

Each scene transition type has its own folder under `Assets/CustomLoadingScreen/` in the mod source tree:

| Folder | When it appears |
|--------|-----------------|
| `Dungeon/` | Entering a dungeon (generation + waiting for players) |
| `InTramWaiting/` | Tram waiting room before a run |
| `Maintenance/` | Returning to maintenance between cycles |
| `FirstEnter/` | First connection to a session |
| `DeathMatch/` | Death match arena load |

Per-dungeon excel keys (for example `Dungeon_Forest`) are normalized to the `Dungeon/` folder.

### Theme subfolders

Inside each context folder, create one subfolder per theme (for example `cyberpunk`, `neon`, `slate`). Use the **same theme name** across contexts if you want a consistent look through maintenance → tram → dungeon.

```
src/MimesisPlayerEnhancement/Assets/CustomLoadingScreen/
  Dungeon/
    my_theme/
      loading.png       # optional — local generation phase
      wait.png          # optional — waiting-for-players phase
      background.png    # fallback for both phases
  InTramWaiting/
    my_theme/
      background.png
  Maintenance/
    my_theme/
      background.png
  FirstEnter/
    my_theme/
      background.png
  DeathMatch/
    my_theme/
      background.png
```

### Dungeon loading + wait coupling

Only dungeon loads have two text phases on the same overlay. Ship matching art in the same theme folder:

| File | Phase |
|------|-------|
| `loading.png` | While the dungeon generates locally |
| `wait.png` | After generation, while waiting for all players |
| `background.png` | Used for both phases when phase-specific files are missing |

Other contexts only need `background.png`.

### Image requirements

- **Format:** `.png` only
- **Recommended size:** 1920×1080 (16:9). The mod stretches the image to the overlay; leave safe margins for vanilla loading text.
- **Naming:** Lowercase filenames exactly as above (`loading.png`, `wait.png`, `background.png`).

Example templates ship in the repo at `assets/examples/custom-loading-screen/` and are copied to `dist/debug/` (or `dist/prod/`) on build:

- `example-dungeon-loading.png`
- `example-dungeon-wait.png`
- `example-dungeon-background.png`
- `example-intramwaiting-background.png`
- `example-maintenance-background.png`
- `example-firstenter-background.png`
- `example-deathmatch-background.png`

Regenerate templates with:

```bash
python3 scripts/generate-loading-screen-examples.py
```

### Build and embed

1. Add PNGs under `src/MimesisPlayerEnhancement/Assets/CustomLoadingScreen/`.
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

Check `Assets/RoundStartSound/LICENSE.md` before redistributing third-party audio with your mod build.

## Quick checklist

**Loading screens**

1. Copy example PNGs from `dist/debug/` or `assets/examples/custom-loading-screen/`.
2. Edit them and place under `Assets/CustomLoadingScreen/<Context>/<theme>/`.
3. Rebuild, set `CustomLoadingScreenMode` to `Random` or `Specific`.

**Sounds**

1. Add `.ogg`/`.wav` files to `Assets/RoundStartSound/`.
2. Rebuild, set `RoundStartSoundMode` to `Random` or `Specific`.

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
