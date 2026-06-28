# Agent guide — Mimesis Player Enhancement

Instructions for AI coding agents working in this repository.

## Do not create `.tmp-*` scratch projects

When you need to inspect game or MelonLoader assemblies, **use the existing dev tools under `src/`**. Do not create throwaway folders like `.tmp-inspect/`, `.tmp-reflect/`, or similar at the repo root.

| Need | Tool | Path |
|------|------|------|
| Game types, methods, constants (`Assembly-CSharp`) | **MimesisInspectionTool** | `src/MimesisInspectionTool/` |
| Full decompiled C# source (browse/read code) | **decompile-game.sh** | `scripts/decompile-game.sh` → `deps/decompiled/` |
| MelonLoader APIs (logging, mod base types, loader internals) | **MimesisReflectionTool** | `src/MimesisReflectionTool/` |

Both tools are in `src/MimesisPlayerEnhancement.sln`, build with `dotnet build`, and output to their own `bin/` folders (not `dist/`).

### Quick start

```bash
# Game metadata (safe, no code execution)
dotnet run --project src/MimesisInspectionTool -- constants MMSaveGameData
dotnet run --project src/MimesisInspectionTool -- member MMSaveGameData CheckSaveSlotID

# MelonLoader runtime reflection
dotnet run --project src/MimesisReflectionTool -- properties MelonLoader.Logging.ColorARGB Green
dotnet run --project src/MimesisReflectionTool -- type MelonLoader.MelonMod
```

See each tool's README for full command reference:

- [src/MimesisInspectionTool/README.md](src/MimesisInspectionTool/README.md)
- [src/MimesisReflectionTool/README.md](src/MimesisReflectionTool/README.md)

### When to extend a tool vs. add a new one

- **New command or printer for the same assembly family** → extend `Program.cs` or the printer in the existing tool.
- **Truly different concern** (e.g. IL decompilation, patch validation) → add a new project under `src/` with a proper name, README, and solution entry — not a `.tmp-*` folder.

### Reference assemblies

Run `./scripts/bootstrap-deps.sh` once to populate `deps/reference/Managed/` and `deps/reference/MelonLoader/net35/`. Tools fall back to `MIMESIS_PATH` or `--game` / `--managed` / `--melonloader` flags when bootstrap paths are missing.

For readable source when inspecting types or call chains, run `./scripts/decompile-game.sh` (requires `dotnet tool install -g ilspycmd`). Output lands in `deps/decompiled/` and is gitignored.

## Mod project layout

- Main mod: `src/MimesisPlayerEnhancement/` (netstandard2.1, Harmony patches)
- Build output: `dist/debug/` or `dist/prod/` (not committed)
- Game references: `deps/reference/` after bootstrap, or local install via `PathConfig.props` / `MIMESIS_PATH`
