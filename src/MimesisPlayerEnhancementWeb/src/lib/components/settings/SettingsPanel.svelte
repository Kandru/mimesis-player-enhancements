<script lang="ts">
  import Api from '$lib/api';
  import SettingsEntry from './SettingsEntry.svelte';
  import Toggle from '$lib/components/Toggle.svelte';
  import ScopeBadges from '$lib/components/ScopeBadges.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { ConfigSectionDto, SettingsDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    canEditEntry,
    entryEditable,
    featureEnabled,
    groupConfigEntries,
    sectionHasModifiedEntries,
    sectionHasVisibleEntries,
    sectionResettableEntries,
    sectionScopes,
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
  const searchContext = $derived({
    itemCatalog: dashboard.itemCatalog,
    dungeonCatalog: dashboard.dungeonCatalog,
  });
  const isGuest = $derived(!dashboard.status.isHost && dashboard.status.isConnected);

  const sections = $derived.by(() => {
    if (!settings) return [];
    return settings.sections.filter((section) =>
      sectionHasVisibleEntries(section, settings, query, searchContext),
    );
  });

  const activeSection = $derived.by(() => {
    if (sections.length === 0) return null;
    const selected = dashboard.selectedSettingsSectionId;
    return sections.find((s) => s.id === selected) ?? sections[0];
  });

  const activeEntryGroups = $derived.by(() => {
    if (!activeSection || !settings) return [];
    return groupConfigEntries(activeSection, settings, query, searchContext);
  });

  const activeSectionResettable = $derived.by(() => {
    if (!activeSection || !settings) return [];
    return sectionResettableEntries(
      activeSection,
      settings,
      scope,
      query,
      dashboard.status.isHost,
      dashboard.status.isConnected,
      searchContext,
    );
  });

  const sectionResetSaving = $derived(
    activeSection != null && dashboard.savingSettingKey === `${activeSection.id}/*`,
  );

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

  async function reloadSettings() {
    if (scope === 'global') await dashboard.loadGlobalSettings(false, true);
    else {
      await dashboard.loadSaveSettings(false, true);
      await dashboard.loadSaveProfileData(true);
    }
  }

  async function saveEntry(sectionId: string, key: string, value: string) {
    dashboard.savingSettingKey = `${sectionId}/${key}`;
    try {
      const api = scope === 'global' ? Api.updateGlobalSetting : Api.updateSaveSetting;
      const result = await api(sectionId, key, value);
      dashboard.showToast((result as { message?: string }).message || t('api.done'));
      await reloadSettings();
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      dashboard.savingSettingKey = '';
    }
  }

  async function resetSetting(sectionId: string, key?: string) {
    dashboard.savingSettingKey = key ? `${sectionId}/${key}` : `${sectionId}/*`;
    try {
      const api = scope === 'global' ? Api.resetGlobalSetting : Api.resetSaveSetting;
      const result = await api(sectionId, key);
      const count = result.resetCount ?? 0;
      const target =
        scope === 'global' ? t('dashboard.reset_target_defaults') : t('dashboard.reset_target_global');
      dashboard.showToast(
        count > 0
          ? t('dashboard.reset_settings_toast', { count: String(count), target })
          : result.message || t('api.done'),
      );
      await reloadSettings();
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

  {#if dashboard.loadingSettings && !settings}
    <p class="text-sm text-gray-500 dark:text-gray-300">{t('dashboard.loading')}</p>
  {:else if !settings}
    <p class="text-sm text-gray-500 dark:text-gray-300">{t('dashboard.settings_unavailable')}</p>
  {:else if sections.length === 0}
    <p class="text-sm text-gray-500 dark:text-gray-300">{t('dashboard.settings_no_results')}</p>
  {:else}
    <div class="settings-layout">
      <nav class="settings-nav" aria-label={t('dashboard.settings_sections_nav')}>
        {#each sections as section (section.id)}
          <div
            class="settings-nav-item {activeSection?.id === section.id ? 'settings-nav-item-active' : ''} {sectionHasModifiedEntries(section, settings, scope) ? 'settings-nav-item-modified' : ''}"
            role="presentation"
          >
            <button
              type="button"
              class="settings-nav-title"
              onclick={() => selectSection(section)}
            >
              {section.title}
            </button>
            <div class="settings-nav-badges">
              <ScopeBadges scopes={sectionScopes(section, settings)} size="sm" />
            </div>
            <div class="settings-nav-toggle">
              {#if section.featureToggle}
                <Toggle
                  checked={sectionToggleChecked(section)}
                  disabled={!sectionToggleEditable(section)}
                  label={section.title}
                  onchange={(enabled) => toggleFeature(section, enabled)}
                />
              {/if}
            </div>
          </div>
        {/each}
      </nav>

      {#if activeSection}
        <div class="settings-content">
          <section class="settings-section-card">
            <div class="settings-section-header">
              <div class="settings-section-heading">
                <div class="settings-section-title-row">
                  <h3 class="settings-section-title">{activeSection.title}</h3>
                  <ScopeBadges scopes={sectionScopes(activeSection, settings)} size="sm" />
                </div>
                {#if activeSection.description}
                  <p class="settings-section-description">{activeSection.description}</p>
                {/if}
              </div>
              {#if activeSectionResettable.length > 0}
                <button
                  type="button"
                  class="btn btn-secondary btn-xs"
                  disabled={sectionResetSaving}
                  title={scope === 'save'
                    ? t('dashboard.settings_reset_all_global_title')
                    : t('dashboard.settings_reset_all_defaults_title')}
                  onclick={() => resetSetting(activeSection.id)}
                >
                  {t('dashboard.settings_reset_all')}
                </button>
              {/if}
            </div>
            <div class="settings-entries">
              {#each activeEntryGroups as group (group.id || group.entries[0]?.key)}
                <div class="settings-entry-group">
                  {#if group.label}
                    <h4 class="settings-entry-group-title">{group.label}</h4>
                  {/if}
                  {#each group.entries as entry (entry.key)}
                    <SettingsEntry
                      {entry}
                      section={activeSection}
                      {settings}
                      {scope}
                      savingKey={dashboard.savingSettingKey}
                      editable={entryEditable(
                        activeSection,
                        entry,
                        settings,
                        scope,
                        dashboard.status.isHost,
                        dashboard.status.isConnected,
                      )}
                      onsave={(value) => saveEntry(activeSection.id, entry.key, value)}
                      onreset={() => resetSetting(activeSection.id, entry.key)}
                    />
                  {/each}
                </div>
              {/each}
            </div>
          </section>
        </div>
      {/if}
    </div>
  {/if}
</div>
