<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import {
    buildStatCards,
    formatCountMapFromBreakdown,
    formatEntityBreakdown,
  } from '$lib/statisticsHelpers';
  import type { StatCountersDto } from '$lib/types';

  const stats = $derived(dashboard.playerStats);

  const runCards = $derived(buildStatCards(stats?.currentRun?.counters, {
    [t('dashboard.stat_run_restarts')]: stats?.global?.runRestarts ?? 0,
  }));

  const allTimeCards = $derived(buildStatCards(stats?.global?.counters, {
    [t('dashboard.stat_sessions')]: stats?.global?.sessionsCompleted ?? 0,
  }));

  const killLines = $derived(
    formatEntityBreakdown(
      stats?.currentRun?.counters?.monsterKillBreakdown,
      'dashboard.killed_entity',
      'dashboard.killed_entity_plural',
    ),
  );

  const deathLines = $derived([
    ...formatEntityBreakdown(
      stats?.currentRun?.counters?.deathsByMonsterBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
    ...formatEntityBreakdown(
      stats?.currentRun?.counters?.deathsByTrapBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
  ]);

  const zoneSections = $derived.by(() => {
    const zones = stats?.currentRun?.zones;
    if (!zones) return [] as Array<{ zone: string; counters: StatCountersDto }>;
    return Object.entries(zones)
      .sort((a, b) => Number(b[0]) - Number(a[0]))
      .map(([zone, counters]) => ({ zone, counters }));
  });
</script>

{#if dashboard.loadingStats}
  <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
{:else if !stats}
  <p class="text-sm text-gray-500">{t('dashboard.player_stats_not_found')}</p>
{:else}
  <div class="space-y-4">
    <div class="card p-6">
      <h2 class="mb-3 text-lg font-semibold">{t('dashboard.statistics_current_run')}</h2>
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {#each runCards as [label, value]}
          <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
            <div class="text-xs text-gray-500">{label}</div>
            <div class="text-lg font-semibold">{value}</div>
          </div>
        {/each}
      </div>
    </div>

    <div class="card p-6">
      <h2 class="mb-3 text-lg font-semibold">{t('dashboard.global_stats')}</h2>
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {#each allTimeCards as [label, value]}
          <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
            <div class="text-xs text-gray-500">{label}</div>
            <div class="text-lg font-semibold">{value}</div>
          </div>
        {/each}
      </div>
    </div>

    {#if killLines.length > 0 || deathLines.length > 0}
      <div class="card p-6">
        <h2 class="mb-3 text-lg font-semibold">{t('dashboard.statistics_combat_breakdown')}</h2>
        {#if killLines.length > 0}
          <h3 class="mb-2 text-sm font-medium text-gray-500">{t('dashboard.statistics_kills')}</h3>
          <ul class="mb-4 space-y-1 text-sm">
            {#each killLines as line}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
        {#if deathLines.length > 0}
          <h3 class="mb-2 text-sm font-medium text-gray-500">{t('dashboard.statistics_deaths')}</h3>
          <ul class="space-y-1 text-sm">
            {#each deathLines as line}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
      </div>
    {/if}

    {#if zoneSections.length > 0}
      <div class="space-y-3">
        <h2 class="text-lg font-semibold">{t('dashboard.statistics_by_zone')}</h2>
        {#each zoneSections as section (section.zone)}
          <details class="card p-4" open={section.zone === zoneSections[0]?.zone}>
            <summary class="cursor-pointer font-medium">{t('dashboard.statistics_zone', { zone: section.zone })}</summary>
            <div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {#each buildStatCards(section.counters) as [label, value]}
                <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
                  <div class="text-xs text-gray-500">{label}</div>
                  <div class="text-lg font-semibold">{value}</div>
                </div>
              {/each}
              {#each formatCountMapFromBreakdown(section.counters.monsterKillBreakdown) as [label, value]}
                <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
                  <div class="text-xs text-gray-500">{label}</div>
                  <div class="text-lg font-semibold">{value}</div>
                </div>
              {/each}
            </div>
          </details>
        {/each}
      </div>
    {/if}
  </div>
{/if}
