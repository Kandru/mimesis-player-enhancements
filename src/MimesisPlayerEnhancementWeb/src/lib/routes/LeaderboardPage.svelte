<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';

  type SortKey = 'name' | 'currency' | 'sessions';
  type SortDir = 'asc' | 'desc';

  let sortKey = $state<SortKey>('currency');
  let sortDir = $state<SortDir>('desc');

  const connectedSet = $derived(new Set(dashboard.leaderboard?.connectedSteamIds || []));

  const entries = $derived.by(() => {
    const source = dashboard.leaderboard?.entries || [];
    const q = dashboard.headerSearchQuery.trim().toLowerCase();
    let list = [...source];
    if (q) {
      list = list.filter((entry) => {
        const hay = [entry.displayName, entry.steamId].map((v) => String(v ?? '').toLowerCase()).join(' ');
        return hay.includes(q);
      });
    }
    list.sort((a, b) => {
      let cmp = 0;
      if (sortKey === 'name') {
        cmp = String(a.displayName || a.steamId).localeCompare(String(b.displayName || b.steamId));
      } else if (sortKey === 'currency') {
        cmp = ((a as Record<string, number>).currencyEarned ?? 0) - ((b as Record<string, number>).currencyEarned ?? 0);
      } else {
        cmp = ((a as Record<string, number>).sessionsCompleted ?? 0) - ((b as Record<string, number>).sessionsCompleted ?? 0);
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return list;
  });

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      sortDir = sortDir === 'asc' ? 'desc' : 'asc';
      return;
    }
    sortKey = key;
    sortDir = key === 'name' ? 'asc' : 'desc';
  }

  function sortIndicator(key: SortKey) {
    if (sortKey !== key) return '';
    return sortDir === 'asc' ? ' ↑' : ' ↓';
  }
</script>

<div class="card overflow-hidden">
  <div class="overflow-x-auto">
    <table class="data-table">
      <thead>
        <tr>
          <th>
            <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('name')}>
              {t('dashboard.player')}{sortIndicator('name')}
            </button>
          </th>
          <th>
            <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('currency')}>
              {t('dashboard.stat_currency')}{sortIndicator('currency')}
            </button>
          </th>
          <th>
            <button type="button" class="leaderboard-sort-btn" onclick={() => toggleSort('sessions')}>
              {t('dashboard.stat_sessions')}{sortIndicator('sessions')}
            </button>
          </th>
        </tr>
      </thead>
      <tbody>
        {#each entries as entry (entry.steamId)}
          <tr class="data-table-row">
            <td>
              <div class="data-table-player-name">
                <button class="text-[var(--brand)] hover:underline" onclick={() => navigate(`player/${entry.steamId}`)}>
                  {entry.displayName || entry.steamId}
                </button>
                {#if connectedSet.has(String(entry.steamId))}
                  <span class="badge bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300">{t('dashboard.badge_online')}</span>
                {/if}
              </div>
              <div class="data-table-muted">{String(entry.steamId)}</div>
            </td>
            <td>{(entry as Record<string, number>).currencyEarned ?? 0}</td>
            <td>{(entry as Record<string, number>).sessionsCompleted ?? 0}</td>
          </tr>
        {/each}
      </tbody>
    </table>
    {#if entries.length === 0}
      <p class="data-table-empty">{t('dashboard.leaderboard_empty')}</p>
    {/if}
  </div>
</div>
