<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { formatCountMap, formatDuration } from '$lib/utils';

  const stats = $derived(dashboard.playerStats);
  const globalCards = $derived.by(() => {
    if (!stats?.global) return [];
    const c = stats.global.counters || {};
    return [
      [t('dashboard.stat_currency'), c.currencyEarned ?? 0],
      [t('dashboard.stat_survival_wins'), c.survivalWins ?? 0],
      [t('dashboard.stat_survival_deaths'), c.survivalDeaths ?? 0],
      [t('dashboard.stat_connected_time'), formatDuration(c.totalConnectedSeconds ?? 0)],
      [t('dashboard.stat_sessions'), stats.global.sessionsCompleted ?? 0],
      ...formatCountMap(c.monsterKills),
    ] as Array<[string, string | number]>;
  });
</script>

{#if dashboard.loadingStats}
  <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
{:else if !stats}
  <p class="text-sm text-gray-500">{t('dashboard.player_stats_not_found')}</p>
{:else}
  <div class="card p-6">
    <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {#each globalCards as [label, value]}
        <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
          <div class="text-xs text-gray-500">{label}</div>
          <div class="text-lg font-semibold">{value}</div>
        </div>
      {/each}
    </div>
  </div>
{/if}
