# Build from source

You need [.NET SDK 8+](https://dotnet.microsoft.com/download). You do **not** need MIMESIS installed to compile.

```bash
chmod +x scripts/*.sh
./scripts/bootstrap-deps.sh   # first time only — downloads build dependencies
./scripts/build.sh            # format + compile → dist/debug/MimesisPlayerEnhancement.dll
./scripts/build.sh Release    # format + compile → dist/prod/MimesisPlayerEnhancement.dll
```

Skip auto-format with `SKIP_FORMAT=true ./scripts/build.sh`.

To copy the built DLL straight into your game for testing:

```bash
COPY_TO_MODS=true MIMESIS_PATH="/path/to/MIMESIS" ./scripts/build.sh
```

For architecture, formatting, feature scaffolding, and contribution workflow, see [DEVELOPMENT.md](DEVELOPMENT.md).
