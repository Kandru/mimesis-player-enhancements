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

  async getPlayerStats(steamId) {
    return Api.fetchJson('/api/players/' + encodeURIComponent(steamId) + '/stats');
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
