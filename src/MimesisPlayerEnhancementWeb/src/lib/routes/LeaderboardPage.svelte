<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';

  const entries = $derived(dashboard.leaderboard?.entries || []);
</script>

<div class="card overflow-hidden">
  <div class="overflow-x-auto">
    <table class="min-w-full text-sm">
      <thead class="bg-gray-50 text-left dark:bg-gray-800">
        <tr>
          <th class="px-4 py-2">{t('dashboard.player')}</th>
          <th class="px-4 py-2">{t('dashboard.stat_currency')}</th>
          <th class="px-4 py-2">{t('dashboard.stat_sessions')}</th>
        </tr>
      </thead>
      <tbody>
        {#each entries as entry (entry.steamId)}
          <tr class="border-t border-gray-100 dark:border-gray-700">
            <td class="px-4 py-2">
              <button class="text-[var(--brand)] hover:underline" onclick={() => navigate(`player/${entry.steamId}`)}>
                {entry.displayName || entry.steamId}
              </button>
            </td>
            <td class="px-4 py-2">{(entry as Record<string, number>).currencyEarned ?? 0}</td>
            <td class="px-4 py-2">{(entry as Record<string, number>).sessionsCompleted ?? 0}</td>
          </tr>
        {/each}
      </tbody>
    </table>
    {#if entries.length === 0}
      <p class="p-4 text-sm text-gray-500">{t('dashboard.leaderboard_empty')}</p>
    {/if}
  </div>
</div>
