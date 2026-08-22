# Custom Assets

**Scope:** local · **Config:** [`MimesisPlayerEnhancement_Ui`](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)

Replace the dungeon landing melody and tram disco ball loop with your own embedded audio. Both features are client-only — each player chooses their own sounds.

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

## Disco ball music

Replace the loop that plays when the tram disco ball is turned on. The mirror-ball button click is unchanged.

### Folder layout

```
src/MimesisPlayerEnhancement/Assets/DiscoBallSound/
  vanilla.wav          # optional — export from game (see below)
  my_track.ogg         # your custom loops
  LICENSE.md           # attribution for third-party audio
```

Flat folder — no subfolders. Supported formats: `.wav`, `.ogg`. Tracks should be authored as seamless loops.

### Export vanilla loop from game files

```bash
./scripts/export-disco-ball-sound.sh
# or: MIMESIS_PATH=/path/to/MIMESIS ./scripts/export-disco-ball-sound.sh
```

This writes `src/MimesisPlayerEnhancement/Assets/DiscoBallSound/vanilla.wav` when UnityPy (or Docker) is available.

### Build and configure

1. Add audio files to `Assets/DiscoBallSound/`.
2. Rebuild the mod.
3. Set `DiscoBallSoundMode`:
   - `Vanilla` — original game loop
   - `Random` — pick one track per dungeon (same track if you toggle the ball off and on)
   - `Specific` — play `DiscoBallSoundVariant`
4. When no tracks are embedded, `Random` and `Specific` fall back to vanilla.
5. Adjust `DiscoBallSoundVolume` (default `0.8`) if custom tracks feel too loud.

Check `Assets/DiscoBallSound/LICENSE.md` before redistributing third-party audio with your mod build.

## Quick checklist

**Sounds**

1. Add `.ogg`/`.wav` files to `Assets/RoundStartSound/`.
2. Rebuild, set `RoundStartSoundMode` to `Random` or `Specific`.

**Disco ball music**

1. Add `.ogg`/`.wav` loop files to `Assets/DiscoBallSound/`.
2. Rebuild, set `DiscoBallSoundMode` to `Random` or `Specific`.

**Full config keys →** [User Interface](../CONFIG.md#user-interface--mimesisplayerenhancement_ui)
