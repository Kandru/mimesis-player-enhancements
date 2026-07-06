function t(key, params) {
  return window.DashboardI18n ? window.DashboardI18n.t(key, params) : key;
}

function gradeLabels() {
  return [
    t('dashboard.grade_broken'),
    t('dashboard.grade_terrible'),
    t('dashboard.grade_slow'),
    t('dashboard.grade_medium'),
    t('dashboard.grade_fine'),
  ];
}

function formatDuration(seconds) {
  if (!seconds) return '0m';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return h > 0 ? h + 'h ' + m + 'm' : m + 'm';
}

function formatCountMap(map, labelKey) {
  if (!map || typeof map !== 'object') return [];
  return Object.entries(map)
    .filter(([, count]) => (count ?? 0) > 0)
    .sort((a, b) => (b[1] ?? 0) - (a[1] ?? 0))
    .map(([key, count]) => {
      const label = labelKey ? t(labelKey, { name: key }) : key;
      return [label, count ?? 0];
    });
}

function isValidSteamId(steamId) {
  if (steamId == null || steamId === '') return false;
  const id = String(steamId);
  return id !== 'null' && id !== 'undefined' && id !== '0';
}

function steamProfileUrl(steamId) {
  return isValidSteamId(steamId)
    ? 'https://steamcommunity.com/profiles/' + encodeURIComponent(String(steamId))
    : '#';
}

function avatarUrl(steamId) {
  if (!isValidSteamId(steamId)) {
    return '/img/default-avatar.svg';
  }
  let url = '/api/players/' + encodeURIComponent(String(steamId)) + '/avatar';
  return url;
}

function formatVitalPercent(value) {
  if (value == null) return '?';
  const n = Number(value);
  if (!Number.isFinite(n)) return '?';
  return n.toFixed(2).replace(/\.?0+$/, '') + '%';
}

const OFFLINE_ROUTES = ['donation', 'global-settings'];

function isOfflineRoute(route) {
  return OFFLINE_ROUTES.includes(route);
}

