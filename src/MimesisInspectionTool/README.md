# MimesisInspectionTool

Small developer utility for exploring **MIMESIS** game assemblies without loading the game or MelonLoader. It uses .NET's `MetadataLoadContext`, so it reads type metadata only and does not execute game code.

Use this when you need to check method signatures, constants (for example save-slot IDs), or type members while working on mod patches.

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download) (or adjust `TargetFramework` in the `.csproj` if needed)
- Game reference assemblies in one of these locations:
  - `./scripts/bootstrap-deps.sh` → `deps/reference/Managed/` (recommended for repo work)
  - A local MIMESIS install via `--game` or `MIMESIS_PATH`

## Build

From the repository root:

```bash
dotnet build src/MimesisInspectionTool/MimesisInspectionTool.csproj
```

Output goes to `src/MimesisInspectionTool/bin/<tfm>/`, not into `dist/` with the mod DLL.

## Run

Always pass `--` before tool arguments when using `dotnet run`:

```bash
dotnet run --project src/MimesisInspectionTool -- [options] <command> [args]
```

Or run the built executable directly:

```bash
./src/MimesisInspectionTool/bin/Debug/net10.0/MimesisInspectionTool constants MMSaveGameData
```

### Options

| Option | Description |
|--------|-------------|
| `--managed <path>` | Directory containing `Assembly-CSharp.dll` (usually `MIMESIS_Data/Managed`) |
| `--game <path>` | MIMESIS install root; resolves `<path>/MIMESIS_Data/Managed` |
| `-h`, `--help` | Show usage |

If neither option is set, the tool tries `MIMESIS_PATH`, then `deps/reference/Managed`.

### Commands

| Command | Description |
|---------|-------------|
| `types [filter]` | List types matching a name substring |
| `type <TypeName>` | Overview of fields, properties, and methods |
| `methods <TypeName> [filter]` | List methods |
| `fields <TypeName> [filter]` | List fields |
| `constants <TypeName>` | List compile-time constants |
| `member <TypeName> <Member>` | Show one method, property, or field |

Type names can be short (`MMSaveGameData`) or fully qualified (`ReluProtocol.MMSaveGameData`).

### Examples

Inspect save-slot constants:

```bash
dotnet run --project src/MimesisInspectionTool -- constants MMSaveGameData
```

Inspect a specific method:

```bash
dotnet run --project src/MimesisInspectionTool -- member MMSaveGameData CheckSaveSlotID
```

Search types related to sessions:

```bash
dotnet run --project src/MimesisInspectionTool -- types Session
```

Use a local game install instead of bootstrapped reference libs:

```bash
MIMESIS_PATH="/path/to/MIMESIS" dotnet run --project src/MimesisInspectionTool -- type VWorld
# or
dotnet run --project src/MimesisInspectionTool -- --game "/path/to/MIMESIS" type VWorld
```

## Notes

- This tool is for **local development only**. It is not loaded by the game.
- Reference assemblies under `deps/reference/` are compile-only extracts; keep them in sync with the game version you target.
- To add new inspection helpers, extend `InspectionPrinter.cs` or add commands in `Program.cs`.
