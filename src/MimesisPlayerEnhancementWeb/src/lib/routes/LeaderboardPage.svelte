<script lang="ts">
  import PlayerIdentity from '$lib/components/players/PlayerIdentity.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { buildStatCards, leaderboardEntryCounters } from '$lib/statisticsHelpers';
  import type { StatCountersDto } from '$lib/types';

  type SortKey = 'name' | 'score' | 'trainValue' | 'sessions';

  let sortKey = $state<SortKey>('score');
  let sortDir = $state<'asc' | 'desc'>('desc');

  const connectedSet = $derived(new Set(dashboard.leaderboard?.connectedSteamIds || []));
  const currentZone = $derived(dashboard.leaderboard?.currentZone ?? 1);

  const serverCards = $derived(buildStatCards(dashboard.leaderboard?.serverTotals, {
    [t('dashboard.stat_run_restarts')]: dashboard.leaderboard?.entries?.reduce((sum, entry) => sum + Number(entry.runRestarts ?? 0), 0) ?? 0,
  }));

  const entries = $derived.by(() => {
    const source = dashboard.leaderboard?.entries || [];
    const q = dashboard.headerSearchQuery.trim().toLowerCase();
    let list = [...source];
    if (q) {
      list = list.filter((entry) => {
        const hay = [entry.displayName, entry.steamId].map((v) => String(v ?? '').toLowerCase()).join(' ');
        return hay.includes(q);
      });
    }
    list.sort((a, b) => {
      const left = leaderboardEntryCounters(a);
      const right = leaderboardEntryCounters(b);
      let cmp = 0;
      if (sortKey === 'name') {
        cmp = String(a.displayName).localeCompare(String(b.displayName), undefined, { sensitivity: 'base' });
      } else if (sortKey === 'score') {
        cmp = left.score - right.score;
      } else if (sortKey === 'trainValue') {
        cmp = left.trainValueDeposited - right.trainValueDeposited;
      } else {
        cmp = left.sessionsCompleted - right.sessionsCompleted;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return list;
  });

  const zoneSummaries = $derived.by(() => {
    const zones = new Map<string, StatCountersDto>();
    for (const entry of dashboard.leaderboard?.entries || []) {
      const parsed = leaderboardEntryCounters(entry);
      for (const [zone, counters] of Object.entries(parsed.zones || {})) {
        const existing = zones.get(zone);
        if (!existing) {
          zones.set(zone, { ...counters });
          continue;
        }
        for (const [key, value] of Object.entries(counters)) {
          if (typeof value === 'number') {
            existing[key as keyof StatCountersDto] = Number(existing[key as keyof StatCountersDto] ?? 0) + value;
          }
        }
      }
    }

    const ordered = [...zones.entries()].sort((a, b) => Number(b[0]) - Number(a[0]));
    return ordered;
  });

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      sortDir = sortDir === 'asc' ? 'desc' : 'asc';
      return;
    }
    sortKey = key;
    sortDir = key === 'name' ? 'asc' : 'desc';
  }

  function sortIndicator(key: SortKey) {
    if (sortKey !== key) return '';
    return sortDir === 'asc' ? ' ↑' : ' ↓';
  }

  function zonePlayerRows(zone: string) {
    return entries
      .map((entry) => {
        const parsed = leaderboardEntryCounters(entry);
        const counters = parsed.zones?.[zone];
        if (!counters) return null;
        return {
          steamId: entry.steamId,
          displayName: entry.displayName,
          score: Math.round(counters.score ?? 0),
          trainValueDeposited: counters.trainValueDeposited ?? 0,
          survivalDeaths: counters.survivalDeaths ?? 0,
          revives: counters.revives ?? 0,
        };
      })
      .filter((row): row is NonNullable<typeof row> => row != null)
      .sort((a, b) => b.score - a.score);
  }
</script>

<div class="space-y-4">
  <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
    {#each serverCards as [label, value]}
      <div class="card p-4">
        <div class="text-xs text-gray-500">{label}</div>
        <div class="text-xl font-semibold">{value}</div>
      </div>
    {/each}
  </div>

  <div class="card overflow-hidden">
    <div class="overflow-x-auto">
      <table class="data-table">
        <thead>
          <tr>
            <th>
              <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('name')}>
                {t('dashboard.player')}{sortIndicator('name')}
              </button>
            </th>
            <th>
              <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('score')}>
                {t('dashboard.stat_team_score')}{sortIndicator('score')}
              </button>
            </th>
            <th>
              <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('trainValue')}>
                {t('dashboard.stat_train_value')}{sortIndicator('trainValue')}
              </button>
            </th>
            <th>{t('dashboard.stat_survival_deaths')}</th>
            <th>{t('dashboard.stat_revives')}</th>
            <th>{t('dashboard.stat_friends_killed')}</th>
            <th>
              <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('sessions')}>
                {t('dashboard.stat_sessions')}{sortIndicator('sessions')}
              </button>
            </th>
          </tr>
        </thead>
        <tbody>
          {#each entries as entry (entry.steamId)}
            {@const stats = leaderboardEntryCounters(entry)}
            <tr class="data-table-row">
              <td>
                <PlayerIdentity steamId={entry.steamId} displayName={entry.displayName}>
                  {#snippet badges()}
                    {#if connectedSet.has(String(entry.steamId))}
                      <span class="badge bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">{t('dashboard.badge_online')}</span>
                    {/if}
                  {/snippet}
                </PlayerIdentity>
              </td>
              <td>{Math.round(stats.score)}</td>
              <td>{stats.trainValueDeposited}</td>
              <td>{stats.survivalDeaths}</td>
              <td>{stats.revives}</td>
              <td>{stats.friendsKilled}</td>
              <td>{stats.sessionsCompleted}</td>
            </tr>
          {/each}
        </tbody>
      </table>
      {#if entries.length === 0}
        <p class="data-table-empty">{t('dashboard.leaderboard_empty')}</p>
      {/if}
    </div>
  </div>

  <div class="space-y-3">
    <h2 class="text-lg font-semibold">{t('dashboard.statistics_by_zone')}</h2>
    {#each zoneSummaries as [zone, totals] (zone)}
      <details class="card p-4" open={zone === String(currentZone)}>
        <summary class="cursor-pointer font-medium">
          {zone === String(currentZone)
            ? t('dashboard.statistics_zone_current', { zone })
            : t('dashboard.statistics_zone', { zone })}
        </summary>
        <div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {#each buildStatCards(totals) as [label, value]}
            <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
              <div class="text-xs text-gray-500">{label}</div>
              <div class="text-lg font-semibold">{value}</div>
            </div>
          {/each}
        </div>
        <div class="mt-4 overflow-x-auto">
          <table class="data-table">
            <thead>
              <tr>
                <th>{t('dashboard.player')}</th>
                <th>{t('dashboard.stat_team_score')}</th>
                <th>{t('dashboard.stat_train_value')}</th>
                <th>{t('dashboard.stat_survival_deaths')}</th>
                <th>{t('dashboard.stat_revives')}</th>
              </tr>
            </thead>
            <tbody>
              {#each zonePlayerRows(zone) as row (row.steamId)}
                <tr class="data-table-row">
                  <td>
                    <PlayerIdentity steamId={row.steamId} displayName={row.displayName} />
                  </td>
                  <td>{row.score}</td>
                  <td>{row.trainValueDeposited}</td>
                  <td>{row.survivalDeaths}</td>
                  <td>{row.revives}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </details>
    {/each}
    {#if zoneSummaries.length === 0}
      <p class="text-sm text-gray-500">{t('dashboard.statistics_zone_empty')}</p>
    {/if}
  </div>
</div>
