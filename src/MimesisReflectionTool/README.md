# MimesisReflectionTool

Developer utility for exploring **MelonLoader** assemblies via runtime reflection. Unlike [MimesisInspectionTool](../MimesisInspectionTool/README.md), which reads game metadata without executing code, this tool loads MelonLoader DLLs and resolves dependencies from the MelonLoader folder.

Use this when you need to inspect MelonLoader APIs (logging colors, mod base types, loader internals) while working on the mod.

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- MelonLoader reference assemblies in one of these locations:
  - `./scripts/bootstrap-deps.sh` → `deps/reference/MelonLoader/net35/` (recommended for repo work)
  - A local MIMESIS install via `--game` or `MIMESIS_PATH`

## Build

From the repository root:

```bash
dotnet build src/MimesisReflectionTool/MimesisReflectionTool.csproj
```

Output goes to `src/MimesisReflectionTool/bin/<tfm>/`, not into `dist/` with the mod DLL.

## Run

Always pass `--` before tool arguments when using `dotnet run`:

```bash
dotnet run --project src/MimesisReflectionTool -- [options] <command> [args]
```

Or run the built executable directly:

```bash
./src/MimesisReflectionTool/bin/Debug/net10.0/MimesisReflectionTool properties MelonLoader.Logging.ColorARGB Green
```

### Options

| Option | Description |
|--------|-------------|
| `--melonloader <path>` | Directory containing `MelonLoader.dll` (usually `MelonLoader/net35`) |
| `--game <path>` | MIMESIS install root; resolves `<path>/MelonLoader/net35` |
| `--assembly <name>` | Other DLL in the MelonLoader folder (default: `MelonLoader`) |
| `-h`, `--help` | Show usage |

If neither path option is set, the tool tries `MIMESIS_PATH`, then `deps/reference/MelonLoader/net35`.

### Commands

| Command | Description |
|---------|-------------|
| `types [filter]` | List types matching a name substring |
| `type <TypeName>` | Overview of fields, properties, and methods |
| `properties <TypeName> [filter]` | List properties |
| `methods <TypeName> [filter]` | List methods |
| `fields <TypeName> [filter]` | List fields |
| `constants <TypeName>` | List compile-time constants |
| `member <TypeName> <Member>` | Show one method, property, or field |

Type names must be fully qualified (for example `MelonLoader.Logging.ColorARGB`).

### Examples

Find green color constants on MelonLoader's logging API:

```bash
dotnet run --project src/MimesisReflectionTool -- properties MelonLoader.Logging.ColorARGB Green
```

Inspect the mod base type:

```bash
dotnet run --project src/MimesisReflectionTool -- type MelonLoader.MelonMod
```

Search for logger-related types:

```bash
dotnet run --project src/MimesisReflectionTool -- types Logger
```

## Notes

- This tool is for **local development only**. It is not loaded by the game.
- For **game** types (`Assembly-CSharp`), use MimesisInspectionTool instead — it is safer and does not require loading Unity/MelonLoader at runtime.
- To add new reflection helpers, extend `ReflectionPrinter.cs` or add commands in `Program.cs`.
