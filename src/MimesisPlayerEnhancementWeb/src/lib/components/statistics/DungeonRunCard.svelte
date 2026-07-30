<script lang="ts">
  import PlayerStatsTable from '$lib/components/statistics/PlayerStatsTable.svelte';
  import StatCardGrid from '$lib/components/statistics/StatCardGrid.svelte';
  import { t } from '$lib/i18n';
  import { formatEntityBreakdown, formatOutcome, formatRunLabel } from '$lib/statisticsHelpers';
  import type { PlayerRunStatsDto, StatisticsHistoryRunDto } from '$lib/types';
  import { formatDuration } from '$lib/utils';

  type RunCardData = StatisticsHistoryRunDto | PlayerRunStatsDto;

  let { run }: { run: RunCardData } = $props();

  const totals = $derived('totals' in run ? run.totals : run.counters);
  const players = $derived('players' in run ? (run.players ?? []) : []);

  const rows = $derived(
    players.map((player) => ({
      steamId: player.steamId,
      displayName: player.displayName,
      counters: player.counters,
    })),
  );

  const durationSeconds = $derived('durationSeconds' in run ? run.durationSeconds : null);
  const cycle = $derived('cycle' in run ? run.cycle : null);

  const killLines = $derived(
    formatEntityBreakdown(
      totals?.monsterKillBreakdown,
      'dashboard.killed_entity',
      'dashboard.killed_entity_plural',
    ),
  );

  const deathLines = $derived([
    ...formatEntityBreakdown(
      totals?.deathsByMonsterBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
    ...formatEntityBreakdown(
      totals?.deathsByTrapBreakdown,
      'dashboard.killed_by_entity',
      'dashboard.killed_by_entity_plural',
    ),
  ]);
</script>

<article class="rounded-lg border border-gray-200 p-4 dark:border-gray-700">
  <header class="mb-3 flex flex-wrap items-center justify-between gap-2">
    <div>
      <h3 class="font-semibold">{formatRunLabel(run)}</h3>
      <p class="text-xs text-gray-500">
        {#if cycle != null}
          {t('dashboard.statistics_run_cycle', { cycle })}
        {/if}
        {#if durationSeconds != null}
          {#if cycle != null}·{/if}
          {t('dashboard.statistics_run_duration', { duration: formatDuration(durationSeconds) })}
        {/if}
      </p>
    </div>
    <span class="badge">{formatOutcome(run.outcome)}</span>
  </header>

  {#if rows.length > 0}
    <PlayerStatsTable {rows} />
  {:else}
    <StatCardGrid counters={totals} />
  {/if}

  {#if killLines.length > 0 || deathLines.length > 0}
    <details class="mt-3">
      <summary class="cursor-pointer text-sm text-gray-500">{t('dashboard.statistics_combat_breakdown')}</summary>
      <div class="mt-2 space-y-2 text-sm">
        {#if killLines.length > 0}
          <ul class="space-y-1">
            {#each killLines as line, index (index)}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
        {#if deathLines.length > 0}
          <ul class="space-y-1">
            {#each deathLines as line, index (index)}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
      </div>
    </details>
  {/if}
</article>
