export const DEV_STEAM_ID = '76561198019045610';

export function isDevSteamId(steamId: string | number | null | undefined): boolean {
  return String(steamId ?? '') === DEV_STEAM_ID;
}
