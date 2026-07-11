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
  return connectionMetaShort(player, voiceLinesLabel);
}

export function connectionMetaShort(player: PlayerDto, voiceLinesLabel: string) {
  const parts: string[] = [];
  if (player.playerUid) parts.push(`#${player.playerUid}`);

  const role = player.connectionRole?.trim().toLowerCase();
  if (role && role !== 'host' && role !== 'client') {
    parts.push(player.connectionRole);
  }

  const addr = player.connectionAddress?.trim();
  if (addr && addr !== 'local' && addr !== '(unavailable)') {
    parts.push(addr === 'steam-sdr' ? 'SDR' : addr);
  }

  if (voiceLinesLabel) parts.push(voiceLinesLabel);
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

function compactStatsLine(
  session: PlayerDto['currentSession'],
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  if (!session) return '';
  const parts: string[] = [];
  if (session.currencyEarned) {
    parts.push(t('dashboard.stat_short_currency', { count: session.currencyEarned }));
  }
  if (session.survivalWins || session.survivalDeaths || session.survivalLeftBehind) {
    parts.push(
      t('dashboard.stat_short_survival', {
        wins: session.survivalWins ?? 0,
        deaths: session.survivalDeaths ?? 0,
        left: session.survivalLeftBehind ?? 0,
      }),
    );
  }
  if (session.deathmatchWins || session.deathmatchDeaths) {
    parts.push(
      t('dashboard.stat_short_deathmatch', {
        wins: session.deathmatchWins ?? 0,
        deaths: session.deathmatchDeaths ?? 0,
      }),
    );
  }
  if (session.revives) parts.push(t('dashboard.stat_short_revives', { count: session.revives }));
  if (session.totalConnectedSeconds) parts.push(formatDuration(session.totalConnectedSeconds));
  return parts.join(' · ');
}

export function statisticsSummaryShort(
  player: PlayerDto,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  return {
    total: compactStatsLine(player.totalStats, t),
    session: compactStatsLine(player.currentSession, t),
  };
}

export function voiceLinesShort(
  count: number,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  if (!count) return '';
  return t('dashboard.voice_lines_short', { count });
}

export function playerActivityLine(
  player: PlayerDto,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  const state = player.activityState?.trim();
  if (!state) return '';
  if (state === 'late_join' && player.activityDetail) {
    return player.activityDetail;
  }
  const detail = player.activityDetail?.trim();
  if (state === 'loading') {
    const sceneKey = detail || 'session';
    const scene = t(`dashboard.player_state_scene_${sceneKey}`, {});
    return t('dashboard.player_state_loading', { scene });
  }
  const key = `dashboard.player_state_${state}`;
  const label = t(key, {});
  return label === key ? state : label;
}

export function statisticsLines(
  player: PlayerDto,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  const total = player.totalStats ? sessionLine({ ...player, currentSession: player.totalStats }, t) : '';
  const session = player.currentSession ? sessionLine(player, t) : '';
  return { total, session };
}

export function storedDetailsLine(
  player: PlayerDto,
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  const parts: string[] = [];
  if (player.isBanned) parts.push(t('dashboard.badge_banned'));
  const { total, session } = statisticsSummaryShort(player, t);
  if (total) parts.push(t('dashboard.statistics_total_prefix', { stats: total }));
  if (session && session !== total) parts.push(t('dashboard.statistics_session_prefix', { stats: session }));
  if (player.voiceLineCount) {
    parts.push(voiceLinesShort(player.voiceLineCount, t));
  }
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
