<script lang="ts">
  import Api from '$lib/api';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { ItemOptionDto, PlayerDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    connectionMeta,
    lateJoinMeta,
    pingBars,
    pingClass,
    pingLabel,
    sessionLine,
    sortConnectedPlayers,
    sortPlayersByName,
    vitalsPercent,
  } from '$lib/playerHelpers';
  import {
    avatarUrl,
    formatVitalPercent,
    isValidSteamId,
    navigate,
    steamProfileUrl,
  } from '$lib/utils';

  let {
    players,
    stored = false,
  }: {
    players: PlayerDto[];
    stored?: boolean;
  } = $props();

  let pageSize = $state(10);
  let page = $state(0);

  const isHost = $derived(dashboard.status.isHost);
  const blindMode = $derived(dashboard.playerBlindMode);

  const filtered = $derived.by(() => {
    const q = dashboard.headerSearchQuery.trim().toLowerCase();
    let list = stored ? sortPlayersByName(players) : sortConnectedPlayers(players);
    if (!q) return list;
    return list.filter((p) => {
      const hay = [
        p.displayName,
        p.steamId,
        p.playerUid,
        p.connectionRole,
        p.connectionAddress,
        p.lateJoinLabel,
      ]
        .map((v) => String(v ?? '').toLowerCase())
        .join(' ');
      return hay.includes(q);
    });
  });

  const pageCount = $derived(Math.max(1, Math.ceil(filtered.length / pageSize)));
  const pageItems = $derived(filtered.slice(page * pageSize, page * pageSize + pageSize));

  $effect(() => {
    if (page >= pageCount) page = Math.max(0, pageCount - 1);
  });

  $effect(() => {
    void dashboard.headerSearchQuery;
    page = 0;
  });

  function showAlive(player: PlayerDto) {
    return !!player.playerUid && (!blindMode || player.isLocal);
  }

  function showVitals(player: PlayerDto) {
    return isHost && !blindMode && player.playerUid && player.health != null;
  }

  function showSession(player: PlayerDto) {
    return !blindMode && !!player.currentSession;
  }

  function canModerate(player: PlayerDto) {
    return isHost && !player.isLocal && !stored;
  }

  function canHeal(player: PlayerDto) {
    return isHost && !blindMode && player.playerUid && player.isAlive;
  }

  function canRespawn(player: PlayerDto) {
    return isHost && !blindMode && !player.isAlive && !!player.playerUid;
  }

  function canGiveItem(player: PlayerDto) {
    return canHeal(player);
  }

  function ensureSpawnSelection(steamId: string | number) {
    const key = String(steamId);
    if (!dashboard.spawnSelections[key]) {
      dashboard.spawnSelections[key] = {
        itemId: dashboard.itemCatalog[0]?.id || '',
        percent: 100,
      };
    }
    return dashboard.spawnSelections[key];
  }

  function getItemCatalogGroups() {
    const order = ['Consumable', 'Equipment', 'Miscellany', 'Developer'];
    const labelKeys: Record<string, string> = {
      Consumable: 'dashboard.spawn_item_category_consumable',
      Equipment: 'dashboard.spawn_item_category_equipment',
      Miscellany: 'dashboard.spawn_item_category_miscellany',
      Developer: 'dashboard.spawn_item_category_developer',
    };
    const buckets: Record<string, ItemOptionDto[]> = {};
    for (const item of dashboard.itemCatalog) {
      const type = item.type || 'Miscellany';
      (buckets[type] ??= []).push(item);
    }
    return order
      .filter((id) => buckets[id]?.length)
      .map((id) => ({ id, label: t(labelKeys[id] || id), items: buckets[id] }));
  }

  function getSelectedItemOption(steamId: string | number) {
    const sel = ensureSpawnSelection(steamId);
    return dashboard.itemCatalog.find((item) => item.id === sel.itemId);
  }

  function hasItemVariants(steamId: string | number) {
    return !!getSelectedItemOption(steamId)?.variants?.length;
  }

  function setSpawnItemId(steamId: string | number, itemId: string) {
    const sel = ensureSpawnSelection(steamId);
    const previousId = sel.itemId;
    sel.itemId = itemId;
    const option = dashboard.itemCatalog.find((item) => item.id === itemId);
    if (option?.variants?.length) {
      const percents = option.variants.map((v) => v.percent);
      if (previousId !== itemId || !percents.includes(sel.percent)) {
        sel.percent = option.variants[0].percent;
      }
    }
  }

  async function moderate(player: PlayerDto, action: string) {
    if (!confirm(t('dashboard.confirm_moderation', { action, steamId: player.steamId }))) return;
    try {
      const res = await Api.postAction(String(player.steamId), action);
      dashboard.showToast(res.message || t('api.done'));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  async function toggleCheat(player: PlayerDto, action: 'godmode' | 'noclip') {
    try {
      const res = await Api.postAction(String(player.steamId), action);
      dashboard.showToast(res.message || t('api.done'));
      if (typeof res.godMode === 'boolean') player.godMode = res.godMode;
      if (typeof res.noClip === 'boolean') player.noClip = res.noClip;
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  async function giveItem(player: PlayerDto) {
    const sel = ensureSpawnSelection(player.steamId);
    if (!sel.itemId) return;
    const percent = hasItemVariants(player.steamId) ? sel.percent : undefined;
    try {
      const res = await Api.spawnItem(String(player.steamId), sel.itemId, percent);
      dashboard.showToast(res.message || t('api.done'));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  async function deletePlayerData(player: PlayerDto) {
    const name = player.displayName || String(player.steamId);
    if (!confirm(t('dashboard.confirm_delete_player', { name, steamId: player.steamId }))) return;
    try {
      const res = await Api.deletePlayer(String(player.steamId));
      dashboard.showToast(res.message || t('api.done'));
      if (dashboard.route === 'player' && String(dashboard.steamId) === String(player.steamId)) {
        navigate('leaderboard');
      }
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  function playerDetails(player: PlayerDto) {
    const late = lateJoinMeta(
      player,
      isHost,
      t('dashboard.late_join_prefix', { label: player.lateJoinLabel || '' }),
      t('dashboard.late_join_attempts', { count: player.lateJoinAttemptCount ?? 0 }),
    );
    const session = showSession(player) ? sessionLine(player, t) : '';
    const stats = [late, session].filter(Boolean).join(' · ');
    return {
      connection: connectionMeta(
        player,
        t('dashboard.voice_lines', { count: player.voiceLineCount ?? 0 }),
      ),
      stats,
    };
  }
</script>

<div class="data-table-card">
  <div class="data-table-toolbar">
    <label class="data-table-page-size">
      <span>{t('dashboard.table_show')}</span>
      <select class="input data-table-select" bind:value={pageSize} onchange={() => (page = 0)}>
        <option value={5}>5</option>
        <option value={10}>10</option>
        <option value={25}>25</option>
        <option value={50}>50</option>
      </select>
      <span>{t('dashboard.table_entries')}</span>
    </label>
  </div>

  <div class="data-table-wrap">
    <table class="data-table">
      <thead>
        <tr>
          <th>{t('dashboard.table_player')}</th>
          {#if !stored}
            <th>{t('dashboard.table_ping')}</th>
          {/if}
          <th>{t('dashboard.table_connection')}</th>
          {#if !stored && isHost}
            <th>{t('dashboard.table_session')}</th>
            <th>{t('dashboard.table_vitals')}</th>
          {/if}
          {#if isHost}
            <th class="data-table-actions-col">{t('dashboard.table_actions')}</th>
          {/if}
        </tr>
      </thead>
      <tbody>
        {#each pageItems as player (player.steamId)}
          {@const details = playerDetails(player)}
          {@const vitals = vitalsPercent(player)}
          <tr class="data-table-row {showAlive(player) ? (player.isAlive ? 'row-alive' : 'row-dead') : ''}">
            <td>
              <div class="data-table-player">
                <img
                  class="data-table-avatar"
                  src={avatarUrl(player.steamId)}
                  alt=""
                  onerror={(e) => ((e.currentTarget as HTMLImageElement).src = '/img/default-avatar.svg')}
                />
                <div class="min-w-0">
                  <div class="data-table-player-name">
                    <a
                      class="hover:text-[var(--brand)]"
                      href={steamProfileUrl(player.steamId)}
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {player.displayName || player.steamId}
                    </a>
                    {#if player.isHost}<span class="badge badge-host">{t('dashboard.badge_host')}</span>{/if}
                    {#if player.isLocal}<span class="badge badge-local">{t('dashboard.badge_you')}</span>{/if}
                    {#if player.isBanned}<span class="badge bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300">{t('dashboard.badge_banned')}</span>{/if}
                    {#if showAlive(player)}
                      <span class="badge {player.isAlive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' : 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300'}">
                        {player.isAlive ? t('dashboard.badge_alive') : t('dashboard.badge_dead')}
                      </span>
                    {/if}
                    {#if isHost && player.lateJoinLabel}
                      <span class="badge bg-violet-100 text-violet-800 dark:bg-violet-900/30 dark:text-violet-300">{player.lateJoinLabel}</span>
                    {/if}
                  </div>
                  <div class="data-table-muted">{String(player.steamId)}</div>
                </div>
              </div>
            </td>
            {#if !stored}
              <td>
                <div class="ping-cell ping-{pingClass(player, isHost)}">
                  <div class="ping-bars" aria-hidden="true">
                    {#each Array(5) as _, i}
                      <span class="ping-bar {i < pingBars(player, isHost) ? 'active' : ''}"></span>
                    {/each}
                  </div>
                  <span class="data-table-muted">{pingLabel(player, isHost, t)}</span>
                </div>
              </td>
            {/if}
            <td>
              <div class="data-table-cell-stack">
                <span>{details.connection}</span>
                {#if details.stats}
                  <span class="data-table-stats">{details.stats}</span>
                {/if}
              </div>
            </td>
            {#if !stored && isHost}
              <td>
                {#if showSession(player)}
                  <span class="data-table-stats">{sessionLine(player, t) || '—'}</span>
                {:else}
                  <span class="data-table-muted">—</span>
                {/if}
              </td>
              <td>
                {#if showVitals(player)}
                  <div class="vitals-cell">
                    {#if vitals.health != null}
                      <div class="vital-row">
                        <span class="vital-label">{t('dashboard.health')}</span>
                        <div class="vital-track"><div class="vital-fill health" style="width:{Math.min(100, vitals.health)}%"></div></div>
                        <span class="vital-value">{formatVitalPercent(vitals.health)}</span>
                      </div>
                    {/if}
                    {#if vitals.toxic != null}
                      <div class="vital-row">
                        <span class="vital-label">{t('dashboard.toxic')}</span>
                        <div class="vital-track"><div class="vital-fill toxic" style="width:{Math.min(100, vitals.toxic)}%"></div></div>
                        <span class="vital-value">{formatVitalPercent(vitals.toxic)}</span>
                      </div>
                    {/if}
                  </div>
                {:else}
                  <span class="data-table-muted">—</span>
                {/if}
              </td>
            {/if}
            {#if isHost}
              <td>
                <div class="data-table-actions">
                  {#if isValidSteamId(player.steamId)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => navigate(`player/${player.steamId}`)}>
                      {t('dashboard.badge_stats')}
                    </button>
                  {/if}
                  {#if canModerate(player)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => moderate(player, 'kick')}>{t('dashboard.kick')}</button>
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => moderate(player, player.isBanned ? 'unban' : 'ban')}>
                      {player.isBanned ? t('dashboard.unban') : t('dashboard.ban')}
                    </button>
                    {#if canHeal(player)}
                      <button type="button" class="btn btn-secondary btn-xs" onclick={() => moderate(player, 'heal')}>{t('dashboard.heal')}</button>
                      <button
                        type="button"
                        class="btn btn-secondary btn-xs {player.godMode ? 'btn-active' : ''}"
                        title={t('dashboard.god_mode_title')}
                        onclick={() => toggleCheat(player, 'godmode')}
                      >
                        {t('dashboard.god_mode')} {player.godMode ? t('dashboard.on') : t('dashboard.off')}
                      </button>
                      <button
                        type="button"
                        class="btn btn-secondary btn-xs {player.noClip ? 'btn-active' : ''}"
                        title={t('dashboard.noclip_title')}
                        onclick={() => toggleCheat(player, 'noclip')}
                      >
                        {t('dashboard.noclip')} {player.noClip ? t('dashboard.on') : t('dashboard.off')}
                      </button>
                    {:else if canRespawn(player)}
                      <button type="button" class="btn btn-secondary btn-xs" onclick={() => moderate(player, 'respawn')}>{t('dashboard.respawn')}</button>
                    {/if}
                  {:else if stored}
                    {#if player.isBanned}
                      <button type="button" class="btn btn-secondary btn-xs" onclick={() => moderate(player, 'unban')}>{t('dashboard.unban')}</button>
                    {/if}
                    <button type="button" class="btn btn-danger btn-xs" onclick={() => deletePlayerData(player)}>{t('dashboard.delete_player')}</button>
                  {/if}
                  {#if canGiveItem(player) && dashboard.itemCatalog.length > 0}
                    <div class="spawn-item-controls">
                      <label class="sr-only" for="spawn-{player.steamId}">{t('dashboard.spawn_item')}</label>
                      <select
                        id="spawn-{player.steamId}"
                        class="input spawn-item-select"
                        value={ensureSpawnSelection(player.steamId).itemId}
                        onchange={(e) => setSpawnItemId(player.steamId, (e.currentTarget as HTMLSelectElement).value)}
                      >
                        {#each getItemCatalogGroups() as group}
                          <optgroup label={group.label}>
                            {#each group.items as item}
                              <option value={item.id}>{item.label}</option>
                            {/each}
                          </optgroup>
                        {/each}
                      </select>
                      {#if hasItemVariants(player.steamId)}
                        <select
                          class="input spawn-item-select spawn-item-percent"
                          value={ensureSpawnSelection(player.steamId).percent}
                          onchange={(e) => {
                            ensureSpawnSelection(player.steamId).percent = parseInt((e.currentTarget as HTMLSelectElement).value, 10);
                          }}
                        >
                          {#each getSelectedItemOption(player.steamId)?.variants || [] as variant}
                            <option value={variant.percent}>{t('dashboard.spawn_item_percent_option', { percent: variant.percent })}</option>
                          {/each}
                        </select>
                      {/if}
                      <button type="button" class="btn btn-primary btn-xs" onclick={() => giveItem(player)}>{t('dashboard.spawn_item_give')}</button>
                    </div>
                  {/if}
                </div>
              </td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>
    {#if pageItems.length === 0}
      <p class="data-table-empty">{stored ? t('dashboard.players_stored_empty') : t('dashboard.no_players_connected')}</p>
    {/if}
  </div>

  <div class="data-table-footer">
    <span>
      {t('dashboard.table_showing', {
        from: filtered.length === 0 ? 0 : page * pageSize + 1,
        to: Math.min(filtered.length, (page + 1) * pageSize),
        total: filtered.length,
      })}
    </span>
    {#if pageCount > 1}
      <div class="data-table-pagination">
        <button type="button" class="btn btn-secondary btn-xs" disabled={page === 0} onclick={() => page--}>{t('dashboard.table_prev')}</button>
        <span class="data-table-page-num">{page + 1} / {pageCount}</span>
        <button type="button" class="btn btn-secondary btn-xs" disabled={page >= pageCount - 1} onclick={() => page++}>{t('dashboard.table_next')}</button>
      </div>
    {/if}
  </div>
</div>
