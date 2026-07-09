# Mimesis Player Enhancement — Web Dashboard (Svelte)

Svelte 5 + Vite + Tailwind 4 frontend for the in-game web dashboard.

## Build (Docker only — no local Node.js required)

From the repository root:

```bash
./scripts/build-webdashboard.sh
```

This builds inside `node:22-alpine`, writes output to `src/MimesisPlayerEnhancement/Assets/WebDashboard/`, and preserves static images under `img/`.

`./scripts/build.sh` runs the web build automatically unless `SKIP_WEB_BUILD=true`.

For dotnet-only builds without a fresh frontend:

```bash
SKIP_WEB_BUILD=true ./scripts/build.sh
# or
dotnet build -p:SkipWebBuild=true
```

## Development

With Docker and a running game dashboard on port 8001:

```bash
docker run --rm -it -v "$(pwd)/src/MimesisPlayerEnhancementWeb:/app" -w /app -p 5173:5173 node:22-alpine sh -c "npm install && npm run dev -- --host 0.0.0.0"
```

Vite proxies `/api` to `http://127.0.0.1:8001` (use host networking or adjust `vite.config.ts` if needed).

## Layout

TailAdmin-inspired shell: left sidebar, top header (host/guest badge, blind mode, dark mode), card-based content. Hash routes match the previous Alpine dashboard (`#/players`, `#/minimap`, etc.).
