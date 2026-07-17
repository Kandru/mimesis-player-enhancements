<script lang="ts">
  import Api from '$lib/api';
  import ConfirmDialog from '$lib/components/ConfirmDialog.svelte';
  import GiveItemDialog from '$lib/components/players/GiveItemDialog.svelte';
  import PlayerIdentity from '$lib/components/players/PlayerIdentity.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { PlayerDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    connectionMeta,
    pingBars,
    pingClass,
    pingLabel,
    playerActivityLine,
    sortConnectedPlayers,
    sortPlayersByName,
    statisticsSummaryShort,
    storedDetailsLine,
    vitalsPercent,
    voiceLinesShort,
  } from '$lib/playerHelpers';
  import {
    avatarUrl,
    formatVitalPercent,
    navigate,
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
  let confirmOpen = $state(false);
  let cheatToggleInFlight = $state<string | null>(null);
  let confirmLoading = $state(false);
  let confirmTitle = $state('');
  let confirmMessage = $state('');
  let confirmLabel = $state('');
  let confirmAction = $state<(() => void | Promise<void>) | null>(null);
  let giveItemOpen = $state(false);
  let giveItemInitialRecipients = $state<string[]>([]);
  let giveItemDialogKey = $state(0);

  const isHost = $derived(dashboard.status.isHost);
  const blindMode = $derived(dashboard.playerBlindMode);
  const eligibleGiveItemPlayers = $derived(
    players.filter((player) => canGiveItem(player)),
  );

  $effect(() => {
    if (isHost && dashboard.status.isConnected && !stored) {
      void dashboard.loadItemCatalog();
    }
  });

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
        p.activityState,
        p.activityDetail,
      ]
        .map((v) => String(v ?? '').toLowerCase())
        .join(' ');
      return hay.includes(q);
    });
  });

  const pageCount = $derived(Math.max(1, Math.ceil(filtered.length / pageSize)));
  const pageItems = $derived(
    stored ? filtered.slice(page * pageSize, page * pageSize + pageSize) : filtered,
  );

  const showingFrom = $derived(
    filtered.length === 0 ? 0 : stored ? page * pageSize + 1 : 1,
  );
  const showingTo = $derived(
    stored ? Math.min(filtered.length, (page + 1) * pageSize) : filtered.length,
  );

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

  function showActivityLine(player: PlayerDto) {
    return !blindMode || player.isLocal;
  }

  function showPingLabel(player: PlayerDto) {
    const label = pingLabel(player, isHost, t);
    return label && label !== '—';
  }

  function showVitals(player: PlayerDto) {
    return isHost && !blindMode && player.playerUid && player.health != null;
  }

  function showStatistics(player: PlayerDto) {
    return !stored && isHost && !blindMode;
  }

  function canKickBan(player: PlayerDto) {
    return isHost && !player.isLocal && !stored;
  }

  function canHeal(player: PlayerDto) {
    return isHost && !blindMode && player.playerUid && player.isAlive;
  }

  function canRespawn(player: PlayerDto) {
    return isHost && !blindMode && !player.isAlive && !!player.playerUid;
  }

  function canGiveItem(player: PlayerDto) {
    return isHost && !blindMode && !!player.playerUid && player.isAlive && !stored;
  }

  function moderationActionLabel(action: string) {
    switch (action) {
      case 'kick':
        return t('dashboard.kick');
      case 'ban':
        return t('dashboard.ban');
      case 'unban':
        return t('dashboard.unban');
      case 'heal':
        return t('dashboard.heal');
      case 'respawn':
        return t('dashboard.respawn');
      default:
        return action;
    }
  }

  function openConfirm(options: {
    title: string;
    message: string;
    confirmLabel: string;
    onConfirm: () => void | Promise<void>;
  }) {
    confirmTitle = options.title;
    confirmMessage = options.message;
    confirmLabel = options.confirmLabel;
    confirmAction = options.onConfirm;
    confirmLoading = false;
    confirmOpen = true;
  }

  function closeConfirm() {
    if (confirmLoading) return;
    confirmOpen = false;
    confirmAction = null;
  }

  async function handleConfirm() {
    if (!confirmAction || confirmLoading) return;
    confirmLoading = true;
    try {
      await confirmAction();
      confirmOpen = false;
      confirmAction = null;
    } finally {
      confirmLoading = false;
    }
  }

  function openGiveItem(player: PlayerDto) {
    giveItemInitialRecipients = [String(player.steamId)];
    giveItemDialogKey += 1;
    giveItemOpen = true;
  }

  function requestModeration(player: PlayerDto, action: string) {
    const actionLabel = moderationActionLabel(action);
    const name = player.displayName;
    openConfirm({
      title: t('dashboard.confirm_action_title', { action: actionLabel }),
      message: t('dashboard.confirm_action_message', { name, steamId: player.steamId }),
      confirmLabel: actionLabel,
      onConfirm: async () => {
        try {
          const res = await Api.postAction(String(player.steamId), action);
          dashboard.showToast(res.message || t('api.done'));
        } catch (e) {
          dashboard.showToast(e instanceof Error ? e.message : String(e));
        }
      },
    });
  }

  async function toggleCheat(player: PlayerDto, action: 'godmode' | 'noclip') {
    const key = `${player.steamId}/${action}`;
    if (cheatToggleInFlight) return;
    cheatToggleInFlight = key;
    try {
      const res = await Api.postAction(String(player.steamId), action);
      dashboard.showToast(res.message || t('api.done'));
      if (typeof res.godMode === 'boolean') player.godMode = res.godMode;
      if (typeof res.noClip === 'boolean') player.noClip = res.noClip;
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      cheatToggleInFlight = null;
    }
  }

  function cheatToggleBusy(player: PlayerDto, action: 'godmode' | 'noclip') {
    return cheatToggleInFlight === `${player.steamId}/${action}`;
  }

  function requestDeletePlayerData(player: PlayerDto) {
    const name = player.displayName;
    openConfirm({
      title: t('dashboard.confirm_delete_title'),
      message: t('dashboard.confirm_delete_player', { name, steamId: player.steamId }),
      confirmLabel: t('dashboard.confirm_delete'),
      onConfirm: async () => {
        try {
          const res = await Api.deletePlayer(String(player.steamId));
          dashboard.showToast(res.message || t('api.done'));
          if (dashboard.route === 'player' && String(dashboard.steamId) === String(player.steamId)) {
            navigate('leaderboard');
          }
        } catch (e) {
          dashboard.showToast(e instanceof Error ? e.message : String(e));
        }
      },
    });
  }

  function connectionDetails(player: PlayerDto) {
    if (stored) {
      return { primary: storedDetailsLine(player, t), secondary: '' };
    }
    return {
      primary: connectionMeta(
        player,
        voiceLinesShort(player.voiceLineCount ?? 0, t),
      ),
      secondary: showActivityLine(player) ? playerActivityLine(player, t) : '',
    };
  }
