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

  async getGlobalSettings() {
    return Api.fetchJson('/api/settings/global');
  },

  async updateGlobalSetting(sectionId, key, value) {
    return Api.fetchJson('/api/settings/global', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key, value }),
    });
  },

  async getSaveSettings() {
    return Api.fetchJson('/api/settings/save');
  },

  async updateSaveSetting(sectionId, key, value) {
    return Api.fetchJson('/api/settings/save', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key, value }),
    });
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
