import type { SettingsDto } from './types';

const GLOBAL_SETTINGS_KEY = 'mpe.dashboard.globalSettings.v2';

export function readCachedGlobalSettings(): SettingsDto | null {
  try {
    const raw = sessionStorage.getItem(GLOBAL_SETTINGS_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as SettingsDto;
    if (!parsed?.sections?.length) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function writeCachedGlobalSettings(settings: SettingsDto) {
  try {
    sessionStorage.setItem(GLOBAL_SETTINGS_KEY, JSON.stringify(settings));
  } catch {
    /* storage full or unavailable */
  }
}
