const Api = {
  async fetchJson(path, options) {
    const res = await fetch(path, Object.assign({ cache: 'no-store' }, options || {}));
    if (!res.ok) {
      let message = path + ' ' + res.status;
      try {
        const body = await res.json();
        if (body.message) message = body.message;
      } catch (_) {
        /* ignore */
      }
      throw new Error(message);
    }
    return res.json();
  },

  async getStatus() {
    return Api.fetchJson('/api/status');
  },

  async getPlayers() {
    return Api.fetchJson('/api/players');
  },

  async getLeaderboard() {
    return Api.fetchJson('/api/leaderboard');
  },

  async getSettings() {
    return Api.fetchJson('/api/settings');
  },

  async updateSetting(sectionId, key, value) {
    return Api.fetchJson('/api/settings', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key, value }),
    });
  },

  async getPlayerStats(steamId) {
    return Api.fetchJson('/api/players/' + encodeURIComponent(steamId) + '/stats');
  },

  async getMinimap(options) {
    const params = new URLSearchParams();
    if (options && options.focusSteamId) {
      params.set('focusSteamId', String(options.focusSteamId));
    }
    if (options && options.showAll) {
      params.set('showAll', 'true');
    }
    const query = params.toString();
    return Api.fetchJson('/api/minimap' + (query ? '?' + query : ''));
  },

  async postAction(steamId, action) {
    const res = await fetch('/api/players/' + encodeURIComponent(steamId) + '/' + action, {
      method: 'POST',
      cache: 'no-store',
    });
    const body = await res.json().catch(() => ({}));
    if (!res.ok && res.status !== 202) {
      throw new Error(body.message || action + ' ' + res.status);
    }
    return body;
  },
};
