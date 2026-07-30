<script lang="ts">
  import DungeonRunCard from '$lib/components/statistics/DungeonRunCard.svelte';
  import PlayerStatsTable from '$lib/components/statistics/PlayerStatsTable.svelte';
  import StatCardGrid from '$lib/components/statistics/StatCardGrid.svelte';
  import { t } from '$lib/i18n';
  import type { StatisticsHistoryZoneDto } from '$lib/types';

  let {
    zone,
    connectedSteamIds = [],
  }: {
    zone: StatisticsHistoryZoneDto;
    connectedSteamIds?: string[];
  } = $props();

  const playerRows = $derived(
    (zone.players || []).map((player) => ({
      steamId: player.steamId,
      displayName: player.displayName,
      counters: player.counters,
    })),
  );
</script>

<details class="card p-4" open={zone.isCurrent}>
  <summary class="cursor-pointer font-medium">
    {zone.isCurrent
      ? t('dashboard.statistics_zone_current', { zone: zone.zone })
      : t('dashboard.statistics_zone', { zone: zone.zone })}
    {#if zone.trimmedRunCount > 0}
      <span class="ml-2 text-xs text-gray-500">
        {t('dashboard.statistics_trimmed_runs', { count: zone.trimmedRunCount })}
      </span>
    {/if}
  </summary>

  <div class="mt-4 space-y-4">
    <StatCardGrid counters={zone.totals} />

    <div>
      <h3 class="mb-2 text-sm font-medium text-gray-500">{t('dashboard.player')}</h3>
      <PlayerStatsTable rows={playerRows} connectedSteamIds={connectedSteamIds} />
    </div>

    <div class="space-y-3">
      <h3 class="text-sm font-medium text-gray-500">{t('dashboard.statistics_runs')}</h3>
      {#if (zone.runs || []).length === 0}
        <p class="text-sm text-gray-500">{t('dashboard.statistics_runs_empty')}</p>
      {:else}
        {#each zone.runs as run (run.runId)}
          <DungeonRunCard {run} />
        {/each}
      {/if}
    </div>
  </div>
</details>
