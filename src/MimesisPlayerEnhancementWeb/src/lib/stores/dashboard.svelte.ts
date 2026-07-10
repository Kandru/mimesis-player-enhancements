import Api from '../api';
import { loadLocale, resolveBrowserLocale } from '../i18n';
import type {
  ItemOptionDto,
  LeaderboardDto,
  MinimapAreaDto,
  MinimapMarkerDto,
  MinimapPayload,
  MinimapTrainDto,
  PlayerDto,
  PlayerStatsDto,
  QuickPresetDto,
  SettingsDto,
  SnapshotPayload,
  StatusDto,
} from '../types';
import { isLobbyRoute } from '../playerHelpers';
import { readCachedGlobalSettings, writeCachedGlobalSettings } from '../settingsCache';
import { isValidSteamId, OFFLINE_ROUTES, parseHash } from '../utils';

function createInitialStatus(): StatusDto {
  return {
    isConnected: false,
    isHost: false,
    saveSlotId: -1,
    lobbyName: '',
    modVersion: '',
    listenUrl: '',
    snapshotVersion: 0,
    configVersion: 0,
    joinAnytimeRoutingCount: 0,
    locale: 'en',
    sessionScene: '',
  };
}

function isBlindModeScene(sessionScene: string | undefined): boolean {
  return sessionScene === 'dungeon' || sessionScene === 'death_match';
}

class DashboardStore {
  status = $state<StatusDto>(createInitialStatus());
  players = $state<PlayerDto[]>([]);
  leaderboard = $state<LeaderboardDto | null>(null);
  minimapRaw = $state<MinimapPayload | null>(null);
  minimap = $state<MinimapPayload | null>(null);
  playerStats = $state<PlayerStatsDto | null>(null);

  route = $state('home');
  settingsSubRoute = $state('');
  steamId = $state<string | null>(null);

  toastMessage = $state('');
  toastVisible = $state(false);
  pageError = $state('');
  apiError = $state(false);
  loadingStats = $state(false);
  loadingSettings = $state(false);

  settingsGlobal = $state<SettingsDto | null>(null);
  settingsSave = $state<SettingsDto | null>(null);
  headerSearchQuery = $state('');
  selectedSettingsSectionId = $state('');
  savingSettingKey = $state('');

  saveProfile = $state<{ profile: { mode: string; presetId: string; label: string } } | null>(null);
  quickPresets = $state<QuickPresetDto[]>([]);
  loadingSaveProfile = $state(false);
  applyingProfile = $state(false);
  shareModalOpen = $state(false);
  shareModalMode = $state<'export' | 'import'>('export');
  shareModalText = $state('');
  shareModalName = $state('');
  selectedLoadPresetId = $state('');

  itemCatalog = $state<ItemOptionDto[]>([]);
  dungeonCatalog = $state<Array<{ id: string; label: string }>>([]);

  minimapFocusSteamId = $state(localStorage.getItem('minimapFocusSteamId') || '');
  minimapAreaId = $state(localStorage.getItem('minimapAreaId') || '');

  sidebarOpen = $state(false);

  playerBlindModeUserEnabled = $state(true);
  playerBlindModeAutoSuspended = $state(false);
  darkMode = $state(localStorage.getItem('darkMode') !== 'false');

  private eventSource: EventSource | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  private lastSnapshotVersion = -1;
  private lastRoute = '';
  private lastSettingsSubRoute = '';
  private lastSteamId: string | null = null;
  private localPlayerWasAlive: boolean | null = null;
  private lastSessionScene = '';
  private globalSettingsPromise: Promise<void> | null = null;
  private saveSettingsPromise: Promise<void> | null = null;
  private saveProfilePromise: Promise<void> | null = null;

  get playerBlindMode() {
    return this.playerBlindModeUserEnabled
      && isBlindModeScene(this.status.sessionScene)
      && !this.playerBlindModeAutoSuspended;
  }

  get isGameRoute() {
    return ['players', 'minimap', 'leaderboard', 'settings', 'player'].includes(this.route);
  }

