<script lang="ts">
  import DungeonRunCard from '$lib/components/statistics/DungeonRunCard.svelte';
  import StatCardGrid from '$lib/components/statistics/StatCardGrid.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import {
    formatCountMapFromBreakdown,
    formatEntityBreakdown,
  } from '$lib/statisticsHelpers';

  const stats = $derived(dashboard.playerStats);

  const killLines = $derived(
    formatEntityBreakdown(
      stats?.currentZone?.counters?.monsterKillBreakdown,
      'dashboard.killed_entity',
      'dashboard.killed_entity_plural',
    ),
  );

  const deathLines = $derived([
    ...formatEntityBreakdown(
      stats?.currentZone?.counters?.deathsByMonsterBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
    ...formatEntityBreakdown(
      stats?.currentZone?.counters?.deathsByTrapBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
  ]);
</script>

{#if dashboard.loadingStats}
  <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
{:else if !stats}
  <p class="text-sm text-gray-500">{t('dashboard.player_stats_not_found')}</p>
{:else}
  <div class="space-y-4">
    <div class="card p-6">
      <h2 class="mb-3 text-lg font-semibold">{t('dashboard.global_stats')}</h2>
      <StatCardGrid counters={stats.global.counters} extras={{
        [t('dashboard.stat_sessions')]: stats.global.sessionsCompleted ?? 0,
        [t('dashboard.stat_runs_played')]: stats.global.dungeonRunsPlayed ?? 0,
        [t('dashboard.stat_highest_zone')]: stats.global.highestZoneReached ?? 0,
      }} />
    </div>

    <div class="card p-6">
      <h2 class="mb-3 text-lg font-semibold">
        {t('dashboard.statistics_zone_current', { zone: stats.currentZone?.zone ?? 1 })}
      </h2>
      <StatCardGrid counters={stats.currentZone?.counters} />
    </div>

    {#if killLines.length > 0 || deathLines.length > 0}
      <div class="card p-6">
        <h2 class="mb-3 text-lg font-semibold">{t('dashboard.statistics_combat_breakdown')}</h2>
        {#if killLines.length > 0}
          <h3 class="mb-2 text-sm font-medium text-gray-500">{t('dashboard.statistics_kills')}</h3>
          <ul class="mb-4 space-y-1 text-sm">
            {#each killLines as line, index (index)}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
        {#if deathLines.length > 0}
          <h3 class="mb-2 text-sm font-medium text-gray-500">{t('dashboard.statistics_deaths')}</h3>
          <ul class="space-y-1 text-sm">
            {#each deathLines as line, index (index)}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
      </div>
    {/if}

    {#if (stats.zones || []).length > 0}
      <div class="space-y-3">
        <h2 class="text-lg font-semibold">{t('dashboard.statistics_by_zone')}</h2>
        {#each stats.zones as section (section.zone)}
          <details class="card p-4" open={section.zone === stats.currentZone?.zone}>
            <summary class="cursor-pointer font-medium">{t('dashboard.statistics_zone', { zone: section.zone })}</summary>
            <div class="mt-4">
              <StatCardGrid counters={section.counters} />
              <div class="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {#each formatCountMapFromBreakdown(section.counters.monsterKillBreakdown) as [label, value] (label)}
                  <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
                    <div class="text-xs text-gray-500">{label}</div>
                    <div class="text-lg font-semibold">{value}</div>
                  </div>
                {/each}
              </div>
            </div>
          </details>
        {/each}
      </div>
    {/if}

    {#if (stats.recentRuns || []).length > 0}
      <div class="space-y-3">
        <h2 class="text-lg font-semibold">{t('dashboard.statistics_runs')}</h2>
        {#each stats.recentRuns.slice(0, 12) as run (run.runId)}
          <DungeonRunCard {run} />
        {/each}
      </div>
    {/if}
  </div>
{/if}
