import { t } from '$lib/i18n';
import type {
  DungeonRunOutcome,
  EntityCountEntryDto,
  LeaderboardEntryDto,
  PlayerRunStatsDto,
  StatCountersDto,
  StatisticsHistoryRunDto,
} from '$lib/types';
import { formatDuration } from '$lib/utils';

export function localizeEntity(entry: EntityCountEntryDto): string {
  if (entry.localizationKey) {
    const localized = t(entry.localizationKey);
    if (localized !== entry.localizationKey) {
      return localized;
    }
  }

  return entry.displayName || entry.key;
}

export function formatEntityBreakdown(
  entries: EntityCountEntryDto[] | undefined,
  templateKey: string,
  templatePluralKey: string,
) {
  if (!entries?.length) return [] as string[];
  return entries.map((entry) => {
    const name = localizeEntity(entry);
    const key = entry.count === 1 ? templateKey : templatePluralKey;
    return t(key, { name, count: entry.count });
  });
}

export function formatCountMapFromBreakdown(entries: EntityCountEntryDto[] | undefined) {
  if (!entries?.length) return [] as Array<[string, number]>;
  return entries.map((entry) => [localizeEntity(entry), entry.count] as [string, number]);
}

export function formatOutcome(outcome: DungeonRunOutcome): string {
  const key = `dashboard.statistics_outcome_${outcome}`;
  const label = t(key);
  return label === key ? outcome : label;
}

export function formatRunLabel(run: StatisticsHistoryRunDto | PlayerRunStatsDto): string {
  const name = run.mapName || run.mapKey || run.runId;
  return t('dashboard.statistics_run_label', { map: name, seed: run.seed });
}

export function buildStatCards(counters: StatCountersDto | undefined, extras?: Record<string, string | number>) {
  if (!counters) return [] as Array<[string, string | number]>;
  const cards: Array<[string, string | number]> = [
    [t('dashboard.stat_team_score'), Math.round(counters.score ?? 0)],
    [t('dashboard.stat_value_saved'), counters.trainValueDeposited ?? 0],
    [t('dashboard.stat_items_saved'), counters.itemsDeposited ?? 0],
    [t('dashboard.stat_items_carried'), counters.itemsCarried ?? 0],
    [t('dashboard.stat_monsters_killed'), counters.monsterKillsTotal ?? 0],
    [t('dashboard.stat_deaths'), counters.deaths ?? 0],
    [t('dashboard.stat_trap_deaths'), counters.trapDeathsTotal ?? 0],
    [t('dashboard.stat_survival_wins'), counters.survivalWins ?? 0],
    [t('dashboard.stat_revives'), counters.revives ?? 0],
    [t('dashboard.stat_friends_killed'), counters.friendsKilled ?? 0],
    [t('dashboard.stat_killed_by_friends'), counters.killedByFriends ?? 0],
    [t('dashboard.stat_dungeon_exits_alive'), counters.dungeonExitsAlive ?? 0],
    [t('dashboard.stat_dungeon_exits_dead'), counters.dungeonExitsDead ?? 0],
  ];

  if (counters.medianLifetimeMs != null) {
    cards.push([t('dashboard.stat_median_lifetime'), formatDuration(Math.floor(counters.medianLifetimeMs / 1000))]);
  }

  if (extras) {
    for (const [label, value] of Object.entries(extras)) {
      cards.push([label, value]);
    }
  }

  return cards;
}

export function buildGlobalSummaryCards(counters: StatCountersDto | undefined, runRestarts = 0) {
  return buildStatCards(counters, runRestarts > 0
    ? { [t('dashboard.stat_run_restarts')]: runRestarts }
    : undefined);
}

export function leaderboardEntrySortValue(entry: LeaderboardEntryDto, key: LeaderboardSortKey) {
  switch (key) {
    case 'name':
      return entry.displayName;
    case 'score':
      return entry.global.score ?? entry.score ?? 0;
    case 'trainValue':
      return entry.global.trainValueDeposited ?? 0;
    case 'sessions':
      return entry.sessionsCompleted ?? 0;
    case 'runs':
      return entry.dungeonRunsPlayed ?? 0;
    case 'zone':
      return entry.highestZoneReached ?? 0;
    default:
      return 0;
  }
}

export type LeaderboardSortKey = 'name' | 'score' | 'trainValue' | 'sessions' | 'runs' | 'zone';
