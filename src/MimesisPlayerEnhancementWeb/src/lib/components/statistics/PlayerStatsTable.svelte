<script lang="ts">
  import PlayerIdentity from '$lib/components/players/PlayerIdentity.svelte';
  import { t } from '$lib/i18n';
  import type { StatCountersDto } from '$lib/types';

  export type PlayerStatsRow = {
    steamId: string;
    displayName: string;
    counters: StatCountersDto;
    highestZoneReached?: number;
    sessionsCompleted?: number;
    dungeonRunsPlayed?: number;
  };

  let {
    rows,
    showHighestZone = false,
    showSessions = false,
    connectedSteamIds = [],
  }: {
    rows: PlayerStatsRow[];
    showHighestZone?: boolean;
    showSessions?: boolean;
    connectedSteamIds?: string[];
  } = $props();

  const connectedSet = $derived(new Set(connectedSteamIds.map(String)));
</script>

<div class="overflow-x-auto">
  <table class="data-table">
    <thead>
      <tr>
        <th>{t('dashboard.player')}</th>
        <th>{t('dashboard.stat_team_score')}</th>
        <th>{t('dashboard.stat_value_saved')}</th>
        <th>{t('dashboard.stat_items_saved')}</th>
        <th>{t('dashboard.stat_monsters_killed')}</th>
        <th>{t('dashboard.stat_friends_killed')}</th>
        <th>{t('dashboard.stat_killed_by_friends')}</th>
        <th>{t('dashboard.stat_deaths')}</th>
        <th>{t('dashboard.stat_revives')}</th>
        {#if showHighestZone}
          <th>{t('dashboard.stat_highest_zone')}</th>
        {/if}
        {#if showSessions}
          <th>{t('dashboard.stat_sessions')}</th>
        {/if}
      </tr>
    </thead>
    <tbody>
      {#each rows as row (row.steamId)}
        <tr class="data-table-row">
          <td>
            <PlayerIdentity steamId={row.steamId} displayName={row.displayName}>
              {#snippet badges()}
                {#if connectedSet.has(String(row.steamId))}
                  <span class="badge bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">{t('dashboard.badge_online')}</span>
                {/if}
              {/snippet}
            </PlayerIdentity>
          </td>
          <td>{Math.round(row.counters.score ?? 0)}</td>
          <td>{row.counters.trainValueDeposited ?? 0}</td>
          <td>{row.counters.itemsDeposited ?? 0}</td>
          <td>{row.counters.monsterKillsTotal ?? 0}</td>
          <td>{row.counters.friendsKilled ?? 0}</td>
          <td>{row.counters.killedByFriends ?? 0}</td>
          <td>{row.counters.deaths ?? 0}</td>
          <td>{row.counters.revives ?? 0}</td>
          {#if showHighestZone}
            <td>{row.highestZoneReached ?? 0}</td>
          {/if}
          {#if showSessions}
            <td>{row.sessionsCompleted ?? 0}</td>
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
  {#if rows.length === 0}
    <p class="data-table-empty">{t('dashboard.leaderboard_empty')}</p>
  {/if}
</div>
