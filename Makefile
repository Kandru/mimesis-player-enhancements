# Mimesis Player Enhancement — all targets run in Docker except `make clean`.
# Run `make` or `make help` for usage.

SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c
.DELETE_ON_ERROR:

ROOT := $(abspath $(dir $(lastword $(MAKEFILE_LIST))))
SLN := src/MimesisPlayerEnhancement.sln
MOD_PROJ := src/MimesisPlayerEnhancement/MimesisPlayerEnhancement.csproj
WEB_SRC := $(ROOT)/src/MimesisPlayerEnhancementWeb
WEB_WORKDIR := /repo/src/MimesisPlayerEnhancementWeb
OPS_DIR := $(ROOT)/docker/ops
REF_MANAGED := $(ROOT)/deps/reference/Managed/Assembly-CSharp.dll
REF_MELON := $(ROOT)/deps/reference/MelonLoader/net35/MelonLoader.dll

OPS_IMAGE := mpe-ops:local
DOTNET_IMAGE := mcr.microsoft.com/dotnet/sdk:10.0
NODE_IMAGE := node:22-alpine
WEB_IMAGE := mpe-webdashboard:local

TOOL_PROJECTS := \
	MimesisInspectionTool/MimesisInspectionTool.csproj \
	MimesisReflectionTool/MimesisReflectionTool.csproj \
	MimesisSeedScanner.Cli/MimesisSeedScanner.Cli.csproj \
	MimesisSeedScanner.Mod/MimesisSeedScanner.Mod.csproj

CONFIG ?= Debug
ifeq ($(filter Release Prod prod,$(CONFIG)),)
  DOTNET_CONFIG := Debug
  DIST_SUBDIR := debug
else
  DOTNET_CONFIG := Release
  DIST_SUBDIR := prod
endif

ifndef MIMESIS_PATH
  MIMESIS_PATH := $(shell sed -n 's:.*<GamePath>\(.*\)</GamePath>.*:\1:p' "$(ROOT)/PathConfig.props" 2>/dev/null | head -1)
endif
ifneq ($(strip $(MIMESIS_PATH)),)
  GAME_MOUNT := -v $(MIMESIS_PATH):/game:ro -e MIMESIS_PATH=/game
  DOTNET_GAME_ARGS := -p:GamePath=/game
else
  DOTNET_GAME_ARGS :=
endif

MOD_EXTRA := $(DOTNET_GAME_ARGS)
ifdef SKIP_WEB
  MOD_EXTRA += -p:SkipWebBuild=true
endif
ifdef COPY_TO_MODS
  MOD_EXTRA += -p:CopyToMods=true
endif

DOCKER_REPO_MOUNT := -v "$(ROOT):/repo" -w /repo
DOCKER_USER := --user "$$(id -u):$$(id -g)"

.PHONY: help all debug release webinterface thunderstore tools check clean \
	require-docker ensure-ops-image deps validate-locale stage-web-sources \
	mod format-csharp check-web

.DEFAULT_GOAL := help

# ---------------------------------------------------------------------------
# Help (default target)
# ---------------------------------------------------------------------------

help:
	@echo "Mimesis Player Enhancement — Docker-only build (except make clean)"
	@echo ""
	@echo "Usage: make <target> [variables]"
	@echo ""
	@echo "Targets:"
	@echo "  debug         Mod Debug build → dist/debug/ (includes webinterface)"
	@echo "  release       Mod Release build → dist/prod/ (includes webinterface)"
	@echo "  webinterface  Svelte UI only → dist/webinterface/<debug|prod>/"
	@echo "  thunderstore  Release build + dist/thunderstore/mpe<version>.zip"
	@echo "  tools         Dev tools + seed scanner → src/*/bin/"
	@echo "  check         Validate locales, format C#, type-check Svelte"
	@echo "  clean         Remove dist/ (host only)"
	@echo "  deps          Download reference assemblies (first-time setup)"
	@echo ""
	@echo "Containers: mpe-ops:local, dotnet/sdk:10.0, node:22-alpine (all --rm)"
	@echo ""
	@echo "Variables:"
	@echo "  CONFIG=Release    webinterface or tools prod/Release output"
	@echo "  MIMESIS_PATH=…    Game install for Docker (default: PathConfig.props)"
	@echo "  SKIP_WEB=1        Skip webinterface; pass -p:SkipWebBuild=true"
	@echo "  COPY_TO_MODS=1    Copy DLL into game Mods/ after build"
	@echo ""
	@echo "Examples:"
	@echo "  make debug"
	@echo "  make release"
	@echo "  make webinterface CONFIG=Release"
	@echo "  SKIP_WEB=1 make debug"
	@echo "  COPY_TO_MODS=1 MIMESIS_PATH=/path/to/MIMESIS make debug"

