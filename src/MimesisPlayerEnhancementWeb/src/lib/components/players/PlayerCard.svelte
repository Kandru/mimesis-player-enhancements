<script lang="ts">
  import Api from '$lib/api';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { PlayerDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import { avatarUrl, formatVitalPercent, isValidSteamId, navigate, steamProfileUrl } from '$lib/utils';

  let { player, stored = false }: { player: PlayerDto; stored?: boolean } = $props();

  const canModerate = $derived(dashboard.status.isHost && !player.isLocal && !stored);
  const showAlive = $derived(!dashboard.playerBlindMode || player.isLocal);
  const showVitals = $derived(
    dashboard.status.isHost && !dashboard.playerBlindMode && player.playerUid && player.health != null,
  );

  function vitals() {
    const healthPercent =
      player.maxHealth && player.maxHealth > 0
        ? (Number(player.health) / Number(player.maxHealth)) * 100
        : null;
    return { health: healthPercent, toxic: player.toxicPercent ?? null };
  }

  async function moderate(action: string) {
    if (!confirm(t('dashboard.confirm_moderation', { action, steamId: player.steamId }))) return;
    try {
      const res = await Api.postAction(String(player.steamId), action);
      dashboard.showToast(res.message || t('api.done'));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  async function toggleCheat(action: 'godmode' | 'noclip') {
    try {
      const res = await Api.postAction(String(player.steamId), action);
      dashboard.showToast(res.message || t('api.done'));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }
</script>

<article class="card flex flex-col overflow-hidden">
  <div class="flex items-start gap-3 p-4">
    <img
      src={avatarUrl(player.steamId)}
      alt=""
      class="h-12 w-12 rounded-full bg-gray-200 object-cover"
      onerror={(e) => ((e.currentTarget as HTMLImageElement).src = '/img/default-avatar.svg')}
    />
    <div class="min-w-0 flex-1">
      <div class="flex flex-wrap items-center gap-2">
        <a
          class="truncate font-semibold hover:text-[var(--brand)]"
          href={steamProfileUrl(player.steamId)}
          target="_blank"
          rel="noopener noreferrer"
        >
          {player.displayName || player.steamId}
        </a>
        {#if player.isHost}<span class="badge badge-host">Host</span>{/if}
        {#if player.isLocal}<span class="badge badge-local">Local</span>{/if}
        {#if showAlive}
          <span class="badge {player.isAlive ? 'bg-green-100 text-green-800' : 'bg-gray-200 text-gray-700'}">
            {player.isAlive ? t('dashboard.alive') : t('dashboard.dead')}
          </span>
        {/if}
      </div>
      <p class="mt-1 text-xs text-gray-500">
        {#if player.playerUid}#{player.playerUid}{/if}
        {#if player.connectionRole} · {player.connectionRole}{/if}
      </p>
    </div>
  </div>

  {#if showVitals}
    {@const v = vitals()}
    <div class="space-y-2 border-t border-gray-100 px-4 py-3 dark:border-gray-700">
      {#if v.health != null}
        <div>
          <div class="mb-1 flex justify-between text-xs"><span>{t('dashboard.health')}</span><span>{formatVitalPercent(v.health)}</span></div>
          <div class="h-2 rounded-full bg-gray-200 dark:bg-gray-700"><div class="h-2 rounded-full bg-green-500" style="width:{Math.min(100, v.health)}%"></div></div>
        </div>
      {/if}
      {#if v.toxic != null}
        <div>
          <div class="mb-1 flex justify-between text-xs"><span>{t('dashboard.toxic')}</span><span>{formatVitalPercent(v.toxic)}</span></div>
          <div class="h-2 rounded-full bg-gray-200 dark:bg-gray-700"><div class="h-2 rounded-full bg-amber-500" style="width:{Math.min(100, v.toxic)}%"></div></div>
        </div>
      {/if}
    </div>
  {/if}

  <div class="mt-auto flex flex-wrap gap-2 border-t border-gray-100 p-4 dark:border-gray-700">
    {#if dashboard.status.isHost && isValidSteamId(player.steamId)}
      <button type="button" class="btn btn-secondary text-xs" onclick={() => navigate(`player/${player.steamId}`)}>
        {t('dashboard.view_stats')}
      </button>
    {/if}
    {#if canModerate}
      <button type="button" class="btn btn-secondary text-xs" onclick={() => moderate('kick')}>{t('dashboard.kick')}</button>
      <button type="button" class="btn btn-secondary text-xs" onclick={() => moderate(player.isBanned ? 'unban' : 'ban')}>
        {player.isBanned ? t('dashboard.unban') : t('dashboard.ban')}
      </button>
      {#if !dashboard.playerBlindMode && player.playerUid}
        {#if player.isAlive}
          <button type="button" class="btn btn-secondary text-xs" onclick={() => moderate('heal')}>{t('dashboard.heal')}</button>
          <button type="button" class="btn btn-secondary text-xs" onclick={() => toggleCheat('godmode')}>God</button>
          <button type="button" class="btn btn-secondary text-xs" onclick={() => toggleCheat('noclip')}>Noclip</button>
        {:else}
          <button type="button" class="btn btn-secondary text-xs" onclick={() => moderate('respawn')}>{t('dashboard.respawn')}</button>
        {/if}
      {/if}
    {/if}
  </div>
</article>
