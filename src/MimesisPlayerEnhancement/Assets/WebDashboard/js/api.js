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

  async getSaveProfile() {
    return Api.fetchJson('/api/settings/save/profile');
  },

  async updateSaveProfile(body) {
    return Api.fetchJson('/api/settings/save/profile', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  async getQuickPresets() {
    return Api.fetchJson('/api/quick-presets');
  },

  async saveQuickPreset(body) {
    return Api.fetchJson('/api/quick-presets', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  async deleteQuickPreset(presetId) {
    return Api.fetchJson('/api/quick-presets/' + encodeURIComponent(presetId), {
      method: 'DELETE',
    });
  },

  async exportQuickPreset(presetId) {
    return Api.fetchJson('/api/quick-presets/' + encodeURIComponent(presetId) + '/export');
  },

  async importQuickPreset(body) {
    return Api.fetchJson('/api/quick-presets/import', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
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

  async getItems() {
    return Api.fetchJson('/api/items');
  },

  async getDungeons() {
    return Api.fetchJson('/api/dungeons');
  },

  async deletePlayer(steamId) {
    return Api.fetchJson('/api/players/' + encodeURIComponent(steamId) + '/delete', {
      method: 'POST',
    });
  },

  async spawnItem(steamId, itemId, percent) {
    const body = { itemId };
    if (percent != null) body.percent = percent;
    return Api.fetchJson('/api/players/' + encodeURIComponent(steamId) + '/spawn-item', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },
};
