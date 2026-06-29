const GRADE_LABELS = ['Broken', 'Terrible', 'Slow', 'Medium', 'Fine'];

function formatDuration(seconds) {
  if (!seconds) return '0m';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return h > 0 ? h + 'h ' + m + 'm' : m + 'm';
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

function csStatsUrl(steamId) {
  return isValidSteamId(steamId)
    ? 'https://csstats.gg/player/' + encodeURIComponent(String(steamId))
    : '#';
}

function avatarUrl(steamId) {
  if (!isValidSteamId(steamId)) {
    return '/img/default-avatar.svg';
  }
  let url = '/api/players/' + encodeURIComponent(String(steamId)) + '/avatar';
  return url;
}

document.addEventListener('alpine:init', () => {
  Alpine.data('dashboard', () => ({
    status: {
      isConnected: false,
      isHost: false,
      saveSlotId: -1,
      modVersion: '',
      snapshotVersion: 0,
      configVersion: 0,
    },
    players: [],
    leaderboard: null,
    playerStats: null,
    settings: null,
    settingsQuery: '',
    route: 'waiting',
    steamId: null,
    toastMessage: '',
    toastVisible: false,
    loadingStats: false,
    loadingSettings: false,
    pageError: '',
    apiError: false,
    lastSnapshotVersion: -1,
    lastConfigVersion: -1,
    savingSettingKey: '',
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

    get subtitle() {
      if (this.apiError) {
        return 'Cannot reach dashboard API';
      }
      if (!this.status.isConnected) {
        return this.status.modVersion
          ? 'v' + this.status.modVersion + ' · Waiting for game'
          : 'Waiting for game';
      }
      const parts = [];
      if (this.status.modVersion) parts.push('v' + this.status.modVersion);
      parts.push(this.status.isHost ? 'Host' : 'Client');
      if (this.status.saveSlotId >= 0) parts.push('Savegame ' + this.status.saveSlotId);
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
        ['Currency', c.currencyEarned ?? 0],
        ['Mimic encounters', c.mimicEncounterCount ?? 0],
        ['Items carried', c.itemCarryCount ?? 0],
        ['Kills', c.kills ?? 0],
        ['Deaths', c.deaths ?? 0],
        ['Revives', c.revives ?? 0],
        ['Voice events', c.voiceEvents ?? 0],
        ['Ally damage', c.damageToAlly ?? 0],
        ['Connected time', formatDuration(c.totalConnectedSeconds ?? 0)],
        ['Sessions', this.playerStats.global.sessionsCompleted ?? 0],
      ];
    },

    get sessionStatCards() {
      const cs = this.playerStats && this.playerStats.currentSession;
      if (!cs || !cs.counters) return [];
      const s = cs.counters;
      return [
        ['Currency', s.currencyEarned ?? 0],
        ['Kills', s.kills ?? 0],
        ['Deaths', s.deaths ?? 0],
        ['Revives', s.revives ?? 0],
      ];
    },

    init() {
      this.minimapFocusSteamId = localStorage.getItem('minimapFocusSteamId') || '';
      this.minimapAreaId = localStorage.getItem('minimapAreaId') || '';
      window.addEventListener('hashchange', () => this.onHashChange());
      this.parseRoute();
      this.eventSource = Sse.connect(
        (payload) => {
          this.applySnapshot(payload);
          this.loadPageData(false);
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
      if (!this.status.isConnected && this.route !== 'waiting') {
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
      document.body.classList.toggle('waiting', !this.status.isConnected);
      document.body.classList.toggle('connected', this.status.isConnected);
    },

    onHashChange() {
      const prevRoute = this.lastRoute;
      const prevSteam = this.lastSteamId;
      this.parseRoute();
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

    applySnapshot(payload) {
      const wasConnected = this.status.isConnected;
      this.status = payload.status || this.status;
      this.players = payload.players || [];
      this.leaderboard = payload.leaderboard || null;
      this.minimapRaw = payload.minimap || null;
      this.apiError = false;
      this.setConnectedMode();

      if (!this.status.isConnected) {
        this.players = [];
        this.leaderboard = null;
        this.playerStats = null;
        this.settings = null;
        this.minimapRaw = null;
        this.minimap = { markers: [], tiles: [], connectionPoints: [], displayMode: 'hidden' };
      } else if (this.route === 'minimap' || this.route === 'player') {
        this.applyMinimapFilter();
      }

      this.ensureDefaultRoute();
      return wasConnected !== this.status.isConnected;
    },

    needsPageRefresh(force) {
      if (force) return true;
      if (this.route !== this.lastRoute) return true;
      if (this.route === 'player' && this.steamId !== this.lastSteamId) return true;
      if (this.route === 'settings') {
        return this.status.configVersion !== this.lastConfigVersion;
      }
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
      if (!this.status.isConnected) {
        this.pageError = '';
        this.lastRoute = this.route;
        this.lastSteamId = this.steamId;
        this.lastSnapshotVersion = this.status.snapshotVersion;
        this.lastConfigVersion = this.settings?.configVersion ?? this.status.configVersion;
        return;
      }

      if (!this.needsPageRefresh(force)) return;

      const preserveScroll = !force;
      const scrollY = preserveScroll ? window.scrollY : 0;

      this.pageError = '';
      try {
        if (this.route === 'player' && this.steamId && this.status.isHost) {
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

        if (this.route === 'settings' && this.status.isHost) {
          const initialLoad = this.settings === null;
          if (initialLoad) this.loadingSettings = true;
          try {
            this.settings = await Api.getSettings();
          } finally {
            if (initialLoad) this.loadingSettings = false;
          }
        } else if (this.route !== 'settings') {
          this.settings = null;
          this.settingsQuery = '';
        }

        if ((this.route === 'minimap' || this.route === 'player') && this.minimapRaw) {
          this.applyMinimapFilter(force);
        }
      } catch (e) {
        this.pageError = e.message || 'Failed to load data';
      }

      this.lastRoute = this.route;
      this.lastSteamId = this.steamId;
      this.lastSnapshotVersion = this.status.snapshotVersion;
      this.lastConfigVersion = this.settings?.configVersion ?? this.status.configVersion;
      this.restoreScroll(scrollY);
    },

    resolveDisplayName(steamId) {
      const id = String(steamId);
      if (this.playerStats && this.playerStats.displayName) {
        const name = String(this.playerStats.displayName).trim();
        if (name && name !== id) return name;
      }
      const entry = (this.leaderboard && this.leaderboard.entries || []).find(
        (e) => String(e.steamId) === id
      );
      if (entry && entry.displayName && String(entry.displayName) !== id) {
        return entry.displayName;
      }
      const player = this.players.find((p) => String(p.steamId) === id);
      if (player && player.displayName && String(player.displayName) !== id) {
        return player.displayName;
      }
      return id;
    },

    pingLabel(p) {
      if (p.isHost || (this.status.isHost && p.isLocal)) return 'Host';
      if (p.networkGrade == null || p.networkGrade < 0) return 'Unknown';
      const level = Math.max(0, Math.min(4, p.networkGrade));
      return GRADE_LABELS[level];
    },

    pingClass(p) {
      if (p.isHost || (this.status.isHost && p.isLocal)) return '';
      if (p.networkGrade == null || p.networkGrade < 0) return '';
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

    pingBarHeight(i) {
      return { height: 4 + i * 3 + 'px' };
    },

    connectionMeta(p) {
      const parts = [];
      if (p.playerUid) parts.push('#' + p.playerUid);
      if (p.connectionRole) parts.push(p.connectionRole);
      if (p.connectionAddress) parts.push(p.connectionAddress);
      parts.push(p.voiceEventCount + ' voice events');
      return parts.join(' · ');
    },

    sessionLine(p) {
      const s = p.currentSession;
      if (!s) return '';
      const parts = [];
      if (s.currencyEarned) parts.push(s.currencyEarned + ' currency');
      parts.push((s.kills ?? 0) + '/' + (s.deaths ?? 0) + '/' + (s.revives ?? 0) + ' K/D/R');
      if (s.totalConnectedSeconds) parts.push(formatDuration(s.totalConnectedSeconds));
      if (s.mimicEncounterCount) parts.push(s.mimicEncounterCount + ' mimics');
      if (s.itemCarryCount) parts.push(s.itemCarryCount + ' items');
      if (s.voiceEvents) parts.push(s.voiceEvents + ' voice');
      if (s.damageToAlly) parts.push(s.damageToAlly + ' ally dmg');
      return parts.join(' · ');
    },

    canModerate(p) {
      return this.status.isHost && !p.isLocal;
    },

    canRespawn(p) {
      return this.status.isHost && !p.isAlive && p.playerUid;
    },

    async moderate(steamId, action) {
      if (!confirm('Confirm ' + action + ' for player ' + steamId + '?')) return;
      try {
        const res = await Api.postAction(steamId, action);
        this.showToast(res.message || 'Done');
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

    avatarUrl(steamId) {
      return avatarUrl(steamId);
    },

    steamProfileUrl(steamId) {
      return steamProfileUrl(steamId);
    },

    csStatsUrl(steamId) {
      return csStatsUrl(steamId);
    },

    isValidSteamId(steamId) {
      return isValidSteamId(steamId);
    },

    filteredSections() {
      if (!this.settings || !this.settings.sections) return [];
      const q = this.settingsQuery.trim().toLowerCase();
      const sections = [];
      for (const section of this.settings.sections) {
        const entries = (section.entries || []).filter((entry) => {
          if (!q) return true;
          return [entry.key, entry.title, entry.description, entry.value, entry.type, entry.defaultValue]
            .some((v) => String(v || '').toLowerCase().includes(q));
        });
        if (entries.length > 0) {
          sections.push({ id: section.id, title: section.title, entries });
        }
      }
      return sections;
    },

    formatSettingValue(entry) {
      const value = entry.value;
      if (entry.type === 'Boolean') {
        if (value === 'True' || value === 'true') return 'On';
        if (value === 'False' || value === 'false') return 'Off';
      }
      if (value == null || value === '') return '—';
      return String(value);
    },

    settingValueClass(entry) {
      if (entry.type !== 'Boolean') return '';
      const value = String(entry.value || '').toLowerCase();
      return value === 'true' ? 'settings-bool-on' : 'settings-bool-off';
    },

    settingDiffersFromDefault(entry) {
      return String(entry.value ?? '') !== String(entry.defaultValue ?? '');
    },

    settingInputId(sectionId, entry) {
      return sectionId + '--' + entry.key;
    },

    isSavingSetting(sectionId, entry) {
      return this.savingSettingKey === sectionId + '/' + entry.key;
    },

    settingDraftValue(entry) {
      const value = entry.value;
      if (entry.type === 'Boolean') {
        return value === 'True' || value === 'true' ? 'true' : 'false';
      }
      return value == null ? '' : String(value);
    },

    async saveSetting(sectionId, entry, rawValue) {
      const saveKey = sectionId + '/' + entry.key;
      if (this.savingSettingKey === saveKey) return;

      const previousValue = entry.value;
      this.savingSettingKey = saveKey;
      try {
        const res = await Api.updateSetting(sectionId, entry.key, String(rawValue));
        entry.value = res.value ?? String(rawValue);
        if (this.settings) {
          this.settings.configVersion = this.status.configVersion;
        }
        this.lastConfigVersion = this.status.configVersion;
        this.showToast(res.message || 'Saved');
      } catch (e) {
        entry.value = previousValue;
        this.showToast(e.message || 'Failed to save setting');
        await this.loadPageData(true);
      } finally {
        if (this.savingSettingKey === saveKey) {
          this.savingSettingKey = '';
        }
      }
    },

    onBooleanSettingChange(sectionId, entry, event) {
      this.saveSetting(sectionId, entry, event.target.value);
    },

    onTextSettingCommit(sectionId, entry, event) {
      const nextValue = event.target.value;
      if (String(nextValue) === this.settingDraftValue(entry)) {
        return;
      }
      this.saveSetting(sectionId, entry, nextValue);
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
      const areaMarkers = allFiltered.filter((marker) => {
        if (!activeAreaId) return true;
        if (!marker.areaId) {
          return this.minimapRaw.displayMode === 'markers-only';
        }
        return marker.areaId === activeAreaId;
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
      if (!marker.isAlive) parts.push('dead');
      return parts.join(' · ');
    },
  }));
});