document.addEventListener('alpine:init', () => {
  Alpine.data('dashboard', () => applySettingsMixin({
    status: {
      isConnected: false,
      isHost: false,
      saveSlotId: -1,
      lobbyName: '',
      modVersion: '',
      snapshotVersion: 0,
      configVersion: 0,
      joinAnytimeRoutingCount: 0,
      locale: 'en',
    },
    players: [],
    leaderboard: null,
    playerStats: null,
    route: 'waiting',
    steamId: null,
    toastMessage: '',
    toastVisible: false,
    loadingStats: false,
    loadingSettings: false,
    pageError: '',
    apiError: false,
    lastSnapshotVersion: -1,
    lastRoute: '',
    lastSteamId: null,
    eventSource: null,
    toastTimer: null,
    minimap: { markers: [], tiles: [], connectionPoints: [] },
    minimapRaw: null,
    minimapShowAll: false,
    minimapFocusSteamId: '',
    minimapAreaId: '',
    minimapLastLayoutVersion: -1,
    minimapLastActiveAreaId: '',
    playerBlindMode: true,
    playerBlindModeUserEnabled: true,
    playerBlindModeAutoSuspended: false,
    _localPlayerWasAlive: null,
    localeVersion: 0,
    itemCatalog: [],
    itemCatalogLocale: '',
    spawnSelections: {},

    t(key, params) {
      void this.localeVersion;
      return DashboardI18n.t(key, params);
    },

    async reloadLocale(lang) {
      await DashboardI18n.loadLocale(lang || 'en');
      this.localeVersion++;
    },

    get isGameRoute() {
      return ['players', 'minimap', 'leaderboard', 'settings', 'player'].includes(this.route);
    },

    isOfflineRoute() {
      return isOfflineRoute(this.route);
    },

    get pageTitle() {
      const name = (this.status.lobbyName || '').trim();
      return name || t('dashboard.title_default');
    },

    get subtitle() {
      if (this.apiError) {
        return t('dashboard.subtitle_api_error');
      }
      if (!this.status.isConnected) {
        return this.status.modVersion
          ? 'v' + this.status.modVersion + ' · ' + t('dashboard.subtitle_waiting')
          : t('dashboard.subtitle_waiting');
      }
      const parts = [];
      if (this.status.modVersion) parts.push('v' + this.status.modVersion);
      parts.push(this.status.isHost ? t('dashboard.subtitle_host') : t('dashboard.subtitle_client'));
      if (this.status.saveSlotId >= 0) {
        parts.push(t('dashboard.subtitle_savegame', { slot: this.status.saveSlotId }));
      }
      return parts.join(' · ');
    },

    get connectedSet() {
      const ids = (this.leaderboard && this.leaderboard.connectedSteamIds) || [];
      return new Set(ids.map(String));
    },

    get globalStatCards() {
      if (!this.playerStats || !this.playerStats.global) return [];
      const c = (this.playerStats.global.counters || {});
      return [
        [t('dashboard.stat_currency'), c.currencyEarned ?? 0],
        [t('dashboard.stat_mimic_encounters'), c.mimicEncounterCount ?? 0],
        [t('dashboard.stat_items_carried'), c.itemCarryCount ?? 0],
        [t('dashboard.stat_survival_wins'), c.survivalWins ?? 0],
        [t('dashboard.stat_left_behind'), c.survivalLeftBehind ?? 0],
        [t('dashboard.stat_survival_deaths'), c.survivalDeaths ?? 0],
        [t('dashboard.stat_deathmatch_wins'), c.deathmatchWins ?? 0],
        [t('dashboard.stat_deathmatch_deaths'), c.deathmatchDeaths ?? 0],
        [t('dashboard.stat_revives'), c.revives ?? 0],
        [t('dashboard.stat_voices_recorded'), c.voiceEvents ?? 0],
        [t('dashboard.stat_friend_damage'), c.damageToFriend ?? 0],
        [t('dashboard.stat_friends_killed'), c.friendsKilled ?? 0],
        [t('dashboard.stat_connected_time'), formatDuration(c.totalConnectedSeconds ?? 0)],
        [t('dashboard.stat_sessions'), this.playerStats.global.sessionsCompleted ?? 0],
        ...formatCountMap(c.monsterKills, ''),
        ...formatCountMap(c.deathsByMonster, 'dashboard.killed_by'),
        ...formatCountMap(c.deathsByTrap, 'dashboard.killed_by'),
      ];
    },

    get sessionStatCards() {
      const cs = this.playerStats && this.playerStats.currentSession;
      if (!cs || !cs.counters) return [];
      const s = cs.counters;
      return [
        [t('dashboard.stat_currency'), s.currencyEarned ?? 0],
        [t('dashboard.stat_survival_deaths'), s.survivalDeaths ?? 0],
        [t('dashboard.stat_survival_wins'), s.survivalWins ?? 0],
        [t('dashboard.stat_left_behind'), s.survivalLeftBehind ?? 0],
        [t('dashboard.stat_deathmatch_wins'), s.deathmatchWins ?? 0],
        [t('dashboard.stat_deathmatch_deaths'), s.deathmatchDeaths ?? 0],
        [t('dashboard.stat_revives'), s.revives ?? 0],
        [t('dashboard.stat_friend_damage'), s.damageToFriend ?? 0],
        [t('dashboard.stat_friends_killed'), s.friendsKilled ?? 0],
        ...formatCountMap(s.monsterKills, ''),
        ...formatCountMap(s.deathsByMonster, 'dashboard.killed_by'),
        ...formatCountMap(s.deathsByTrap, 'dashboard.killed_by'),
      ];
    },

    async init() {
      this.minimapFocusSteamId = localStorage.getItem('minimapFocusSteamId') || '';
      this.minimapAreaId = localStorage.getItem('minimapAreaId') || '';
      window.addEventListener('hashchange', () => this.onHashChange());
      this.parseRoute();
      this.setConnectedMode();
      try {
        const status = await Api.getStatus();
        await this.reloadLocale(status.locale || 'en');
        this.applySnapshot({ status, players: [], leaderboard: null });
      } catch {
        await this.reloadLocale('en');
      }
      this.syncDocumentTitle();
      if (this.route === 'global-settings') {
        this.loadPageData(true);
      }
      this.eventSource = Sse.connect(
        (payload) => {
          this.applySnapshot(payload);
          if (this.route !== 'global-settings' && this.route !== 'settings') {
            this.loadPageData(false);
          }
        },
        (minimap) => {
          this.applyMinimapLive(minimap);
        },
        () => {
          this.apiError = true;
          this.status.isConnected = false;
          this.setConnectedMode();
        }
      );
    },

    parseRoute() {
      const hash = location.hash || '#/waiting';
      const parts = hash.replace(/^#\/?/, '').split('/').filter(Boolean);
      this.route = parts[0] || 'waiting';
      this.steamId = parts[1] ? String(parts[1]) : null;
    },

    ensureDefaultRoute() {
      if (
        !this.status.isConnected
        && this.route !== 'waiting'
        && !isOfflineRoute(this.route)
      ) {
        location.hash = '#/waiting';
        this.parseRoute();
      } else if (
        this.status.isConnected &&
        (this.route === 'waiting' || !location.hash || location.hash === '#')
      ) {
        location.hash = '#/players';
        this.parseRoute();
      }
    },

    setConnectedMode() {
      const waitingLayout = !this.status.isConnected && !isOfflineRoute(this.route);
      document.body.classList.toggle('waiting', waitingLayout);
      document.body.classList.toggle('connected', this.status.isConnected);
    },

    syncDocumentTitle() {
      const title = this.pageTitle;
      if (document.title !== title) {
        document.title = title;
      }
    },

    onHashChange() {
      const prevRoute = this.lastRoute;
      const prevSteam = this.lastSteamId;
      this.parseRoute();
      this.setConnectedMode();
      const wasOnSettings = prevRoute === 'global-settings' || prevRoute === 'settings';
      const isOnSettings = this.route === 'global-settings' || this.route === 'settings';
      this.clearSettingsOnRouteChange(wasOnSettings, isOnSettings, prevRoute);
      if (this.route !== prevRoute || this.steamId !== prevSteam) {
        this.loadPageData(true);
      }
    },

    showToast(message) {
      this.toastMessage = message;
      this.toastVisible = true;
      if (this.toastTimer) clearTimeout(this.toastTimer);
      this.toastTimer = setTimeout(() => {
        this.toastVisible = false;
      }, 3500);
    },

    applyMinimapLive(minimap) {
      if (!this.status.isConnected || !minimap) {
        return;
      }

      this.minimapRaw = minimap;
      if (this.route === 'minimap' || this.route === 'player') {
        this.applyMinimapFilter(false);
      }
    },

    async applySnapshot(payload) {
      const wasConnected = this.status.isConnected;
      const previousLocale = this.status.locale || 'en';
      this.status = payload.status || this.status;
      if (payload.status?.locale && payload.status.locale !== previousLocale && window.DashboardI18n) {
        await this.reloadLocale(payload.status.locale);
      }
      if (payload.playersLiveOnly) {
        this.mergeLivePlayers(payload.players || []);
      } else {
        this.players = payload.players || [];
        this.leaderboard = payload.leaderboard ?? this.leaderboard;
        if (payload.minimap != null) {
          this.minimapRaw = payload.minimap;
        }
      }
      if (this.status.isConnected) {
        this.syncBlindModeForLocalPlayer();
      }
      this.apiError = false;
      this.setConnectedMode();

      if (!this.status.isConnected && wasConnected && this.isGameRoute) {
        this.players = [];
        this.leaderboard = null;
        this.playerStats = null;
        this.handleSettingsOnDisconnect();
        this.minimapRaw = null;
        this.minimap = { markers: [], tiles: [], connectionPoints: [], displayMode: 'hidden' };
        this._localPlayerWasAlive = null;
      } else if (this.status.isConnected && (this.route === 'minimap' || this.route === 'player')) {
        this.applyMinimapFilter();
      }

      this.ensureDefaultRoute();
      this.syncDocumentTitle();
      this.handleSettingsOnSnapshot();
      return wasConnected !== this.status.isConnected;
    },

    mergeLivePlayers(livePlayers) {
      if (!Array.isArray(livePlayers) || livePlayers.length === 0) {
        return;
      }

      for (const live of livePlayers) {
        const id = String(live.steamId);
        const index = (this.players || []).findIndex((player) => String(player.steamId) === id);
        if (index >= 0) {
          this.players[index] = live;
        } else {
          this.players.push(live);
        }
      }
    },

    needsPageRefresh(force) {
      if (force) return true;
      if (this.savingSettingKey) return false;
      if (this.route === 'global-settings' || this.route === 'settings') return false;
      if (this.route !== this.lastRoute) return true;
      if (this.route === 'player' && this.steamId !== this.lastSteamId) return true;
      if (this.route === 'player' && this.status.isHost) {
        return this.status.snapshotVersion !== this.lastSnapshotVersion;
      }
      return false;
    },

    restoreScroll(scrollY) {
      if (scrollY <= 0) return;
      requestAnimationFrame(() => {
        window.scrollTo(0, scrollY);
      });
    },

    async loadPageData(force) {
      const onGlobalSettings = this.route === 'global-settings';
      const onSaveSettings = this.route === 'settings' && this.status.isHost;

      if (!this.status.isConnected && !isOfflineRoute(this.route)) {
        this.pageError = '';
        this.lastRoute = this.route;
        this.lastSteamId = this.steamId;
        this.lastSnapshotVersion = this.status.snapshotVersion;
        return;
      }

      if (!this.needsPageRefresh(force)) return;

      const preserveScroll = !force;
      const scrollY = preserveScroll ? window.scrollY : 0;

      this.pageError = '';
      try {
        if (this.status.isConnected && this.route === 'player' && this.steamId && this.status.isHost) {
          const initialLoad = this.playerStats === null;
          if (initialLoad) this.loadingStats = true;
          try {
            this.playerStats = await Api.getPlayerStats(this.steamId);
          } finally {
            if (initialLoad) this.loadingStats = false;
          }
        } else if (this.route !== 'player') {
          this.playerStats = null;
        }

        await this.loadSettingsInPageData(onGlobalSettings, onSaveSettings);

        if ((this.route === 'minimap' || this.route === 'player') && this.minimapRaw) {
          this.applyMinimapFilter(force);
        }

        if (this.status.isHost && this.route === 'players') {
          await this.loadItemCatalog();
        }
      } catch (e) {
        this.pageError = e.message || t('dashboard.failed_load');
      }

      this.lastRoute = this.route;
      this.lastSteamId = this.steamId;
      this.lastSnapshotVersion = this.status.snapshotVersion;
      this.restoreScroll(scrollY);
    },

    resolveDisplayName(steamId) {
      const id = String(steamId);
      const usable = (name) => {
        const text = String(name ?? '').trim();
        return text && text !== id ? text : '';
      };
      // Server centralizes name resolution on the players payload; other
      // sources are fallbacks, and the raw Steam ID is the last resort.
      const player = (this.players || []).find((p) => String(p.steamId) === id);
      if (player && usable(player.displayName)) return usable(player.displayName);
      const entry = ((this.leaderboard && this.leaderboard.entries) || []).find(
        (e) => String(e.steamId) === id
      );
      if (entry && usable(entry.displayName)) return usable(entry.displayName);
      if (this.playerStats && String(this.playerStats.steamId) === id) {
        const name = usable(this.playerStats.displayName);
        if (name) return name;
      }
      return id;
    },

    pingLabel(p) {
      if (p.isHost || (this.status.isHost && p.isLocal)) return t('dashboard.subtitle_host');
      if (p.networkGrade == null || p.networkGrade < 0) return t('dashboard.ping_unknown');
      const level = Math.max(0, Math.min(4, p.networkGrade));
      return gradeLabels()[level];
    },

    pingClass(p) {
      if (p.isHost || (this.status.isHost && p.isLocal)) return '';
      if (p.networkGrade == null || p.networkGrade < 0) return 'unknown';
      const level = Math.max(0, Math.min(4, p.networkGrade));
      if (level <= 1) return 'poor';
      if (level <= 2) return 'medium';
      return '';
    },

    pingActive(p) {
      if (p.isHost || (this.status.isHost && p.isLocal)) return 4;
      if (p.networkGrade == null || p.networkGrade < 0) return 0;
      return Math.max(0, Math.min(4, p.networkGrade)) + 1;
    },

    connectionMeta(p) {
      const parts = [];
      if (p.playerUid) parts.push('#' + p.playerUid);
      if (p.connectionRole) parts.push(p.connectionRole);
      if (p.connectionAddress) parts.push(p.connectionAddress);
      parts.push(t('dashboard.voice_lines', { count: p.voiceLineCount }));
      return parts.join(' · ');
    },

    lateJoinMeta(p) {
      if (!this.status.isHost || !p.lateJoinLabel) return '';
      const parts = [t('dashboard.late_join_prefix', { label: p.lateJoinLabel })];
      if (p.lateJoinStuckSeconds != null && p.lateJoinStuckSeconds > 0) {
        parts.push(Math.round(p.lateJoinStuckSeconds) + 's');
      }
      if (p.lateJoinAttemptCount > 1) {
        parts.push(p.lateJoinAttemptCount + ' attempts');
      }
      return parts.join(' · ');
    },

    showLateJoinBadge(p) {
      return this.status.isHost && !!p.lateJoinLabel;
    },

    sessionLine(p) {
      const s = p.currentSession;
      if (!s) return '';
      const parts = [];
      if (s.currencyEarned) parts.push(s.currencyEarned + ' currency');
      parts.push(
        (s.survivalWins ?? 0) + 'W/' +
        (s.survivalDeaths ?? 0) + 'D/' +
        (s.survivalLeftBehind ?? 0) + 'L'
      );
      if (s.deathmatchWins || s.deathmatchDeaths) {
        parts.push((s.deathmatchWins ?? 0) + '/' + (s.deathmatchDeaths ?? 0) + ' DM W/D');
      }
      if (s.revives) parts.push(s.revives + ' revives');
      if (s.totalConnectedSeconds) parts.push(formatDuration(s.totalConnectedSeconds));
      if (s.mimicEncounterCount) parts.push(s.mimicEncounterCount + ' mimics');
      if (s.itemCarryCount) parts.push(s.itemCarryCount + ' items');
      if (s.damageToFriend) parts.push(s.damageToFriend + ' friend dmg');
      if (s.friendsKilled) parts.push(s.friendsKilled + ' friends killed');
      return parts.join(' · ');
    },

    canModerate(p) {
      return this.status.isHost && !p.isLocal;
    },

    showPlayerAliveState(p) {
      if (!p.playerUid) return false;
      return !this.playerBlindMode || p.isLocal;
    },

    isPlayerConnected(p) {
      return !!p.playerUid;
    },

    sortPlayersByName(a, b) {
      return String(a.displayName || '').localeCompare(String(b.displayName || ''), undefined, {
        sensitivity: 'base',
      });
    },

    sortConnectedPlayers(list) {
      list.sort((a, b) => {
        const tier = (p) => (p.isAlive ? 0 : 1);
        const tierCmp = tier(a) - tier(b);
        if (tierCmp !== 0) return tierCmp;
        if (a.isHost !== b.isHost) return a.isHost ? -1 : 1;
        return this.sortPlayersByName(a, b);
      });
      return list;
    },

    overviewPlayers() {
      return this.sortConnectedPlayers([...(this.players || [])]);
    },

    connectedOverviewPlayers() {
      return this.sortConnectedPlayers(
        (this.players || []).filter((p) => this.isPlayerConnected(p)),
      );
    },

    storedOverviewPlayers() {
      const list = (this.players || []).filter((p) => !this.isPlayerConnected(p));
      list.sort((a, b) => this.sortPlayersByName(a, b));
      return list;
    },

    playerRowStateClass(p) {
      if (!this.showPlayerAliveState(p)) return '';
      return p.isAlive ? 'alive' : 'dead';
    },

    playerDetailLines(p) {
      const parts = [];
      const lateJoin = this.lateJoinMeta(p);
      if (lateJoin) parts.push(lateJoin);
      if (this.showPlayerSessionStats(p)) {
        const session = this.sessionLine(p);
        if (session) parts.push(session);
      }
      return {
        connection: this.connectionMeta(p),
        stats: parts.join(' · '),
      };
    },

    vitalsPercent(p) {
      if (!p || p.health == null) {
        return { health: null, toxic: null };
      }
      const healthPercent = p.maxHealth > 0
        ? (Number(p.health) / Number(p.maxHealth)) * 100
        : null;
      const toxic = p.toxicPercent != null ? Number(p.toxicPercent) : null;
      return {
        health: healthPercent,
        toxic: Number.isFinite(toxic) ? toxic : null,
      };
    },

    showPlayerSessionStats(p) {
      return !this.playerBlindMode && p.currentSession;
    },

    showPlayerVitals(p) {
      return this.status.isHost
        && !this.playerBlindMode
        && p.playerUid
        && p.health != null;
    },

    canHeal(p) {
      return this.status.isHost && !this.playerBlindMode && p.playerUid && p.isAlive;
    },

    canGiveItem(p) {
      return this.canHeal(p);
    },

    async loadItemCatalog() {
      if (!this.status.isHost) return;
      const locale = this.status.locale || 'en';
      if (this.itemCatalog.length > 0 && this.itemCatalogLocale === locale) return;
      try {
        const res = await Api.getItems();
        this.itemCatalog = res.items || [];
        this.itemCatalogLocale = locale;
      } catch (_) {
        /* catalog optional — spawn controls stay hidden if empty */
      }
    },

    ensureSpawnSelection(steamId) {
      const key = String(steamId);
      if (!this.spawnSelections[key]) {
        this.spawnSelections[key] = {
          itemId: this.itemCatalog[0]?.id || '',
          percent: 100,
        };
      }
    },

    getSpawnItemId(steamId) {
      this.ensureSpawnSelection(steamId);
      return this.spawnSelections[String(steamId)].itemId;
    },

    setSpawnItemId(steamId, itemId) {
      this.ensureSpawnSelection(steamId);
      const sel = this.spawnSelections[String(steamId)];
      sel.itemId = itemId;
      const option = this.itemCatalog.find((item) => item.id === itemId);
      if (option?.variants?.length) {
        sel.percent = option.variants[0].percent;
      }
    },

    getSelectedItemOption(steamId) {
      const itemId = this.getSpawnItemId(steamId);
      return this.itemCatalog.find((item) => item.id === itemId);
    },

    hasItemVariants(steamId) {
      const option = this.getSelectedItemOption(steamId);
      return !!(option?.variants?.length);
    },

    getItemVariants(steamId) {
      return this.getSelectedItemOption(steamId)?.variants || [];
    },

    getSpawnPercent(steamId) {
      this.ensureSpawnSelection(steamId);
      return this.spawnSelections[String(steamId)].percent;
    },

    setSpawnPercent(steamId, percent) {
      this.ensureSpawnSelection(steamId);
      this.spawnSelections[String(steamId)].percent = parseInt(percent, 10);
    },

    formatSpawnPercent(percent) {
      return t('dashboard.spawn_item_percent_option', { percent });
    },

    async giveItem(p) {
      this.ensureSpawnSelection(p.steamId);
      const sel = this.spawnSelections[String(p.steamId)];
      if (!sel.itemId) return;
      const percent = this.hasItemVariants(p.steamId) ? sel.percent : null;
      try {
        const res = await Api.spawnItem(p.steamId, sel.itemId, percent);
        this.showToast(res.message || t('api.done'));
      } catch (e) {
        this.showToast(e.message);
      }
    },

    getLocalPlayer() {
      return (this.players || []).find((p) => p.isLocal && p.playerUid);
    },

    syncEffectiveBlindMode() {
      const next = this.playerBlindModeUserEnabled && !this.playerBlindModeAutoSuspended;
      const changed = this.playerBlindMode !== next;
      this.playerBlindMode = next;
      return changed;
    },

    syncBlindModeForLocalPlayer() {
      const local = this.getLocalPlayer();
      if (!local) return;

      const isAlive = local.isAlive;
      const wasAlive = this._localPlayerWasAlive;

      if (wasAlive === null) {
        if (!isAlive && this.playerBlindModeUserEnabled) {
          this.playerBlindModeAutoSuspended = true;
        }
        this._localPlayerWasAlive = isAlive;
      } else if (wasAlive && !isAlive) {
        if (this.playerBlindModeUserEnabled) {
          this.playerBlindModeAutoSuspended = true;
        }
        this._localPlayerWasAlive = isAlive;
      } else if (!wasAlive && isAlive) {
        if (this.playerBlindModeUserEnabled && this.playerBlindModeAutoSuspended) {
          this.playerBlindModeAutoSuspended = false;
        }
        this._localPlayerWasAlive = isAlive;
      }

      if (this.syncEffectiveBlindMode()) {
        this.applyMinimapFilter(true);
      }
    },

    togglePlayerBlindMode() {
      this.playerBlindModeUserEnabled = !this.playerBlindModeUserEnabled;
      this.playerBlindModeAutoSuspended = false;
      const local = this.getLocalPlayer();
      if (local && !local.isAlive && this.playerBlindModeUserEnabled) {
        this.playerBlindModeAutoSuspended = true;
      }
      this.syncEffectiveBlindMode();
      this.applyMinimapFilter(true);
    },

    canRespawn(p) {
      return this.status.isHost && !this.playerBlindMode && !p.isAlive && p.playerUid;
    },

    async moderate(steamId, action) {
      if (!confirm(t('dashboard.confirm_moderation', { action, steamId }))) return;
      try {
        const res = await Api.postAction(steamId, action);
        this.showToast(res.message || t('api.done'));
      } catch (e) {
        this.showToast(e.message);
      }
    },

    onAvatarError(event) {
      event.target.src = '/img/default-avatar.svg';
    },

    formatDuration(seconds) {
      return formatDuration(seconds);
    },

    formatVitalPercent(value) {
      return formatVitalPercent(value);
    },

    avatarUrl(steamId) {
      return avatarUrl(steamId);
    },

    steamProfileUrl(steamId) {
      return steamProfileUrl(steamId);
    },

    isValidSteamId(steamId) {
      return isValidSteamId(steamId);
    },

    minimapFocusOptions() {
      return (this.players || []).filter((p) => isValidSteamId(p.steamId));
    },

    resolveMinimapFocus() {
      if (this.route === 'player' && this.steamId) {
        return String(this.steamId);
      }
      if (this.minimapFocusSteamId) {
        return String(this.minimapFocusSteamId);
      }
      const local = (this.players || []).find((p) => p.isLocal);
      if (local && isValidSteamId(local.steamId)) {
        return String(local.steamId);
      }
      const first = (this.players || []).find((p) => isValidSteamId(p.steamId));
      return first ? String(first.steamId) : '';
    },

    onMinimapFocusChange(event) {
      this.minimapFocusSteamId = event.target.value || '';
      localStorage.setItem('minimapFocusSteamId', this.minimapFocusSteamId);
      this.applyMinimapFilter(true);
    },

    onMinimapAreaChange(event) {
      this.minimapAreaId = event.target.value || '';
      localStorage.setItem('minimapAreaId', this.minimapAreaId);
      this.applyMinimapFilter(true);
    },

    minimapAreaOptions() {
      const areas = (this.minimapRaw && this.minimapRaw.areas) || [];
      return areas.filter((area) => area.id);
    },

    isMinimapUserCentric() {
      if (this.route === 'player' && this.steamId) {
        return true;
      }
      if (this.status.isHost && this.minimapShowAll) {
        return false;
      }
      return !!this.resolveMinimapFocus();
    },

    resolveMinimapAreaId(filteredMarkers) {
      const areas = (this.minimapRaw && this.minimapRaw.areas) || [];
      if (!areas.length) {
        return '';
      }

      if (this.isMinimapUserCentric()) {
        const focusSteamId = this.resolveMinimapFocus();
        if (focusSteamId && filteredMarkers && filteredMarkers.length > 0) {
          const focused = filteredMarkers.find((marker) => String(marker.steamId) === focusSteamId);
          if (focused && focused.areaId) {
            return focused.areaId;
          }
        }
      }

      if (this.minimapAreaId && areas.some((area) => area.id === this.minimapAreaId)) {
        return this.minimapAreaId;
      }

      if (this.minimapRaw.defaultAreaId) {
        return this.minimapRaw.defaultAreaId;
      }

      return areas[0].id;
    },

    resolveMinimapArea(areaId) {
      const areas = (this.minimapRaw && this.minimapRaw.areas) || [];
      return areas.find((area) => area.id === areaId) || null;
    },

    minimapShowsMap() {
      if (!this.minimap || this.minimap.displayMode === 'hidden') {
        return false;
      }
      if (this.isMinimapUserCentric()) {
        const focusSteamId = this.resolveMinimapFocus();
        const focused = (this.minimap.markers || []).find(
          (marker) => String(marker.steamId) === focusSteamId
        );
        if (focused && !focused.areaId && this.minimap.layoutKind === 'dungeon') {
          return false;
        }
      }
      return true;
    },

    minimapHasTileLayout() {
      return !!(this.minimap && this.minimap.tiles && this.minimap.tiles.length);
    },

    onMinimapShowAllChange(event) {
      this.minimapShowAll = !!event.target.checked;
      this.applyMinimapFilter(true);
    },

    resolveTrainForArea(rawTrain, activeAreaId) {
      if (!rawTrain || !activeAreaId) {
        return null;
      }
      if (rawTrain.areaId && rawTrain.areaId !== activeAreaId) {
        return null;
      }
      return rawTrain;
    },

    applyMinimapFilter(forceRender) {
      if (!this.minimapRaw) {
        this.minimap = { markers: [], tiles: [], connectionPoints: [], displayMode: 'hidden' };
        return;
      }

      const focusSteamId = this.resolveMinimapFocus();
      const allFiltered = MinimapRenderer.filterMarkers(
        this.minimapRaw.markers || [],
        focusSteamId,
        this.status.isHost && this.minimapShowAll,
        this.status.isHost
      );

      const activeAreaId = this.resolveMinimapAreaId(allFiltered);
      const activeArea = this.resolveMinimapArea(activeAreaId);
      const areaMarkers = allFiltered
        .filter((marker) => {
          if (!activeAreaId) return true;
          if (!marker.areaId) {
            return this.minimapRaw.displayMode === 'markers-only';
          }
          return marker.areaId === activeAreaId;
        })
        .map((marker) => {
          // Prefer a resolved username over a raw Steam ID for marker labels.
          const name = String(marker.displayName || '').trim();
          if (name && name !== String(marker.steamId)) return marker;
          return Object.assign({}, marker, { displayName: this.resolveDisplayName(marker.steamId) });
        });

      const layoutChanged = this.minimapRaw.layoutVersion !== this.minimapLastLayoutVersion;
      const activeAreaChanged = activeAreaId !== this.minimapLastActiveAreaId;
      this.minimap = {
        layoutVersion: this.minimapRaw.layoutVersion,
        layoutKind: this.minimapRaw.layoutKind,
        displayMode: this.minimapRaw.displayMode,
        sceneLabel: this.minimapRaw.sceneLabel,
        activeAreaId,
        activeAreaLabel: activeArea ? activeArea.label : '',
        bounds: activeArea ? activeArea.bounds : this.minimapRaw.bounds,
        tiles: activeArea ? activeArea.tiles || [] : this.minimapRaw.tiles || [],
        connectionPoints: activeArea
          ? activeArea.connectionPoints || []
          : [],
        train: this.resolveTrainForArea(this.minimapRaw.train, activeAreaId),
        markers: areaMarkers,
        blindMode: this.playerBlindMode,
      };
      this.minimapLastLayoutVersion = this.minimapRaw.layoutVersion;
      this.minimapLastActiveAreaId = activeAreaId;
      this.$nextTick(() => this.renderMinimapMaps(forceRender || layoutChanged || activeAreaChanged));
    },

    renderMinimapMaps(forceFullRender) {
      const maps = document.querySelectorAll('[data-minimap-svg]');
      maps.forEach((svg) => {
        const sameLayout =
          svg._minimapLayoutVersion === this.minimap.layoutVersion
          && svg._minimapActiveAreaId === (this.minimap.activeAreaId || '');
        if (!forceFullRender && sameLayout && svg.querySelector('.minimap-map-root')) {
          MinimapRenderer.updateMarkers(svg, this.minimap);
          return;
        }
        MinimapRenderer.render(svg, this.minimap);
      });
    },

    minimapMarkerSummary(marker) {
      if (!marker) return '';
      const parts = [marker.displayName || marker.steamId];
      if (marker.roomName) parts.push(marker.roomName);
      if (marker.areaId && marker.areaId !== this.minimap.activeAreaId) {
        parts.push(marker.areaId);
      }
      if (!this.playerBlindMode && !marker.isAlive) parts.push('dead');
      return parts.join(' · ');
    },
  }));
});
