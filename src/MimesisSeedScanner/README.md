# MimesisSeedScanner

Developer tool for precomputing **dungeon map flavor** seed pools baked into the main mod.

## Architecture

1. **`MimesisSeedScanner.Mod`** — MelonLoader dev mod; **F10** exports `scan-catalog.json` (Unity tile/flow bake, seconds)
2. **`MimesisSeedScanner.Cli`** — headless scan + codegen (net10.0, multi-threaded, resumable)
3. **`MimesisSeedScanner/`** — shared catalog types, scoring, pool selection

## Quick start

```bash
# Build catalog-export mod
dotnet build src/MimesisSeedScanner.Mod/MimesisSeedScanner.Mod.csproj -c Release
cp src/MimesisSeedScanner.Mod/bin/Release/MimesisSeedScanner.dll "$MIMESIS_PATH/Mods/"

# In game (main menu): press F10 → MelonLoader/UserData/scan-catalog.json

# Headless scan
dotnet run --project src/MimesisSeedScanner.Cli -- scan \
  --catalog /path/to/scan-catalog.json

# Codegen + convenience script
./scripts/generate-dungeon-seeds.sh seed-scan-results.json
```

Or use the all-in-one script (runs scan if catalog exists, then codegen):

```bash
./scripts/generate-dungeon-seeds.sh
```

## CLI commands

### `scan`

```bash
dotnet run --project src/MimesisSeedScanner.Cli -- scan \
  --catalog scan-catalog.json \
  [--output seed-scan-results.json] \
  [--max-seed 2147483647] \
  [--pool-size 500] \
  [--seed-stride 100000] \
  [--threads 16] \
  [--time-budget 4h] \
  [--shard-dir seed-scan-shards]
```

- Parallel workers with shard checkpoints (resume after interrupt)
- Top candidates per flavor kept in memory; merge applies percentile cut + random sample of `--pool-size`
- **24 curated flavors** (see wiki)

### `codegen`

```bash
dotnet run --project src/MimesisSeedScanner.Cli -- codegen seed-scan-results.json \
  --output src/MimesisPlayerEnhancement/Features/DungeonRandomizer/DungeonSeedPools.Generated.cs
```

### `verify`

Dump offline-generator metrics for spot-checking parity:

```bash
dotnet run --project src/MimesisSeedScanner.Cli -- verify \
  --catalog scan-catalog.json --flow FlowId --seeds 1,42,999
```

## When to rescan

- Game update changes dungeon flows or tile prefabs
- New flavors or scoring changes in `SeedScoring.cs`
- Pool size or scan range changes

See [docs/DEVELOPMENT.md](../../docs/DEVELOPMENT.md#updating-the-predefined-dungeon-seeds) for the full maintainer workflow.

## Flavors

See [dungeon-randomizer wiki](../../docs/wiki/features/dungeon-randomizer.md) for player-facing descriptions.
