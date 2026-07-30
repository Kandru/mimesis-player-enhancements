<script lang="ts">
  import PlayerStatsTable from '$lib/components/statistics/PlayerStatsTable.svelte';
  import StatCardGrid from '$lib/components/statistics/StatCardGrid.svelte';
  import ZoneSection from '$lib/components/statistics/ZoneSection.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import {
    leaderboardEntrySortValue,
    type LeaderboardSortKey,
  } from '$lib/statisticsHelpers';
  import type { LeaderboardEntryDto } from '$lib/types';

  let sortKey = $state<LeaderboardSortKey>('score');
  let sortDir = $state<'asc' | 'desc'>('desc');

  const connectedSteamIds = $derived(dashboard.leaderboard?.connectedSteamIds ?? []);
  const historyRevision = $derived(dashboard.leaderboard?.historyRevision ?? -1);
  const cachedHistoryRevision = $derived(dashboard.statisticsHistory?.historyRevision ?? -1);

  const runRestartsTotal = $derived(
    dashboard.leaderboard?.entries?.reduce((sum, entry) => sum + Number(entry.runRestarts ?? 0), 0) ?? 0,
  );

  const entries = $derived.by(() => {
    const source = dashboard.leaderboard?.entries ?? [];
    const q = dashboard.headerSearchQuery.trim().toLowerCase();
    let list: LeaderboardEntryDto[] = [...source];
    if (q) {
      list = list.filter((entry) => {
        const hay = [entry.displayName, entry.steamId].map((v) => String(v ?? '').toLowerCase()).join(' ');
        return hay.includes(q);
      });
    }
    list.sort((a, b) => {
      const left = leaderboardEntrySortValue(a, sortKey);
      const right = leaderboardEntrySortValue(b, sortKey);
      let cmp = 0;
      if (typeof left === 'string' && typeof right === 'string') {
        cmp = left.localeCompare(right, undefined, { sensitivity: 'base' });
      } else {
        cmp = Number(left) - Number(right);
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return list;
  });

  const globalRows = $derived(
    entries.map((entry) => ({
      steamId: entry.steamId,
      displayName: entry.displayName,
      counters: entry.global,
      highestZoneReached: entry.highestZoneReached,
      sessionsCompleted: entry.sessionsCompleted,
      dungeonRunsPlayed: entry.dungeonRunsPlayed,
    })),
  );

  const historyZones = $derived(dashboard.statisticsHistory?.zones ?? []);

  $effect(() => {
    if (dashboard.route !== 'leaderboard' || !dashboard.status.isConnected || !dashboard.status.isHost) {
      return;
    }
    const needsRefresh = historyRevision < 0 || historyRevision !== cachedHistoryRevision;
    void dashboard.loadStatisticsHistory(needsRefresh);
  });

  function toggleSort(key: LeaderboardSortKey) {
    if (sortKey === key) {
      sortDir = sortDir === 'asc' ? 'desc' : 'asc';
      return;
    }
    sortKey = key;
    sortDir = key === 'name' ? 'asc' : 'desc';
  }

  function sortIndicator(key: LeaderboardSortKey) {
    if (sortKey !== key) return '';
    return sortDir === 'asc' ? ' ↑' : ' ↓';
  }
</script>

<div class="space-y-4">
  <section class="space-y-3">
    <h2 class="text-lg font-semibold">{t('dashboard.statistics_global')}</h2>
    <StatCardGrid
      counters={dashboard.leaderboard?.serverGlobalTotals}
      extras={runRestartsTotal > 0 ? { [t('dashboard.stat_run_restarts')]: runRestartsTotal } : undefined}
    />
  </section>

  <section class="card overflow-hidden">
    <div class="border-b border-gray-200 px-4 py-3 dark:border-gray-700">
      <div class="flex flex-wrap gap-2 text-xs">
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('name')}>
          {t('dashboard.player')}{sortIndicator('name')}
        </button>
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('score')}>
          {t('dashboard.stat_team_score')}{sortIndicator('score')}
        </button>
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('trainValue')}>
          {t('dashboard.stat_value_saved')}{sortIndicator('trainValue')}
        </button>
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('zone')}>
          {t('dashboard.stat_highest_zone')}{sortIndicator('zone')}
        </button>
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('sessions')}>
          {t('dashboard.stat_sessions')}{sortIndicator('sessions')}
        </button>
        <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('runs')}>
          {t('dashboard.stat_runs_played')}{sortIndicator('runs')}
        </button>
      </div>
    </div>
    <PlayerStatsTable
      rows={globalRows}
      {connectedSteamIds}
      showHighestZone
      showSessions
    />
  </section>

  <section class="space-y-3">
    <div class="flex items-center gap-3">
      <h2 class="text-lg font-semibold">{t('dashboard.statistics_by_zone')}</h2>
      {#if dashboard.loadingStatisticsHistory}
        <span class="text-sm text-gray-500">{t('dashboard.loading')}</span>
      {/if}
    </div>

    {#if (dashboard.statisticsHistory?.trimmedZoneCount ?? 0) > 0}
      <p class="text-sm text-gray-500">
        {t('dashboard.statistics_trimmed_zones', { count: dashboard.statisticsHistory?.trimmedZoneCount ?? 0 })}
      </p>
    {/if}

    {#each historyZones as zone (zone.zone)}
      <ZoneSection {zone} {connectedSteamIds} />
    {/each}

    {#if !dashboard.loadingStatisticsHistory && historyZones.length === 0}
      <p class="text-sm text-gray-500">{t('dashboard.statistics_zone_empty')}</p>
    {/if}
  </section>
</div>
