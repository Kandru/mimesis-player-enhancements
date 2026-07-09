import Api from '../api';
import { loadLocale, resolveBrowserLocale } from '../i18n';
import type {
  ItemOptionDto,
  LeaderboardDto,
  MinimapPayload,
  PlayerDto,
  PlayerStatsDto,
  QuickPresetDto,
  SettingsDto,
  SnapshotPayload,
  StatusDto,
} from '../types';
import { isLobbyRoute } from '../playerHelpers';
import { OFFLINE_ROUTES, parseHash } from '../utils';

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
  };
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
  mobileSidebarOpen = $state(false);

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
  spawnSelections = $state<Record<string, { itemId: string; percent: number }>>({});

  minimapShowAll = $state(false);
  minimapFocusSteamId = $state(localStorage.getItem('minimapFocusSteamId') || '');
  minimapAreaId = $state(localStorage.getItem('minimapAreaId') || '');

  playerBlindModeUserEnabled = $state(true);
  playerBlindModeAutoSuspended = $state(false);
  darkMode = $state(localStorage.getItem('darkMode') !== 'false');

  private eventSource: EventSource | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  private lastSnapshotVersion = -1;
  private lastRoute = '';
  private lastSteamId: string | null = null;
  private localPlayerWasAlive: boolean | null = null;

  get playerBlindMode() {
    return this.playerBlindModeUserEnabled && !this.playerBlindModeAutoSuspended;
  }

  get isGameRoute() {
    return ['players', 'minimap', 'leaderboard', 'settings', 'player'].includes(this.route);
  }

  init() {
    if (this.darkMode) document.documentElement.classList.add('dark');
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
        if (this.route === 'global-settings') this.loadPageData(true);
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
    };
    this.eventSource = source;
  }

  onHashChange() {
    const prevRoute = this.lastRoute;
    const prevSteam = this.lastSteamId;
    const parsed = parseHash();
    this.route = parsed.route;
    this.settingsSubRoute = parsed.settingsSubRoute;
    this.steamId = parsed.steamId;
    this.mobileSidebarOpen = false;
    this.headerSearchQuery = '';
    if (parsed.route !== 'global-settings' && !(parsed.route === 'settings' && parsed.settingsSubRoute === 'customize')) {
      this.selectedSettingsSectionId = '';
    }
    this.setConnectedMode();
    if (this.route !== prevRoute || this.steamId !== prevSteam) {
      this.loadPageData(true);
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
    }
    this.apiError = false;
    this.setConnectedMode();
    if (!this.status.isConnected && wasConnected && this.isGameRoute) {
      this.players = [];
      this.leaderboard = null;
      this.playerStats = null;
      this.settingsSave = null;
      this.minimapRaw = null;
      this.minimap = null;
      this.localPlayerWasAlive = null;
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
    if (!this.minimapRaw) return;
    const raw = this.minimapRaw;
    const layout = raw.layout;
    let activeAreaId = this.minimapAreaId;
    const markers = raw.markers || [];

    const focusId = this.minimapFocusSteamId;
    const followLocal =
      !focusId && this.getLocalPlayer()?.isAlive && this.playerBlindMode;
    const focusMarker = focusId
      ? markers.find((m) => String(m.steamId) === focusId)
      : followLocal
        ? markers.find((m) => m.isLocal)
        : markers.find((m) => m.isLocal) ?? markers[0];

    if (focusMarker?.areaId) activeAreaId = focusMarker.areaId;
    else if (!activeAreaId) activeAreaId = layout.defaultAreaId;

    const area = layout.areas?.find((a) => a.id === activeAreaId) ?? layout.areas?.[0];
    const tiles = area?.tiles?.length ? area.tiles : layout.tiles || [];
    const connectionPoints = area?.connectionPoints?.length
      ? area.connectionPoints
      : [];

    this.minimap = {
      ...raw,
      activeAreaId: area?.id || activeAreaId,
      markers,
      layout: { ...layout, tiles },
      blindMode: this.playerBlindMode,
    };
    this.minimap.layout = { ...this.minimap.layout, tiles };
    (this.minimap as MinimapPayload & { connectionPoints?: unknown }).connectionPoints =
      connectionPoints;

    if (force || activeAreaId !== this.minimapAreaId) {
      this.minimapAreaId = activeAreaId;
      localStorage.setItem('minimapAreaId', activeAreaId);
    }
  }

  async loadPageData(force = false) {
    const onGlobal = this.route === 'global-settings';
    const onSaveCustomize =
      this.route === 'settings' && this.settingsSubRoute === 'customize' && this.status.isHost;
    const onSaveProfile =
      this.route === 'settings' && this.settingsSubRoute !== 'customize' && this.status.isHost;

    if (!this.status.isConnected && !OFFLINE_ROUTES.includes(this.route)) {
      this.lastRoute = this.route;
      this.lastSteamId = this.steamId;
      this.lastSnapshotVersion = this.status.snapshotVersion;
      return;
    }

    if (!force && this.savingSettingKey) return;
    if (!force && this.route === this.lastRoute && this.route !== 'player') {
      if (this.route !== 'global-settings' && !(this.route === 'settings' && this.settingsSubRoute === 'customize')) {
        if (this.route !== 'player') return;
      }
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

      if (onGlobal || onSaveCustomize) {
        await Promise.all([this.loadItemCatalog(), this.loadDungeonCatalog()]);
      }
      if (onGlobal) {
        this.loadingSettings = true;
        try {
          this.settingsGlobal = await Api.getGlobalSettings();
        } finally {
          this.loadingSettings = false;
        }
      } else if (this.route !== 'global-settings') {
        this.settingsGlobal = null;
      }
      if (onSaveCustomize) {
        this.loadingSettings = true;
        try {
          this.settingsSave = await Api.getSaveSettings();
        } finally {
          this.loadingSettings = false;
        }
      } else if (this.route !== 'settings' || this.settingsSubRoute !== 'customize') {
        this.settingsSave = null;
      }
      if (onSaveProfile) await this.loadSaveProfileData(force);
      if ((this.route === 'minimap' || this.route === 'player') && this.minimapRaw) {
        this.applyMinimapFilter(force);
      }
      if (this.status.isHost && this.route === 'players') await this.loadItemCatalog();
    } catch (e) {
      this.pageError = e instanceof Error ? e.message : String(e);
    }
    this.lastRoute = this.route;
    this.lastSteamId = this.steamId;
    this.lastSnapshotVersion = this.status.snapshotVersion;
  }

  async loadItemCatalog() {
    try {
      const res = await Api.getItems();
      this.itemCatalog = res.items || [];
      for (const key of Object.keys(this.spawnSelections)) {
        const sel = this.spawnSelections[key];
        if (!sel.itemId && this.itemCatalog.length > 0) {
          sel.itemId = this.itemCatalog[0].id;
        }
      }
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
    this.loadingSaveProfile = true;
    try {
      const [profile, presets] = await Promise.all([
        Api.getSaveProfile(),
        Api.getQuickPresets(),
      ]);
      this.saveProfile = profile as typeof this.saveProfile;
      this.quickPresets = presets.presets || [];
    } finally {
      this.loadingSaveProfile = false;
    }
  }
}

export const dashboard = new DashboardStore();