  init() {
    if (this.darkMode) document.documentElement.classList.add('dark');
    const cachedGlobal = readCachedGlobalSettings();
    if (cachedGlobal) this.settingsGlobal = cachedGlobal;
    void this.loadGlobalSettings(true, false);

    const { route, settingsSubRoute, steamId } = parseHash();
    this.route = route;
    this.settingsSubRoute = settingsSubRoute;
    this.steamId = steamId;
    window.addEventListener('hashchange', () => this.onHashChange());

    Api.getStatus()
      .then(async (status) => {
        await loadLocale(status.locale || resolveBrowserLocale());
        this.applySnapshot({ status, players: [], leaderboard: null });
      })
      .catch(async () => {
        await loadLocale(resolveBrowserLocale());
      })
      .finally(() => {
        this.setConnectedMode();
        this.loadPageData(false);
        void this.prefetchDashboardData();
        this.connectSse();
      });
  }

  private connectSse() {
    this.eventSource?.close();
    const source = new EventSource('/api/events');
    source.addEventListener('snapshot', (ev) => {
      try {
        this.applySnapshot(JSON.parse(ev.data));
        if (
          this.route !== 'global-settings' &&
          !(this.route === 'settings' && (this.settingsSubRoute === 'customize' || !this.settingsSubRoute))
        ) {
          this.loadPageData(false);
        }
      } catch {
        /* ignore */
      }
    });
    source.addEventListener('minimap', (ev) => {
      try {
        this.applyMinimapLive(JSON.parse(ev.data));
      } catch {
        /* ignore */
      }
    });
    source.onerror = () => {
      this.apiError = true;
      this.status.isConnected = false;
      this.setConnectedMode();
      this.ensureDefaultRoute();
    };
    this.eventSource = source;
  }

  onHashChange() {
    const prevRoute = this.lastRoute;
    const prevSteam = this.lastSteamId;
    const prevSubRoute = this.lastSettingsSubRoute;
    const parsed = parseHash();
    this.route = parsed.route;
    this.settingsSubRoute = parsed.settingsSubRoute;
    this.steamId = parsed.steamId;
    this.headerSearchQuery = '';
    const onSaveCustomize =
      parsed.route === 'settings'
      && (parsed.settingsSubRoute === 'customize' || this.saveProfile?.profile?.mode === 'custom');
    if (parsed.route !== 'global-settings' && !onSaveCustomize) {
      this.selectedSettingsSectionId = '';
    }
    this.setConnectedMode();
    if (
      this.route !== prevRoute
      || this.steamId !== prevSteam
      || this.settingsSubRoute !== prevSubRoute
    ) {
      this.loadPageData(false);
    }
  }

  setConnectedMode() {
    const waiting = !this.status.isConnected && !OFFLINE_ROUTES.includes(this.route);
    document.body.classList.toggle('waiting', waiting);
    document.body.classList.toggle('connected', this.status.isConnected);
  }

  ensureDefaultRoute() {
    if (!this.status.isConnected && isLobbyRoute(this.route)) {
      location.hash = '#/home';
      const p = parseHash();
      this.route = p.route;
    } else if (this.status.isConnected && (!location.hash || location.hash === '#')) {
      location.hash = '#/players';
      this.route = 'players';
    }
  }

