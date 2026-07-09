export function isValidSteamId(steamId: string | number | null | undefined) {
  if (steamId == null || steamId === '') return false;
  const id = String(steamId);
  return id !== 'null' && id !== 'undefined' && id !== '0';
}

export function avatarUrl(steamId: string | number) {
  if (!isValidSteamId(steamId)) return '/img/default-avatar.svg';
  return `/api/players/${encodeURIComponent(String(steamId))}/avatar`;
}

export function steamProfileUrl(steamId: string | number) {
  return isValidSteamId(steamId)
    ? `https://steamcommunity.com/profiles/${encodeURIComponent(String(steamId))}`
    : '#';
}

export function formatDuration(seconds: number) {
  if (!seconds) return '0m';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
}

export function formatVitalPercent(value: number | null | undefined) {
  if (value == null) return '?';
  const n = Number(value);
  if (!Number.isFinite(n)) return '?';
  return `${n.toFixed(2).replace(/\.?0+$/, '')}%`;
}

export function formatCountMap(
  map: Record<string, number> | undefined,
  labelFn?: (key: string) => string,
) {
  if (!map) return [] as Array<[string, number]>;
  return Object.entries(map)
    .filter(([, count]) => (count ?? 0) > 0)
    .sort((a, b) => (b[1] ?? 0) - (a[1] ?? 0))
    .map(([key, count]) => [labelFn ? labelFn(key) : key, count ?? 0] as [string, number]);
}

export const OFFLINE_ROUTES = ['home', 'donation', 'global-settings'];

export function parseHash(): {
  route: string;
  settingsSubRoute: string;
  steamId: string | null;
} {
  const hash = location.hash || '#/home';
  const parts = hash.replace(/^#\/?/, '').split('/').filter(Boolean);
  let route = parts[0] || 'home';
  if (route === 'waiting') route = 'home';
  let settingsSubRoute = '';
  let steamId: string | null = null;
  if (route === 'settings') settingsSubRoute = parts[1] || '';
  if (route === 'player') steamId = parts[1] ? String(parts[1]) : null;
  return { route, settingsSubRoute, steamId };
}

export function navigate(path: string) {
  location.hash = path.startsWith('#') ? path : `#/${path}`;
}
