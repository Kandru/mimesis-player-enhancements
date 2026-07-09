<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import PlayerCard from '$lib/components/players/PlayerCard.svelte';
  import { t } from '$lib/i18n';

  const connected = $derived(
    [...dashboard.players]
      .filter((p) => p.playerUid)
      .sort((a, b) => {
        const tier = (p: typeof a) => (p.isAlive ? 0 : 1);
        const tc = tier(a) - tier(b);
        if (tc !== 0) return tc;
        if (a.isHost !== b.isHost) return a.isHost ? -1 : 1;
        return String(a.displayName).localeCompare(String(b.displayName));
      }),
  );

  const stored = $derived(
    [...dashboard.players]
      .filter((p) => !p.playerUid)
      .sort((a, b) => String(a.displayName).localeCompare(String(b.displayName))),
  );
</script>

{#if dashboard.pageError}
  <div class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
    {dashboard.pageError}
  </div>
{/if}

<section class="mb-8">
  <h2 class="mb-4 text-lg font-semibold">{t('dashboard.players_connected_heading')}</h2>
  <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
    {#each connected as player (player.steamId)}
      <PlayerCard {player} />
    {/each}
  </div>
  {#if connected.length === 0}
    <p class="text-sm text-gray-500">{t('dashboard.no_players_connected')}</p>
  {/if}
</section>

{#if dashboard.status.isHost && stored.length > 0}
  <section>
    <h2 class="mb-4 text-lg font-semibold">{t('dashboard.players_stored_heading')}</h2>
    <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
      {#each stored as player (player.steamId)}
        <PlayerCard {player} stored />
      {/each}
    </div>
  </section>
{/if}