all: debug

# ---------------------------------------------------------------------------
# Docker prerequisites
# ---------------------------------------------------------------------------

require-docker:
	@command -v docker >/dev/null || { echo "error: docker is required" >&2; exit 1; }

ensure-ops-image: require-docker
	@if docker image inspect $(OPS_IMAGE) >/dev/null 2>&1; then \
		echo "==> Ops image OK ($(OPS_IMAGE))"; \
	else \
		echo "==> Building ops image ($(OPS_IMAGE))…"; \
		docker build -t $(OPS_IMAGE) -f "$(OPS_DIR)/Dockerfile" "$(OPS_DIR)"; \
	fi

# ---------------------------------------------------------------------------
# Shared steps (ops container)
# ---------------------------------------------------------------------------

deps: require-docker ensure-ops-image
	@if [[ -f "$(REF_MANAGED)" && -f "$(REF_MELON)" ]]; then \
		echo "==> Reference assemblies OK"; \
	else \
		echo "==> Bootstrapping reference assemblies via Docker…"; \
		docker run --rm \
			$(DOCKER_USER) \
			$(DOCKER_REPO_MOUNT) \
			$(GAME_MOUNT) \
			$(OPS_IMAGE) \
			./scripts/bootstrap-deps.sh; \
	fi

validate-locale: ensure-ops-image
	@echo "==> Validating locale files via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		python3 scripts/validate-locale.sh

stage-web-sources: ensure-ops-image
	@echo "==> Staging web sources via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		./scripts/stage-web-sources.sh

# ---------------------------------------------------------------------------
# Webinterface (Node + web build image)
# ---------------------------------------------------------------------------

webinterface: require-docker deps stage-web-sources ensure-ops-image
	@echo "==> Preparing webinterface output directory ($(DIST_SUBDIR)) via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		sh -c 'rm -rf "/repo/dist/webinterface/$(DIST_SUBDIR)" && mkdir -p "/repo/dist/webinterface/$(DIST_SUBDIR)"'
	@echo "==> Building webinterface image ($(WEB_IMAGE))…"
	@docker build -t $(WEB_IMAGE) -f "$(WEB_SRC)/Dockerfile" "$(WEB_SRC)"
	@echo "==> Exporting webinterface ($(DIST_SUBDIR)) via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		-v "$(ROOT)/dist/webinterface/$(DIST_SUBDIR):/out" \
		$(WEB_IMAGE)
	@test -f "$(ROOT)/dist/webinterface/$(DIST_SUBDIR)/index.html" \
		|| { echo "error: web build did not produce index.html" >&2; exit 1; }
	@echo "==> Webinterface ready: dist/webinterface/$(DIST_SUBDIR)/"

# ---------------------------------------------------------------------------
# Mod (dotnet SDK container)
# ---------------------------------------------------------------------------

mod: require-docker deps validate-locale
	@if [[ -n "$(strip $(MIMESIS_PATH))" ]]; then \
		echo "==> Using game assemblies from $(MIMESIS_PATH)"; \
	else \
		echo "==> Using bootstrap reference assemblies (set MIMESIS_PATH for full Unity refs)"; \
	fi
	@echo "==> dotnet build $(MOD_PROJ) -c $(DOTNET_CONFIG)"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(GAME_MOUNT) \
		$(DOTNET_IMAGE) \
		dotnet build $(MOD_PROJ) -c $(DOTNET_CONFIG) $(MOD_EXTRA)
	@echo "==> Mod ready: dist/$(DIST_SUBDIR)/MimesisPlayerEnhancement.dll"

