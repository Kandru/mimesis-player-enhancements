import { t } from '$lib/i18n';
import type { EntityCountEntryDto, StatCountersDto } from '$lib/types';
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

export function sumMonsterKills(counters?: StatCountersDto): number {
  if (!counters?.monsterKills) return 0;
  return Object.values(counters.monsterKills).reduce((sum, value) => sum + (value ?? 0), 0);
}

export function buildStatCards(counters: StatCountersDto | undefined, extras?: Record<string, string | number>) {
  if (!counters) return [] as Array<[string, string | number]>;
  const cards: Array<[string, string | number]> = [
    [t('dashboard.stat_team_score'), Math.round(counters.score ?? 0)],
    [t('dashboard.stat_train_value'), counters.trainValueDeposited ?? 0],
    [t('dashboard.stat_currency'), counters.currencyEarned ?? 0],
    [t('dashboard.stat_survival_wins'), counters.survivalWins ?? 0],
    [t('dashboard.stat_survival_deaths'), counters.survivalDeaths ?? 0],
    [t('dashboard.stat_revives'), counters.revives ?? 0],
    [t('dashboard.stat_friends_killed'), counters.friendsKilled ?? 0],
    [t('dashboard.stat_trap_deaths'), counters.trapDeaths ?? 0],
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

export function leaderboardEntryCounters(entry: Record<string, unknown>) {
  return {
    score: Number(entry.score ?? 0),
    trainValueDeposited: Number(entry.trainValueDeposited ?? 0),
    survivalDeaths: Number(entry.survivalDeaths ?? 0),
    revives: Number(entry.revives ?? 0),
    friendsKilled: Number(entry.friendsKilled ?? 0),
    currencyEarned: Number(entry.currencyEarned ?? 0),
    sessionsCompleted: Number(entry.sessionsCompleted ?? 0),
    runRestarts: Number(entry.runRestarts ?? 0),
    trapDeaths: Number(entry.trapDeaths ?? 0),
    run: entry.run as StatCountersDto | undefined,
    zones: entry.zones as Record<string, StatCountersDto> | undefined,
  };
}
