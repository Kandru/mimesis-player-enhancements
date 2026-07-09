import type { PlayerDto } from './types';
import { formatDuration } from './utils';

export const LOBBY_ROUTES = ['players', 'minimap', 'leaderboard', 'settings', 'player'] as const;

export function isLobbyRoute(route: string) {
  return (LOBBY_ROUTES as readonly string[]).includes(route);
}

export function sortConnectedPlayers(list: PlayerDto[]) {
  return [...list].sort((a, b) => {
    const tier = (p: PlayerDto) => (p.isAlive ? 0 : 1);
    const tierCmp = tier(a) - tier(b);
    if (tierCmp !== 0) return tierCmp;
    if (a.isHost !== b.isHost) return a.isHost ? -1 : 1;
    return String(a.displayName).localeCompare(String(b.displayName), undefined, { sensitivity: 'base' });
  });
}

export function sortPlayersByName(list: PlayerDto[]) {
  return [...list].sort((a, b) =>
    String(a.displayName).localeCompare(String(b.displayName), undefined, { sensitivity: 'base' }),
  );
}

export function connectionMeta(player: PlayerDto, voiceLinesLabel: string) {
  const parts: string[] = [];
  if (player.playerUid) parts.push(`#${player.playerUid}`);
  if (player.connectionRole) parts.push(player.connectionRole);
  if (player.connectionAddress) parts.push(player.connectionAddress);
  parts.push(voiceLinesLabel);
  return parts.join(' · ');
}

export function lateJoinMeta(
  player: PlayerDto,
  isHost: boolean,
  prefix: string,
  attemptsLabel: string,
) {
  if (!isHost || !player.lateJoinLabel) return '';
  const parts = [prefix];
  if (player.lateJoinStuckSeconds != null && player.lateJoinStuckSeconds > 0) {
    parts.push(`${Math.round(player.lateJoinStuckSeconds)}s`);
  }
  if ((player.lateJoinAttemptCount ?? 0) > 1) {
    parts.push(attemptsLabel);
  }
  return parts.join(' · ');
}

export function sessionLine(
  player: PlayerDto,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  const s = player.currentSession;
  if (!s) return '';
  const parts: string[] = [];
  if (s.currencyEarned) parts.push(t('dashboard.session_line_currency', { count: s.currencyEarned }));
  parts.push(
    t('dashboard.session_line_survival', {
      wins: s.survivalWins ?? 0,
      deaths: s.survivalDeaths ?? 0,
      left: s.survivalLeftBehind ?? 0,
    }),
  );
  if (s.deathmatchWins || s.deathmatchDeaths) {
    parts.push(
      t('dashboard.session_line_deathmatch', {
        wins: s.deathmatchWins ?? 0,
        deaths: s.deathmatchDeaths ?? 0,
      }),
    );
  }
  if (s.revives) parts.push(t('dashboard.session_line_revives', { count: s.revives }));
  if (s.totalConnectedSeconds) parts.push(formatDuration(s.totalConnectedSeconds));
  if (s.mimicEncounterCount) parts.push(t('dashboard.session_line_mimics', { count: s.mimicEncounterCount }));
  if (s.itemCarryCount) parts.push(t('dashboard.session_line_items', { count: s.itemCarryCount }));
  if (s.damageToFriend) parts.push(t('dashboard.session_line_friend_damage', { count: s.damageToFriend }));
  if (s.friendsKilled) parts.push(t('dashboard.session_line_friends_killed', { count: s.friendsKilled }));
  return parts.join(' · ');
}

export function vitalsPercent(player: PlayerDto) {
  if (player.health == null) return { health: null, toxic: null };
  const healthPercent =
    player.maxHealth && player.maxHealth > 0
      ? (Number(player.health) / Number(player.maxHealth)) * 100
      : null;
  const toxic = player.toxicPercent != null ? Number(player.toxicPercent) : null;
  return {
    health: healthPercent,
    toxic: Number.isFinite(toxic) ? toxic : null,
  };
}

export function pingLabel(
  player: PlayerDto,
  isHost: boolean,
  t: (key: string) => string,
) {
  if (player.isHost || (isHost && player.isLocal)) return '—';
  if (player.networkGrade == null || player.networkGrade < 0) return t('dashboard.ping_unknown');
  const keys = ['grade_broken', 'grade_terrible', 'grade_slow', 'grade_medium', 'grade_fine'] as const;
  const level = Math.max(0, Math.min(4, player.networkGrade));
  return t(`dashboard.${keys[level]}`);
}

export function pingBars(player: PlayerDto, isHost: boolean) {
  if (player.isHost || (isHost && player.isLocal)) return 5;
  if (player.networkGrade == null || player.networkGrade < 0) return 0;
  return Math.max(0, Math.min(4, player.networkGrade)) + 1;
}

export function pingClass(player: PlayerDto, isHost: boolean) {
  if (player.isHost || (isHost && player.isLocal)) return '';
  if (player.networkGrade == null || player.networkGrade < 0) return 'unknown';
  const level = Math.max(0, Math.min(4, player.networkGrade));
  if (level <= 1) return 'poor';
  if (level <= 2) return 'medium';
  return 'good';
}
