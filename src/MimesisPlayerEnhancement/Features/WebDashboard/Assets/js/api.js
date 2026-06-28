const Api = {
  async fetchJson(path, options) {
    const res = await fetch(path, Object.assign({ cache: 'no-store' }, options || {}));
    if (!res.ok) throw new Error(path + ' ' + res.status);
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

  async getPlayerStats(steamId) {
    const res = await fetch('/api/players/' + encodeURIComponent(steamId) + '/stats', {
      cache: 'no-store',
    });
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      throw new Error(body.message || 'stats ' + res.status);
    }
    return res.json();
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
