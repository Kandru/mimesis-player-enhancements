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

function avatarUrl(steamId, cacheVersion) {
  if (!isValidSteamId(steamId)) {
    return '/img/default-avatar.svg';
  }
  let url = '/api/players/' + encodeURIComponent(String(steamId)) + '/avatar';
  if (cacheVersion != null && cacheVersion !== '') {
    url += '?v=' + encodeURIComponent(cacheVersion);
  }
  return url;
}

document.addEventListener('alpine:init', () => {
  Alpine.data('dashboard', () => ({
    status: {
      inSession: false,
      isHost: false,
      saveSlotId: -1,
      modVersion: '',
      snapshotVersion: 0,
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
    lastRoute: '',
    lastSteamId: null,
    pollTimer: null,
    toastTimer: null,

    get subtitle() {
      if (this.apiError) {
        return 'Cannot reach dashboard API';
      }
      if (!this.status.inSession) {
        return this.status.modVersion
          ? 'v' + this.status.modVersion + ' · Waiting for session'
          : 'Waiting for session';
      }
      const parts = [];
      if (this.status.modVersion) parts.push('v' + this.status.modVersion);
      parts.push(this.status.isHost ? 'Host' : 'Guest');
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
      window.addEventListener('hashchange', () => this.onHashChange());
      this.refreshStatus().then(() => {
        this.parseRoute();
        this.ensureDefaultRoute();
        return this.loadPageData(true);
      }).then(() => {
        this.startPolling();
      });
    },

    parseRoute() {
      const hash = location.hash || '#/waiting';
      const parts = hash.replace(/^#\/?/, '').split('/').filter(Boolean);
      this.route = parts[0] || 'waiting';
      this.steamId = parts[1] ? String(parts[1]) : null;
    },

    ensureDefaultRoute() {
      if (!this.status.inSession && this.route !== 'waiting') {
        location.hash = '#/waiting';
        this.parseRoute();
      } else if (
        this.status.inSession &&
        (this.route === 'waiting' || !location.hash || location.hash === '#')
      ) {
        location.hash = '#/players';
        this.parseRoute();
      }
    },

    setSessionMode() {
      document.body.classList.toggle('waiting', !this.status.inSession);
      document.body.classList.toggle('in-session', this.status.inSession);
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

    async refreshStatus() {
      const wasInSession = this.status.inSession;
      try {
        this.status = await Api.getStatus();
        this.apiError = false;
        this.setSessionMode();
        this.ensureDefaultRoute();
        return wasInSession !== this.status.inSession;
      } catch (_) {
        this.apiError = true;
        this.status.inSession = false;
        this.setSessionMode();
        return wasInSession;
      }
    },

    needsPageRefresh(force) {
      if (force) return true;
      if (this.route !== this.lastRoute) return true;
      if (this.route === 'player' && this.steamId !== this.lastSteamId) return true;
      if (this.route === 'settings') return false;
      if (this.status.snapshotVersion !== this.lastSnapshotVersion) return true;
      return false;
    },

    restoreScroll(scrollY) {
      if (scrollY <= 0) return;
      requestAnimationFrame(() => {
        window.scrollTo(0, scrollY);
      });
    },

    async loadPageData(force) {
      if (!this.status.inSession) {
        this.players = [];
        this.leaderboard = null;
        this.playerStats = null;
        this.settings = null;
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
        if (this.route === 'players' || this.route === 'player') {
          const data = await Api.getPlayers();
          this.players = data.players || [];
        }

        if (this.status.isHost && (this.route === 'leaderboard' || this.route === 'player' || this.route === 'players')) {
          this.leaderboard = await Api.getLeaderboard();
        }

        if (this.route === 'player' && this.steamId) {
          const initialLoad = this.playerStats === null;
          if (initialLoad) this.loadingStats = true;
          try {
            this.playerStats = await Api.getPlayerStats(this.steamId);
          } finally {
            if (initialLoad) this.loadingStats = false;
          }
        } else {
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
        } else {
          this.settings = null;
          this.settingsQuery = '';
        }
      } catch (e) {
        this.pageError = e.message || 'Failed to load data';
      }

      this.lastRoute = this.route;
      this.lastSteamId = this.steamId;
      this.lastSnapshotVersion = this.status.snapshotVersion;
      this.restoreScroll(scrollY);
    },

    startPolling() {
      if (this.pollTimer) clearInterval(this.pollTimer);
      this.pollTimer = setInterval(async () => {
        const sessionChanged = await this.refreshStatus();
        await this.loadPageData(sessionChanged);
      }, 2000);
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

    async moderate(steamId, action) {
      if (!confirm('Confirm ' + action + ' for player ' + steamId + '?')) return;
      try {
        const res = await Api.postAction(steamId, action);
        this.showToast(res.message || 'Done');
        await this.loadPageData(true);
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

    avatarUrl(steamId, cacheVersion) {
      return avatarUrl(steamId, cacheVersion);
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
  }));
});
