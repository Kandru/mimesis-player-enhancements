<script lang="ts">
  import Toggle from '$lib/components/Toggle.svelte';
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

<header class="app-header">
  <div class="flex min-w-0 items-center gap-3">
    <button
      type="button"
      class="header-icon-btn lg:hidden"
      aria-label="Open menu"
      onclick={() => (dashboard.mobileSidebarOpen = true)}
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
    </button>
    <div class="min-w-0">
      <h1 class="header-title">
        {dashboard.status.lobbyName?.trim() || t('dashboard.title_default')}
      </h1>
      <p class="header-subtitle">{subtitle}</p>
    </div>
  </div>

  <div class="header-actions">
    {#if dashboard.status.isConnected}
      <span class="badge {dashboard.status.isHost ? 'badge-host' : 'badge-guest'}">
        {dashboard.status.isHost ? t('dashboard.role_host') : t('dashboard.role_guest')}
      </span>
    {/if}

    {#if dashboard.status.isHost && dashboard.status.isConnected}
      <div class="header-blind-toggle" title={t('dashboard.blind_mode_title')}>
        <span class="header-blind-label">{t('dashboard.blind_mode')}</span>
        <Toggle
          checked={dashboard.playerBlindModeUserEnabled}
          label={t('dashboard.blind_mode')}
          onchange={() => dashboard.togglePlayerBlindMode()}
        />
      </div>
    {/if}

    <button
      type="button"
      class="header-icon-btn"
      title={dashboard.darkMode ? t('dashboard.theme_light') : t('dashboard.theme_dark')}
      onclick={() => dashboard.toggleDarkMode()}
    >
      {#if dashboard.darkMode}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
      {:else}
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
      {/if}
    </button>
  </div>
</header>
