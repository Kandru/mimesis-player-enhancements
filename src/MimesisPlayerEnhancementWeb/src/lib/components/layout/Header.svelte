<script lang="ts">
  import Toggle from '$lib/components/Toggle.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { getHeaderSearchPlaceholder, isHeaderSearchVisible } from '$lib/headerSearch';
  import { getPageDescription, getPageTitle } from '$lib/pageTitles';

  const pageTitle = $derived.by(() =>
    getPageTitle(
      dashboard.route,
      dashboard.settingsSubRoute,
      dashboard.playerStats?.displayName || dashboard.playerStats?.steamId,
    ),
  );

  const pageDescription = $derived.by(() =>
    getPageDescription(dashboard.route, dashboard.settingsSubRoute),
  );

  const showSearch = $derived(
    isHeaderSearchVisible(
      dashboard.route,
      dashboard.settingsSubRoute,
      dashboard.saveProfile?.profile?.mode ?? '',
    ),
  );

  const searchPlaceholder = $derived(
    getHeaderSearchPlaceholder(
      dashboard.route,
      dashboard.settingsSubRoute,
      dashboard.saveProfile?.profile?.mode ?? '',
    ),
  );

  const statusVariant = $derived.by(() => {
    if (dashboard.apiError) return 'error';
    if (!dashboard.status.isConnected) return 'waiting';
    return 'connected';
  });

  const statusText = $derived.by(() => {
    if (dashboard.apiError) return t('dashboard.subtitle_api_error');
    if (!dashboard.status.isConnected) return t('dashboard.home_status_waiting');
    const parts = [t('dashboard.home_status_connected')];
    parts.push(
      dashboard.status.isHost ? t('dashboard.subtitle_host') : t('dashboard.subtitle_client'),
    );
    if (dashboard.status.saveSlotId >= 0) {
      parts.push(`#${dashboard.status.saveSlotId + 1}`);
    }
    return parts.join(' · ');
  });

  const blindModeDisabled = $derived(!dashboard.status.isConnected);
</script>

<header class="app-header">
  <div class="header-start">
    <button
      type="button"
      class="header-menu-btn"
      aria-label={t('dashboard.menu_open')}
      onclick={() => (dashboard.sidebarOpen = !dashboard.sidebarOpen)}
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
        <line x1="3" y1="6" x2="21" y2="6" />
        <line x1="3" y1="12" x2="21" y2="12" />
        <line x1="3" y1="18" x2="21" y2="18" />
      </svg>
    </button>
    <div class="header-title-block">
      <h1 class="header-title">{pageTitle}</h1>
      {#if pageDescription}
        <p class="header-subtitle">{pageDescription}</p>
      {/if}
    </div>
  </div>

  {#if showSearch}
    <label class="header-search">
      <span class="header-search-icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
      </span>
      <input
        class="header-search-input"
        type="search"
        placeholder={searchPlaceholder}
        bind:value={dashboard.headerSearchQuery}
        aria-label={searchPlaceholder}
      />
    </label>
  {/if}

  <div class="header-actions">
    <div
      class="header-status-btn header-status-{statusVariant}"
      role="status"
      title={statusText}
    >
      <span class="header-status-dot" aria-hidden="true"></span>
      <span class="header-status-text">{statusText}</span>
    </div>

    <div
      class="header-blind-toggle {blindModeDisabled ? 'header-blind-toggle-disabled' : ''}"
      title={blindModeDisabled ? t('dashboard.nav_lobby_disabled_hint') : t('dashboard.blind_mode_title')}
    >
      <span class="header-blind-label">{t('dashboard.blind_mode')}</span>
      <Toggle
        checked={dashboard.playerBlindModeEnabled}
        disabled={blindModeDisabled}
        label={t('dashboard.blind_mode')}
        onchange={() => dashboard.togglePlayerBlindMode()}
      />
    </div>

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