ifeq ($(SKIP_WEB),1)
debug: DOTNET_CONFIG=Debug
debug: DIST_SUBDIR=debug
debug: mod
release: DOTNET_CONFIG=Release
release: DIST_SUBDIR=prod
release: mod
else
debug: DOTNET_CONFIG=Debug
debug: DIST_SUBDIR=debug
debug: webinterface mod
release: DOTNET_CONFIG=Release
release: DIST_SUBDIR=prod
release: webinterface mod
endif

# ---------------------------------------------------------------------------
# Thunderstore (ops container)
# ---------------------------------------------------------------------------

thunderstore: release ensure-ops-image
	@echo "==> Packaging Thunderstore release via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		./scripts/package-thunderstore.sh

# ---------------------------------------------------------------------------
# Dev tools (dotnet SDK container)
# ---------------------------------------------------------------------------

tools: require-docker deps
	@echo "==> Formatting C# ($(DOTNET_CONFIG)) via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(GAME_MOUNT) \
		$(DOTNET_IMAGE) \
		dotnet format $(SLN) --verbosity minimal $(DOTNET_GAME_ARGS)
	@echo "==> Building dev tools ($(DOTNET_CONFIG)) via Docker…"
	@for rel in $(TOOL_PROJECTS); do \
		echo "==> dotnet build $$rel"; \
		docker run --rm \
			$(DOCKER_USER) \
			$(DOCKER_REPO_MOUNT) \
			$(GAME_MOUNT) \
			$(DOTNET_IMAGE) \
			dotnet build "src/$$rel" -c $(DOTNET_CONFIG); \
	done
	@echo "==> Tools ready under src/*/bin/$(DOTNET_CONFIG)/"

# ---------------------------------------------------------------------------
# Quality checks
# ---------------------------------------------------------------------------

format-csharp: require-docker
	@echo "==> Formatting C# via Docker…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(GAME_MOUNT) \
		$(DOTNET_IMAGE) \
		dotnet format $(SLN) --verbosity minimal $(DOTNET_GAME_ARGS)

check-web: require-docker stage-web-sources
	@echo "==> Type-checking Svelte via Docker…"
	@docker run --rm \
		-v "$(ROOT):/repo:ro" \
		$(NODE_IMAGE) \
		sh -c 'set -euo pipefail; \
			rm -rf /tmp/web; \
			cp -a /repo/src/MimesisPlayerEnhancementWeb /tmp/web; \
			cd /tmp/web; \
			npm ci --silent; \
			node scripts/generate-wiki.mjs; \
			node scripts/generate-changelog.mjs; \
			npm run check'

check: ensure-ops-image require-docker
	@echo "==> Running quality checks via Docker…"
	@echo "==> [1/3] Validating locale files…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		python3 scripts/validate-locale.sh
	@echo "==> [2/3] Formatting C#…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(GAME_MOUNT) \
		$(DOTNET_IMAGE) \
		dotnet format $(SLN) --verbosity minimal $(DOTNET_GAME_ARGS)
	@echo "==> [3/3] Type-checking Svelte…"
	@docker run --rm \
		$(DOCKER_USER) \
		$(DOCKER_REPO_MOUNT) \
		$(OPS_IMAGE) \
		./scripts/stage-web-sources.sh
	@docker run --rm \
		-v "$(ROOT):/repo:ro" \
		$(NODE_IMAGE) \
		sh -c 'set -euo pipefail; \
			rm -rf /tmp/web; \
			cp -a /repo/src/MimesisPlayerEnhancementWeb /tmp/web; \
			cd /tmp/web; \
			npm ci --silent; \
			node scripts/generate-wiki.mjs; \
			node scripts/generate-changelog.mjs; \
			npm run check'
	@echo "==> All checks passed"

# ---------------------------------------------------------------------------
# Clean (host only)
# ---------------------------------------------------------------------------

clean:
	@echo "==> Removing dist/…"
	@rm -rf "$(ROOT)/dist"
	@echo "==> Clean complete"
