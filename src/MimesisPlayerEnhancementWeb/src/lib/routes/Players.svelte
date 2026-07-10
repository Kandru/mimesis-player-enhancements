<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import PlayersTable from '$lib/components/players/PlayersTable.svelte';
  import { t } from '$lib/i18n';

  const connected = $derived(dashboard.players.filter((p) => p.playerUid));
  const stored = $derived(dashboard.players.filter((p) => !p.playerUid));
  const routingCount = $derived(dashboard.status.joinAnytimeRoutingCount ?? 0);
</script>

{#if dashboard.pageError}
  <div class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
    {dashboard.pageError}
  </div>
{/if}

{#if dashboard.status.isHost && routingCount > 0}
  <div class="page-heading mb-3">
    <p class="page-subtitle">
      {t('dashboard.late_join_routing', { count: routingCount })}
    </p>
  </div>
{/if}

<PlayersTable players={connected} />

{#if dashboard.status.isHost && stored.length > 0}
  <section class="mt-6">
    <div class="page-heading mb-3">
      <h3 class="page-title text-base">{t('dashboard.players_stored_heading')}</h3>
    </div>
    <PlayersTable players={stored} stored />
  </section>
{/if}
