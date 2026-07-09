<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';

  const subtitle = $derived.by(() => {
    if (dashboard.apiError) return t('dashboard.subtitle_api_error');
    if (!dashboard.status.isConnected) {
      return dashboard.status.modVersion
        ? `v${dashboard.status.modVersion} · ${t('dashboard.subtitle_waiting')}`
        : t('dashboard.subtitle_waiting');
    }
    const parts: string[] = [];
    if (dashboard.status.modVersion) parts.push(`v${dashboard.status.modVersion}`);
    parts.push(
      dashboard.status.isHost ? t('dashboard.subtitle_host') : t('dashboard.subtitle_client'),
    );
    if (dashboard.status.saveSlotId >= 0) {
      parts.push(t('dashboard.subtitle_savegame', { slot: dashboard.status.saveSlotId }));
    }
    return parts.join(' · ');
  });
</script>

<header
  class="sticky top-0 z-30 flex h-[var(--header-height)] items-center justify-between gap-4 border-b border-gray-200 bg-white/90 px-4 backdrop-blur dark:border-gray-700 dark:bg-gray-900/90 md:px-6"
>
  <div class="min-w-0">
    <h1 class="truncate text-lg font-semibold">
      {dashboard.status.lobbyName?.trim() || t('dashboard.title_default')}
    </h1>
    <p class="truncate text-xs text-gray-500">{subtitle}</p>
  </div>

  <div class="flex shrink-0 items-center gap-2">
    {#if dashboard.status.isConnected}
      <span class="badge {dashboard.status.isHost ? 'badge-host' : 'badge-guest'}">
        {dashboard.status.isHost ? t('dashboard.role_host') : t('dashboard.role_guest')}
      </span>
    {/if}

    {#if dashboard.status.isHost && dashboard.status.isConnected}
      <label class="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-300">
        <input
          type="checkbox"
          class="rounded"
          checked={dashboard.playerBlindModeUserEnabled}
          onchange={() => dashboard.togglePlayerBlindMode()}
        />
        {t('dashboard.blind_mode')}
      </label>
    {/if}

    <button type="button" class="btn btn-secondary text-xs" onclick={() => dashboard.toggleDarkMode()}>
      {dashboard.darkMode ? t('dashboard.theme_light') : t('dashboard.theme_dark')}
    </button>
  </div>
</header>
