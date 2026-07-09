<script lang="ts">
  import Api from '$lib/api';
  import SettingsEntry from './SettingsEntry.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { ConfigSectionDto, SettingsDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    canEditEntry,
    entryVisible,
    featureEnabled,
    matchesSettingsQuery,
    sectionHasVisibleEntries,
  } from '$lib/settings';

  let {
    settings,
    scope,
    heading,
    intro,
    guestFilter = false,
    guestSectionVisible,
  }: {
    settings: SettingsDto | null;
    scope: 'global' | 'save';
    heading: string;
    intro: string;
    guestFilter?: boolean;
    guestSectionVisible?: (section: ConfigSectionDto) => boolean;
  } = $props();

  const query = $derived(dashboard.settingsQuery.trim().toLowerCase());

  const sections = $derived.by(() => {
    if (!settings) return [];
    return settings.sections.filter((section) => {
      if (guestFilter && guestSectionVisible && !guestSectionVisible(section)) return false;
      return sectionHasVisibleEntries(section, settings, query);
    });
  });

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
</script>

<div class="space-y-6">
  <div>
    <h2 class="text-xl font-semibold">{heading}</h2>
    <p class="mt-1 text-sm text-gray-600 dark:text-gray-300">{intro}</p>
    {#if settings?.configPath}
      <p class="mt-2 text-xs text-gray-500">{settings.configPath}</p>
    {/if}
  </div>

  <input
    class="input max-w-md"
    placeholder={t('dashboard.settings_search_placeholder')}
    bind:value={dashboard.settingsQuery}
  />

  {#if dashboard.loadingSettings}
    <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
  {:else if !settings}
    <p class="text-sm text-gray-500">{t('dashboard.settings_unavailable')}</p>
  {:else}
    {#each sections as section (section.id)}
      <section class="card p-4 {guestFilter && !section.entries.some((e) => e.hasLocalEffect) && !section.featureToggle?.hasLocalEffect ? 'opacity-80' : ''}">
        <h3 class="mb-3 text-lg font-medium">{section.title}</h3>
        {#if section.featureToggle}
          <SettingsEntry
            entry={section.featureToggle}
            {section}
            {scope}
            editable={canEditEntry(section.featureToggle, scope, dashboard.status.isHost, dashboard.status.isConnected)}
            onsave={(value) => saveEntry(section.id, section.featureToggle!.key, value)}
          />
        {/if}
        {#if featureEnabled(section, settings)}
          <div class="space-y-4">
            {#each section.entries as entry (entry.key)}
              {#if entryVisible(section, entry, settings) && matchesSettingsQuery(entry, section.title, query)}
                <SettingsEntry
                  {entry}
                  {section}
                  {scope}
                  editable={canEditEntry(entry, scope, dashboard.status.isHost, dashboard.status.isConnected)}
                  onsave={(value) => saveEntry(section.id, entry.key, value)}
                />
              {/if}
            {/each}
          </div>
        {/if}
      </section>
    {/each}
  {/if}
</div>