</script>

<div class="data-table-card">
  <div class="data-table-toolbar">
    <span>
      {t('dashboard.table_showing', {
        from: showingFrom,
        to: showingTo,
        total: filtered.length,
      })}
    </span>
    {#if stored && pageCount > 1}
      <div class="data-table-pagination">
        <button type="button" class="btn btn-secondary btn-xs" disabled={page === 0} onclick={() => page--}>{t('dashboard.table_prev')}</button>
        <span class="data-table-page-num">{page + 1} / {pageCount}</span>
        <button type="button" class="btn btn-secondary btn-xs" disabled={page >= pageCount - 1} onclick={() => page++}>{t('dashboard.table_next')}</button>
      </div>
    {/if}
  </div>

  <div class="data-table-wrap">
    <table class="data-table">
      <thead>
        <tr>
          <th>{t('dashboard.table_player')}</th>
          {#if !stored}
            <th>{t('dashboard.table_ping')}</th>
          {/if}
          <th>{stored ? t('dashboard.table_details') : t('dashboard.table_connection')}</th>
          {#if !stored && isHost}
            <th>{t('dashboard.table_statistics')}</th>
            <th>{t('dashboard.table_vitals')}</th>
          {/if}
          {#if isHost}
            <th class="data-table-actions-col">{t('dashboard.table_actions')}</th>
          {/if}
        </tr>
      </thead>
      <tbody>
        {#each pageItems as player (player.steamId)}
          {@const details = connectionDetails(player)}
          {@const vitals = vitalsPercent(player)}
          {@const stats = statisticsSummaryShort(player, t)}
          <tr class="data-table-row {showAlive(player) ? (player.isAlive ? 'row-alive' : 'row-dead') : ''}">
            <td>
              <div class="data-table-player">
                <img
                  class="data-table-avatar"
                  src={avatarUrl(player.steamId)}
                  alt=""
                  onerror={(e) => ((e.currentTarget as HTMLImageElement).src = '/img/default-avatar.svg')}
                />
                <div class="min-w-0 flex-1">
                  <PlayerIdentity
                    steamId={player.steamId}
                    displayName={player.displayName}
                    profileLink={isHost}
                  >
                    {#snippet badges()}
                      {#if player.isHost}<span class="badge badge-host">{t('dashboard.badge_host')}</span>{/if}
                      {#if player.isLocal}<span class="badge badge-local">{t('dashboard.badge_you')}</span>{/if}
                      {#if player.isBanned}<span class="badge bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300">{t('dashboard.badge_banned')}</span>{/if}
                      {#if showAlive(player)}
                        <span class="badge {player.isAlive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' : 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300'}">
                          {player.isAlive ? t('dashboard.badge_alive') : t('dashboard.badge_dead')}
                        </span>
                      {/if}
                      {#if isHost && showActivityLine(player) && player.lateJoinLabel}
                        <span class="badge bg-violet-100 text-violet-800 dark:bg-violet-900/30 dark:text-violet-300">{player.lateJoinLabel}</span>
                      {/if}
                    {/snippet}
                  </PlayerIdentity>
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
                  {#if showPingLabel(player)}
                    <span class="data-table-muted">{pingLabel(player, isHost, t)}</span>
                  {/if}
                </div>
              </td>
            {/if}
            <td>
              <div class="data-table-cell-stack">
                {#if details.primary}
                  <span>{details.primary}</span>
                {/if}
                {#if details.secondary}
                  <span class="data-table-stats">{details.secondary}</span>
                {/if}
                {#if !details.primary && !details.secondary}
                  <span class="data-table-muted">—</span>
                {/if}
              </div>
            </td>
            {#if !stored && isHost}
              <td>
                {#if showStatistics(player)}
                  <div class="data-table-cell-stack data-table-cell-compact">
                    {#if stats.total}
                      <span class="data-table-stats" title={t('dashboard.statistics_total')}>
                        <span class="data-table-stat-label">{t('dashboard.statistics_total')}</span>
                        {stats.total}
                      </span>
                    {/if}
                    {#if stats.session}
                      <span class="data-table-stats" title={t('dashboard.statistics_session')}>
                        <span class="data-table-stat-label">{t('dashboard.statistics_session')}</span>
                        {stats.session}
                      </span>
                    {/if}
                    {#if !stats.total && !stats.session}
                      <span class="data-table-muted">—</span>
                    {/if}
                  </div>
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
                {/if}
              </td>
            {/if}
            {#if isHost}
              <td>
                <div class="data-table-actions">
                  {#if canKickBan(player)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => requestModeration(player, 'kick')}>{t('dashboard.kick')}</button>
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => requestModeration(player, player.isBanned ? 'unban' : 'ban')}>
                      {player.isBanned ? t('dashboard.unban') : t('dashboard.ban')}
                    </button>
                  {/if}
                  {#if canHeal(player)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => requestModeration(player, 'heal')}>{t('dashboard.heal')}</button>
                    <button
                      type="button"
                      class="btn btn-secondary btn-xs {player.godMode ? 'btn-active' : ''}"
                      title={t('dashboard.god_mode_title')}
                      disabled={cheatToggleBusy(player, 'godmode')}
                      onclick={() => toggleCheat(player, 'godmode')}
                    >
                      {t('dashboard.god_mode')} {player.godMode ? t('dashboard.on') : t('dashboard.off')}
                    </button>
                    <button
                      type="button"
                      class="btn btn-secondary btn-xs {player.noClip ? 'btn-active' : ''}"
                      title={t('dashboard.noclip_title')}
                      disabled={cheatToggleBusy(player, 'noclip')}
                      onclick={() => toggleCheat(player, 'noclip')}
                    >
                      {t('dashboard.noclip')} {player.noClip ? t('dashboard.on') : t('dashboard.off')}
                    </button>
                  {:else if canRespawn(player)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => requestModeration(player, 'respawn')}>{t('dashboard.respawn')}</button>
                  {/if}
                  {#if stored}
                    {#if player.isBanned}
                      <button type="button" class="btn btn-secondary btn-xs" onclick={() => requestModeration(player, 'unban')}>{t('dashboard.unban')}</button>
                    {/if}
                    <button type="button" class="btn btn-danger btn-xs" onclick={() => requestDeletePlayerData(player)}>{t('dashboard.delete_player')}</button>
                  {/if}
                  {#if canGiveItem(player)}
                    <button type="button" class="btn btn-secondary btn-xs" onclick={() => openGiveItem(player)}>
                      {t('dashboard.give_items')}
                    </button>
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
</div>

<ConfirmDialog
  bind:open={confirmOpen}
  title={confirmTitle}
  message={confirmMessage}
  confirmLabel={confirmLabel}
  loading={confirmLoading}
  onConfirm={handleConfirm}
  onCancel={closeConfirm}
/>

{#key giveItemDialogKey}
  <GiveItemDialog
    bind:open={giveItemOpen}
    eligiblePlayers={eligibleGiveItemPlayers}
    initialRecipients={giveItemInitialRecipients}
  />
{/key}
