const Api = {
  async fetchJson<T>(path: string, options?: RequestInit): Promise<T> {
    const res = await fetch(path, { cache: 'no-store', ...options });
    if (!res.ok) {
      let message = `${path} ${res.status}`;
      try {
        const body = await res.json();
        if (body.message) message = body.message;
      } catch {
        /* ignore */
      }
      throw new Error(message);
    }
    return res.json();
  },

  getStatus() {
    return Api.fetchJson<import('./types').StatusDto>('/api/status');
  },

  acknowledgeChangelog() {
    return Api.fetchJson<import('./types').ChangelogAcknowledgeResult>('/api/changelog/acknowledge', {
      method: 'POST',
    });
  },

  disableAllPlayerCheats() {
    return Api.fetchJson('/api/host-cheats', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ disableAll: true }),
    });
  },

  getGlobalSettings() {
    return Api.fetchJson<import('./types').SettingsDto>('/api/settings/global');
  },

  updateGlobalSetting(sectionId: string, key: string, value: string) {
    return Api.fetchJson('/api/settings/global', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key, value }),
    });
  },

  resetGlobalSetting(sectionId: string, key?: string) {
    return Api.fetchJson<{ resetCount?: number; message?: string }>('/api/settings/global/reset', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key: key ?? '' }),
    });
  },

  getSaveSettings() {
    return Api.fetchJson<import('./types').SettingsDto>('/api/settings/save');
  },

  updateSaveSetting(sectionId: string, key: string, value: string) {
    return Api.fetchJson('/api/settings/save', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key, value }),
    });
  },

  resetSaveSetting(sectionId: string, key?: string) {
    return Api.fetchJson<{ resetCount?: number; message?: string }>('/api/settings/save/reset', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sectionId, key: key ?? '' }),
    });
  },

  getSaveProfile() {
    return Api.fetchJson('/api/settings/save/profile');
  },

  updateSaveProfile(body: { mode: string; presetId?: string }) {
    return Api.fetchJson('/api/settings/save/profile', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  getQuickPresets() {
    return Api.fetchJson<{ presets: import('./types').QuickPresetDto[] }>('/api/quick-presets');
  },

  saveQuickPreset(body: Record<string, unknown>) {
    return Api.fetchJson('/api/quick-presets', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  deleteQuickPreset(presetId: string) {
    return Api.fetchJson(`/api/quick-presets/${encodeURIComponent(presetId)}`, { method: 'DELETE' });
  },

  exportQuickPreset(presetId: string) {
    return Api.fetchJson<{ shareString: string; name: string }>(
      `/api/quick-presets/${encodeURIComponent(presetId)}/export`,
    );
  },

  importQuickPreset(body: Record<string, unknown>) {
    return Api.fetchJson('/api/quick-presets/import', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  getPlayerStats(steamId: string) {
    return Api.fetchJson<import('./types').PlayerStatsDto>(
      `/api/players/${encodeURIComponent(steamId)}/stats`,
    );
  },

  async postAction(steamId: string, action: string) {
    const res = await fetch(`/api/players/${encodeURIComponent(steamId)}/${action}`, {
      method: 'POST',
      cache: 'no-store',
    });
    const body = (await res.json().catch(() => ({}))) as {
      message?: string;
      godMode?: boolean;
      noClip?: boolean;
    };
    if (!res.ok && res.status !== 202) {
      throw new Error(body.message || `${action} ${res.status}`);
    }
    return body;
  },

  getItems() {
    return Api.fetchJson<{ items: import('./types').ItemOptionDto[] }>('/api/items');
  },

  getDungeons() {
    return Api.fetchJson<{ dungeons: Array<{ id: string; label: string }> }>('/api/dungeons');
  },

  deletePlayer(steamId: string) {
    return Api.fetchJson<{ message?: string }>(`/api/players/${encodeURIComponent(steamId)}/delete`, { method: 'POST' });
  },

  spawnItem(steamId: string, itemId: string, percent?: number) {
    const body: Record<string, unknown> = { itemId };
    if (percent != null) body.percent = percent;
    return Api.fetchJson(`/api/players/${encodeURIComponent(steamId)}/spawn-item`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  },

  setBlindMode(enabled: boolean) {
    return Api.fetchJson('/api/minimap/blind', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ enabled }),
    });
  },
};

export default Api;
