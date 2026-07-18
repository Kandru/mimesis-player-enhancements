# Build from source

You need **Docker** only. No local Python, Node.js, .NET SDK, or MIMESIS install is required (game path optional for full Unity references).

```bash
make deps          # first time only — downloads reference assemblies (ops container)
make debug         # mod → dist/debug/ (+ webinterface)
make release       # mod → dist/prod/ (+ webinterface)
make webinterface  # Svelte UI only → dist/webinterface/debug/
make tools         # dev tools + seed scanner → src/*/bin/
make check         # validate locales, format C#, type-check Svelte
make thunderstore  # release + dist/thunderstore/mpe<version>.zip
make clean         # empty dist/debug, prod, webinterface, thunderstore (host only — no Docker)
```

`make` or `make help` lists all targets.

**Containers** (all `docker run --rm` — destroyed after each step):

| Image | Used for |
|-------|----------|
| `mpe-ops:local` | deps bootstrap, locale validation, web staging, Thunderstore packaging |
| `mcr.microsoft.com/dotnet/sdk:10.0` | mod/tools compile, C# format |
| `node:22-alpine` | webinterface Vite build, Svelte type-check |

**Package caches** (named Docker volumes, persist across runs):

| Volume | Caches |
|--------|--------|
| `mpe-nuget-cache` | NuGet packages (`NUGET_PACKAGES`) for dotnet builds |
| `mpe-npm-cache` | npm download cache for web builds and checks |

Reset caches if packages are corrupted or stale:

```bash
docker volume rm mpe-nuget-cache mpe-npm-cache
```

Volumes are created automatically and ownership is fixed for your host user on each build/check run.

If `PathConfig.props` or `MIMESIS_PATH` is set, the game install is mounted read-only into containers for full Unity references.

```bash
SKIP_WEB=1 make debug
COPY_TO_MODS=1 MIMESIS_PATH="/path/to/MIMESIS" make debug
make webinterface CONFIG=Release
```

For architecture, formatting, feature scaffolding, and contribution workflow, see [DEVELOPMENT.md](DEVELOPMENT.md).