  showToast(message: string) {
    this.toastMessage = message;
    this.toastVisible = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.toastVisible = false;
    }, 3500);
  }

  toggleDarkMode() {
    this.darkMode = !this.darkMode;
    localStorage.setItem('darkMode', String(this.darkMode));
    document.documentElement.classList.toggle('dark', this.darkMode);
  }

  async togglePlayerBlindMode() {
    this.playerBlindModeUserEnabled = !this.playerBlindModeUserEnabled;
    this.playerBlindModeAutoSuspended = false;
    const local = this.getLocalPlayer();
    if (local && !local.isAlive && this.playerBlindModeUserEnabled) {
      this.playerBlindModeAutoSuspended = true;
    }
    try {
      await Api.setBlindMode(this.playerBlindMode);
    } catch {
      /* local fallback */
    }
    this.applyMinimapFilter(true);
    if (this.playerBlindMode && this.hasActivePlayerCheats()) {
      Api.disableAllPlayerCheats().catch((e) => this.showToast(e.message));
    }
  }

  getLocalPlayer() {
    return this.players.find((p) => p.isLocal && p.playerUid);
  }

  hasActivePlayerCheats() {
    return this.players.some((p) => p.godMode || p.noClip);
  }

  syncBlindModeForLocalPlayer() {
    const local = this.getLocalPlayer();
    if (!local) return;
    const isAlive = local.isAlive;
    const wasAlive = this.localPlayerWasAlive;
    if (wasAlive === null) {
      if (!isAlive && this.playerBlindModeUserEnabled) this.playerBlindModeAutoSuspended = true;
      this.localPlayerWasAlive = isAlive;
    } else if (wasAlive && !isAlive) {
      if (this.playerBlindModeUserEnabled) this.playerBlindModeAutoSuspended = true;
      this.localPlayerWasAlive = isAlive;
    } else if (!wasAlive && isAlive) {
      if (this.playerBlindModeUserEnabled && this.playerBlindModeAutoSuspended) {
        this.playerBlindModeAutoSuspended = false;
      }
      this.localPlayerWasAlive = isAlive;
    }
    this.applyMinimapFilter(true);
  }

  applySnapshot(payload: SnapshotPayload) {
    const wasConnected = this.status.isConnected;
    const previousSessionScene = this.lastSessionScene;
    this.status = payload.status || this.status;
    if (payload.playersLiveOnly) {
      for (const live of payload.players || []) {
        const id = String(live.steamId);
        const idx = this.players.findIndex((p) => String(p.steamId) === id);
        if (idx >= 0) this.players[idx] = live;
        else this.players.push(live);
      }
    } else {
      this.players = payload.players || [];
      this.leaderboard = payload.leaderboard ?? this.leaderboard;
      if (payload.minimap != null) this.minimapRaw = payload.minimap;
    }
    if (this.status.isConnected) {
      this.syncBlindModeForLocalPlayer();
      if (this.playerBlindMode && this.hasActivePlayerCheats()) {
        Api.disableAllPlayerCheats().catch(() => {});
      }
      const sessionScene = this.status.sessionScene ?? '';
      if (sessionScene !== previousSessionScene) {
        this.lastSessionScene = sessionScene;
        this.applyMinimapFilter(true);
      }
    }
    this.apiError = false;
    this.setConnectedMode();
    if (!this.status.isConnected && wasConnected && this.isGameRoute) {
      this.players = [];
      this.leaderboard = null;
      this.playerStats = null;
      this.settingsSave = null;
      this.saveProfile = null;
      this.quickPresets = [];
      this.minimapRaw = null;
      this.minimap = null;
      this.localPlayerWasAlive = null;
      this.lastSessionScene = '';
    } else if (this.status.isConnected && !wasConnected) {
      void this.prefetchDashboardData();
    } else if (this.status.isConnected && (this.route === 'minimap' || this.route === 'player')) {
      this.applyMinimapFilter();
    }
    this.ensureDefaultRoute();
  }

  applyMinimapLive(minimap: MinimapPayload) {
    if (!this.status.isConnected || !minimap) return;
    this.minimapRaw = minimap;
    if (this.route === 'minimap' || this.route === 'player') {
      this.applyMinimapFilter(false);
    }
  }

  applyMinimapFilter(force = false) {
    if (!this.minimapRaw) {
      this.minimap = null;
      return;
    }

    const raw = this.minimapRaw;
    const areas = raw.areas || [];
    const connectedIds = new Set(
      this.players.filter((p) => p.playerUid).map((p) => String(p.steamId)),
    );
    if (this.minimapFocusSteamId && !connectedIds.has(this.minimapFocusSteamId)) {
      this.minimapFocusSteamId = '';
      localStorage.removeItem('minimapFocusSteamId');
    }

    const markers = this.filterMinimapMarkers(raw.markers || []);
    const activeAreaId = this.resolveMinimapAreaId(markers, areas, raw);
    const activeArea = areas.find((area) => area.id === activeAreaId) ?? null;
    const areaMarkers = markers.filter((marker) => {
      if (!activeAreaId) return true;
      if (!marker.areaId) return raw.displayMode === 'markers-only';
      return marker.areaId === activeAreaId;
    });

    this.minimap = {
      layoutVersion: raw.layoutVersion,
      layoutKind: raw.layoutKind,
      displayMode: raw.displayMode,
      sceneLabel: raw.sceneLabel,
      defaultAreaId: raw.defaultAreaId,
      bounds: activeArea?.bounds ?? raw.bounds,
      areas: raw.areas,
      tiles: activeArea?.tiles?.length ? activeArea.tiles : raw.tiles || [],
      connections: raw.connections,
      markers: areaMarkers,
      train: this.resolveTrainForArea(raw.train, activeAreaId),
      activeAreaId,
      activeAreaLabel: activeArea?.label ?? '',
      connectionPoints: activeArea?.connectionPoints?.length
        ? activeArea.connectionPoints
        : [],
      blindMode: this.playerBlindMode,
    };

    if (force || activeAreaId !== this.minimapAreaId) {
      this.minimapAreaId = activeAreaId;
      localStorage.setItem('minimapAreaId', activeAreaId);
    }
  }

  private resolveMinimapFocus() {
    if (this.route === 'player' && this.steamId) return String(this.steamId);
    if (this.minimapFocusSteamId) return this.minimapFocusSteamId;
    const local = this.getLocalPlayer();
    if (local && isValidSteamId(local.steamId)) return String(local.steamId);
    const first = this.players.find((p) => isValidSteamId(p.steamId));
    return first ? String(first.steamId) : '';
  }

  private resolveMinimapAreaId(
    filteredMarkers: MinimapMarkerDto[],
    areas: MinimapAreaDto[],
    raw: MinimapPayload,
  ) {
    if (!areas.length) return '';

    const focusId = this.resolveMinimapFocus();
    if (focusId && filteredMarkers.length > 0) {
      const focused = filteredMarkers.find((marker) => String(marker.steamId) === focusId);
      if (focused?.areaId) return focused.areaId;
    }

    if (this.minimapAreaId && areas.some((area) => area.id === this.minimapAreaId)) {
      return this.minimapAreaId;
    }

    if (raw.defaultAreaId) return raw.defaultAreaId;
    return areas[0].id;
  }

  private resolveTrainForArea(
    rawTrain: MinimapTrainDto | null | undefined,
    activeAreaId: string,
  ) {
    if (!rawTrain || !activeAreaId) return null;
    if (rawTrain.areaId && rawTrain.areaId !== activeAreaId) return null;
    return rawTrain;
  }

  private filterMinimapMarkers(markers: MinimapPayload['markers']) {
    const connectedIds = new Set(
      this.players.filter((p) => p.playerUid).map((p) => String(p.steamId)),
    );
    let filtered = (markers || []).filter((m) => connectedIds.has(String(m.steamId)));
    if (this.playerBlindMode) {
      filtered = filtered.filter((m) => m.isLocal);
    }
    return filtered;
  }

  prefetchDashboardData(force = false) {
    void this.loadGlobalSettings(true, force);
    void this.loadItemCatalog();
    void this.loadDungeonCatalog();
    if (this.status.isConnected && this.status.isHost) {
      void this.loadSaveProfileData(force);
      void this.loadSaveSettings(true, force);
    }
  }

  async loadGlobalSettings(background = false, force = false) {
    if (!force && this.settingsGlobal) return;
    if (this.globalSettingsPromise) return this.globalSettingsPromise;

    const showSpinner = !background && this.route === 'global-settings';
    if (showSpinner) this.loadingSettings = true;

    this.globalSettingsPromise = (async () => {
      try {
        this.settingsGlobal = await Api.getGlobalSettings();
        if (this.settingsGlobal) writeCachedGlobalSettings(this.settingsGlobal);
      } catch {
        /* optional during background prefetch */
      } finally {
        if (showSpinner) this.loadingSettings = false;
        this.globalSettingsPromise = null;
      }
    })();

    return this.globalSettingsPromise;
  }

  async loadSaveSettings(background = false, force = false) {
    if (!this.status.isHost) return;
    if (!force && this.settingsSave) return;
    if (this.saveSettingsPromise) return this.saveSettingsPromise;

    const showSpinner =
      !background
      && this.route === 'settings'
      && (this.settingsSubRoute === 'customize' || this.saveProfile?.profile?.mode === 'custom');
    if (showSpinner) this.loadingSettings = true;

    this.saveSettingsPromise = (async () => {
      try {
        this.settingsSave = await Api.getSaveSettings();
      } catch {
        /* optional during background prefetch */
      } finally {
        if (showSpinner) this.loadingSettings = false;
        this.saveSettingsPromise = null;
      }
    })();

    return this.saveSettingsPromise;
  }

  async loadPageData(force = false) {
    const onGlobal = this.route === 'global-settings';
    const onSettings = this.route === 'settings' && this.status.isHost;
    const settingsActivelyEditing =
      onSettings
      && (this.settingsSubRoute === 'customize' || this.saveProfile?.profile?.mode === 'custom');

    if (!this.status.isConnected && !OFFLINE_ROUTES.includes(this.route)) {
      this.lastRoute = this.route;
      this.lastSettingsSubRoute = this.settingsSubRoute;
      this.lastSteamId = this.steamId;
      this.lastSnapshotVersion = this.status.snapshotVersion;
      return;
    }

    if (!force && this.savingSettingKey) return;
    if (
      !force
      && this.route === this.lastRoute
      && this.settingsSubRoute === this.lastSettingsSubRoute
      && this.route !== 'player'
      && !settingsActivelyEditing
    ) {
      return;
    }
    if (!force && this.route === 'player' && this.steamId === this.lastSteamId &&
        this.status.snapshotVersion === this.lastSnapshotVersion) return;

    this.pageError = '';
    try {
      if (this.status.isConnected && this.route === 'player' && this.steamId && this.status.isHost) {
        this.loadingStats = true;
        try {
          this.playerStats = await Api.getPlayerStats(this.steamId);
        } finally {
          this.loadingStats = false;
        }
      } else if (this.route !== 'player') {
        this.playerStats = null;
      }

      if (onGlobal) {
        await this.loadGlobalSettings(false, false);
        void this.loadItemCatalog();
        void this.loadDungeonCatalog();
      }
      if (onSettings) {
        await this.loadSaveProfileData(force);
        const showSavePanel =
          this.settingsSubRoute === 'customize' || this.saveProfile?.profile?.mode === 'custom';
        if (showSavePanel) {
          await this.loadSaveSettings(false, force);
        }
      }
      if ((this.route === 'minimap' || this.route === 'player') && this.minimapRaw) {
        this.applyMinimapFilter(force);
      }
      if (this.status.isHost && this.route === 'players') await this.loadItemCatalog();
    } catch (e) {
      this.pageError = e instanceof Error ? e.message : String(e);
    }
    this.lastRoute = this.route;
    this.lastSettingsSubRoute = this.settingsSubRoute;
    this.lastSteamId = this.steamId;
    this.lastSnapshotVersion = this.status.snapshotVersion;
  }

  async loadItemCatalog() {
    try {
      const res = await Api.getItems();
      this.itemCatalog = res.items || [];
    } catch {
      /* optional */
    }
  }

  async loadDungeonCatalog() {
    try {
      const res = await Api.getDungeons();
      this.dungeonCatalog = res.dungeons || [];
    } catch {
      /* optional */
    }
  }

  async loadSaveProfileData(force = false) {
    if (!this.status.isHost) return;
    if (!force && this.saveProfile) return;
    if (this.saveProfilePromise) return this.saveProfilePromise;

    this.loadingSaveProfile = true;
    this.saveProfilePromise = (async () => {
      try {
        const [profile, presets] = await Promise.all([
          Api.getSaveProfile(),
          Api.getQuickPresets(),
        ]);
        this.saveProfile = profile as typeof this.saveProfile;
        this.quickPresets = presets.presets || [];
      } catch {
        /* optional during background prefetch */
      } finally {
        this.loadingSaveProfile = false;
        this.saveProfilePromise = null;
      }
    })();

    return this.saveProfilePromise;
  }
}

export const dashboard = new DashboardStore();
