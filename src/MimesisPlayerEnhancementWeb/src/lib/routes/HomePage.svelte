<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';

  const webFeatures = [
    { titleKey: 'home_web_players_title', descKey: 'home_web_players_desc', host: true },
    { titleKey: 'home_web_minimap_title', descKey: 'home_web_minimap_desc' },
    { titleKey: 'home_web_stats_title', descKey: 'home_web_stats_desc', host: true },
    { titleKey: 'home_web_settings_title', descKey: 'home_web_settings_desc', host: true },
  ] as const;

  const modFeatures = [
    { titleKey: 'home_mod_more_players_title', descKey: 'home_mod_more_players_desc' },
    { titleKey: 'home_mod_more_voices_title', descKey: 'home_mod_more_voices_desc' },
    { titleKey: 'home_mod_persistence_title', descKey: 'home_mod_persistence_desc' },
    { titleKey: 'home_mod_join_anytime_title', descKey: 'home_mod_join_anytime_desc' },
    { titleKey: 'home_mod_ui_title', descKey: 'home_mod_ui_desc' },
    { titleKey: 'home_mod_statistics_title', descKey: 'home_mod_statistics_desc' },
    { titleKey: 'home_mod_announcements_title', descKey: 'home_mod_announcements_desc' },
    { titleKey: 'home_mod_spawn_scaling_title', descKey: 'home_mod_spawn_scaling_desc' },
    { titleKey: 'home_mod_loot_title', descKey: 'home_mod_loot_desc' },
    { titleKey: 'home_mod_economy_title', descKey: 'home_mod_economy_desc' },
    { titleKey: 'home_mod_dungeon_time_title', descKey: 'home_mod_dungeon_time_desc' },
    { titleKey: 'home_mod_mimic_tuning_title', descKey: 'home_mod_mimic_tuning_desc' },
    { titleKey: 'home_mod_player_tuning_title', descKey: 'home_mod_player_tuning_desc' },
    { titleKey: 'home_mod_dungeon_randomizer_title', descKey: 'home_mod_dungeon_randomizer_desc' },
    { titleKey: 'home_mod_weather_title', descKey: 'home_mod_weather_desc' },
  ] as const;
</script>

<article class="home-page">
  <header class="home-hero">
    <img class="home-logo" src="/img/logo.png" alt="" width="64" height="64" />
    <div class="home-hero-text">
      <h1 class="home-heading">{t('dashboard.home_heading')}</h1>
      <p class="home-lead">{t('dashboard.home_lead')}</p>
    </div>
  </header>

  <div
    class="home-status-banner {dashboard.status.isConnected ? 'home-status-connected' : 'home-status-waiting'}"
    role="status"
  >
    <span class="home-status-dot" aria-hidden="true"></span>
    <div>
      <p class="home-status-title">
        {dashboard.status.isConnected
          ? t('dashboard.home_status_connected')
          : t('dashboard.home_status_waiting')}
      </p>
      {#if !dashboard.status.isConnected}
        <p class="home-status-hint">{t('dashboard.home_status_hint')}</p>
      {:else}
        <p class="home-status-hint">
          {dashboard.status.isHost ? t('dashboard.subtitle_host') : t('dashboard.subtitle_client')}
          {#if dashboard.status.saveSlotId >= 0}
            · {t('dashboard.subtitle_savegame', { slot: dashboard.status.saveSlotId })}
          {/if}
        </p>
      {/if}
    </div>
  </div>

  <section class="home-section">
    <div class="home-section-header">
      <h2 class="home-section-title">{t('dashboard.home_section_web')}</h2>
      <p class="home-section-lead">{t('dashboard.home_section_web_lead')}</p>
    </div>
    <div class="home-card-grid home-card-grid-dashboard">
      {#each webFeatures as feature (feature.titleKey)}
        <div class="home-feature-card card">
          <div class="home-feature-card-head">
            <h3 class="home-feature-card-title">{t(`dashboard.${feature.titleKey}`)}</h3>
            {#if 'host' in feature && feature.host}
              <span class="home-feature-badge">{t('dashboard.settings_host_only')}</span>
            {/if}
          </div>
          <p class="home-feature-card-desc">{t(`dashboard.${feature.descKey}`)}</p>
        </div>
      {/each}
    </div>
  </section>

  <section class="home-section">
    <div class="home-section-header">
      <h2 class="home-section-title">{t('dashboard.home_section_mod')}</h2>
      <p class="home-section-lead">{t('dashboard.home_section_mod_lead')}</p>
    </div>
    <div class="home-card-grid">
      {#each modFeatures as feature (feature.titleKey)}
        <div class="home-feature-card card">
          <h3 class="home-feature-card-title">{t(`dashboard.${feature.titleKey}`)}</h3>
          <p class="home-feature-card-desc">{t(`dashboard.${feature.descKey}`)}</p>
        </div>
      {/each}
    </div>
  </section>
</article>
