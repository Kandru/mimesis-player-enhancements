<script lang="ts">
  import Api from '$lib/api';
  import SettingsEntry from './SettingsEntry.svelte';
  import Toggle from '$lib/components/Toggle.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { ConfigSectionDto, SettingsDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    canEditEntry,
    entryEditable,
    entryVisible,
    featureEnabled,
    matchesSettingsQuery,
    sectionHasVisibleEntries,
  } from '$lib/settings';

  let {
    settings,
    scope,
    intro,
  }: {
    settings: SettingsDto | null;
    scope: 'global' | 'save';
    intro: string;
  } = $props();

  const query = $derived(dashboard.headerSearchQuery.trim().toLowerCase());
  const isGuest = $derived(!dashboard.status.isHost && dashboard.status.isConnected);

  const sections = $derived.by(() => {
    if (!settings) return [];
    return settings.sections.filter((section) =>
      sectionHasVisibleEntries(section, settings, query),
    );
  });

  const activeSection = $derived.by(() => {
    if (sections.length === 0) return null;
    const selected = dashboard.selectedSettingsSectionId;
    return sections.find((s) => s.id === selected) ?? sections[0];
  });

  $effect(() => {
    if (!sections.length) {
      dashboard.selectedSettingsSectionId = '';
      return;
    }
    if (!sections.some((s) => s.id === dashboard.selectedSettingsSectionId)) {
      dashboard.selectedSettingsSectionId = sections[0].id;
    }
  });

  function selectSection(section: ConfigSectionDto) {
    dashboard.selectedSettingsSectionId = section.id;
  }

  async function saveEntry(sectionId: string, key: string, value: string) {
    dashboard.savingSettingKey = `${sectionId}/${key}`;
    try {
      const api = scope === 'global' ? Api.updateGlobalSetting : Api.updateSaveSetting;
      const result = await api(sectionId, key, value);
      dashboard.showToast((result as { message?: string }).message || t('api.done'));
      if (scope === 'global') dashboard.settingsGlobal = await Api.getGlobalSettings();
      else dashboard.settingsSave = await Api.getSaveSettings();
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      dashboard.savingSettingKey = '';
    }
  }

  function toggleFeature(section: ConfigSectionDto, enabled: boolean) {
    if (!section.featureToggle) return;
    void saveEntry(section.id, section.featureToggle.key, enabled ? 'true' : 'false');
  }

  function sectionToggleChecked(section: ConfigSectionDto) {
    return featureEnabled(section, settings);
  }

  function sectionToggleEditable(section: ConfigSectionDto) {
    if (!section.featureToggle) return false;
    return canEditEntry(
      section.featureToggle,
      scope,
      dashboard.status.isHost,
      dashboard.status.isConnected,
    );
  }
</script>

<div class="settings-page">
  <div class="settings-panel-header">
    <p class="settings-panel-intro">{intro}</p>
    {#if settings?.configPath}
      <p class="settings-panel-path">{settings.configPath}</p>
    {/if}
  </div>

  {#if isGuest}
    <div class="settings-guest-banner">{t('dashboard.settings_guest_readonly')}</div>
  {/if}

  {#if dashboard.loadingSettings}
    <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
  {:else if !settings}
    <p class="text-sm text-gray-500">{t('dashboard.settings_unavailable')}</p>
  {:else if sections.length === 0}
    <p class="text-sm text-gray-500">{t('dashboard.settings_no_results')}</p>
  {:else}
    <div class="settings-layout">
      <nav class="settings-nav" aria-label={t('dashboard.settings_sections_nav')}>
        {#each sections as section (section.id)}
          <div
            class="settings-nav-item {activeSection?.id === section.id ? 'settings-nav-item-active' : ''}"
            role="presentation"
          >
            <button
              type="button"
              class="settings-nav-label border-0 bg-transparent p-0 text-left"
              onclick={() => selectSection(section)}
            >
              {section.title}
            </button>
            {#if section.featureToggle}
              <Toggle
                checked={sectionToggleChecked(section)}
                disabled={!sectionToggleEditable(section)}
                label={section.title}
                onchange={(enabled) => toggleFeature(section, enabled)}
              />
            {/if}
          </div>
        {/each}
      </nav>

      {#if activeSection}
        <div class="settings-content">
          <section class="settings-section-card">
            <h3 class="settings-section-title">{activeSection.title}</h3>
            <div class="settings-entries">
              {#if activeSection.featureToggle && matchesSettingsQuery(activeSection.featureToggle, activeSection.title, query)}
                <SettingsEntry
                  entry={activeSection.featureToggle}
                  section={activeSection}
                  {settings}
                  {scope}
                  editable={entryEditable(
                    activeSection,
                    activeSection.featureToggle,
                    settings,
                    scope,
                    dashboard.status.isHost,
                    dashboard.status.isConnected,
                  )}
                  onsave={(value) => saveEntry(activeSection.id, activeSection.featureToggle!.key, value)}
                />
              {/if}
              {#each activeSection.entries as entry (entry.key)}
                {#if entryVisible(activeSection, entry, settings) && matchesSettingsQuery(entry, activeSection.title, query)}
                  <SettingsEntry
                    {entry}
                    section={activeSection}
                    {settings}
                    {scope}
                    editable={entryEditable(
                      activeSection,
                      entry,
                      settings,
                      scope,
                      dashboard.status.isHost,
                      dashboard.status.isConnected,
                    )}
                    onsave={(value) => saveEntry(activeSection.id, entry.key, value)}
                  />
                {/if}
              {/each}
            </div>
          </section>
        </div>
      {/if}
    </div>
  {/if}
</div>
