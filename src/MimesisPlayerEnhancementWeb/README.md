# Mimesis Player Enhancement — Web Dashboard (Svelte)

Svelte 5 + Vite + Tailwind 4 frontend for the in-game web dashboard.

## Build (Docker only — no local Node.js required)

From the repository root:

```bash
make webinterface
make webinterface CONFIG=Release
```

This builds inside Docker (`node:22-alpine`) and writes output to `dist/webinterface/debug/` or `dist/webinterface/prod/`. Static images live in `public/img/` and are copied into the build output by Vite. npm and NuGet packages are cached in named Docker volumes (`mpe-npm-cache`, `mpe-nuget-cache`).

`make debug` / `make release` run the web build automatically unless `SKIP_WEB=1`.

For C#-only iteration when web assets are already built:

```bash
SKIP_WEB=1 make debug
```

### Standalone Docker image

The [`Dockerfile`](Dockerfile) builds a self-contained image (not used by `make webinterface`). Requires BuildKit:

```bash
DOCKER_BUILDKIT=1 docker build -t mpe-webdashboard:local .
```

## Development

With Docker and a running game dashboard on port 8001:

```bash
docker run --rm -it -v "$(pwd)/src/MimesisPlayerEnhancementWeb:/app" -w /app -p 5173:5173 node:22-alpine sh -c "npm install && npm run dev -- --host 0.0.0.0"
```

Vite proxies `/api` to `http://127.0.0.1:8001` (use host networking or adjust `vite.config.ts` if needed).

## Layout

TailAdmin-inspired shell: left sidebar, top header (host/guest badge, blind mode, dark mode), card-based content. Hash routes match the previous Alpine dashboard (`#/players`, `#/minimap`, etc.).
