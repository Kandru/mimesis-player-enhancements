<script lang="ts">
  import type { ConfigEntryDto, ConfigSectionDto, SettingsDto } from '$lib/types';
  import Toggle from '$lib/components/Toggle.svelte';
  import ScopeBadges from '$lib/components/ScopeBadges.svelte';
  import SearchablePicker from '$lib/components/settings/SearchablePicker.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { parseCsv } from '$lib/listValue';
  import {
    buildDungeonPickerOptions,
    buildItemPickerOptions,
    buildVariantPickerOptions,
    buildWeatherPresetOptions,
    isSearchableSelectEntry,
  } from '$lib/pickerOptions';
  import {
    entryIsModified,
    entryScopes,
    featureEnabled,
    formatDefaultHint,
    formatGlobalHint,
  } from '$lib/settings';

  let {
    entry,
    section,
    settings,
    scope,
    editable,
    savingKey = '',
    onsave,
    onreset,
  }: {
    entry: ConfigEntryDto;
    section: ConfigSectionDto;
    settings: SettingsDto | null;
    scope: 'global' | 'save';
    editable: boolean;
    savingKey?: string;
    onsave: (value: string) => void;
    onreset: () => void;
  } = $props();

  function onChange(ev: Event) {
    const el = ev.currentTarget as HTMLInputElement | HTMLSelectElement;
    let value = el.value;
    if (entry.type === 'Boolean' && el instanceof HTMLInputElement && el.type === 'checkbox') {
      value = el.checked ? 'true' : 'false';
    }
    onsave(value);
  }

  const rowKey = $derived(`${section.id}/${entry.key}`);
  const isSaving = $derived(savingKey === rowKey || savingKey === `${section.id}/*`);
  const showReset = $derived(editable && entryIsModified(entry, scope));
  const resetTitle = $derived(
    scope === 'save' ? formatGlobalHint(entry) : formatDefaultHint(entry),
  );

  const boolChecked = $derived(entry.value === 'true' || entry.value === 'True');
  const selectValue = $derived.by(() => {
    if (entry.inputKind !== 'Select') {
      return entry.value;
    }

    const options = entry.selectOptions;
    if (options.length === 0) {
      return entry.value;
    }

    return options.some((option) => option.value === entry.value)
      ? entry.value
      : options[0].value;
  });
  const featureOff = $derived(!featureEnabled(section, settings));
  const hostReadOnlyHint = $derived(
    !editable && !featureOff && !entry.hasLocalEffect ? t('dashboard.settings_host_only_hint') : undefined,
  );

  const MULTI_PICKER_KINDS = ['ItemIdList', 'DungeonIdList', 'WeatherPresetList', 'VariantIdList'];
  const isMultiPicker = $derived(MULTI_PICKER_KINDS.includes(entry.inputKind));
  const isSearchableSelect = $derived(isSearchableSelectEntry(entry));

  const pickerOptions = $derived.by(() => {
    switch (entry.inputKind) {
      case 'ItemIdList':
        return buildItemPickerOptions(dashboard.itemCatalog, t);
      case 'DungeonIdList':
        return buildDungeonPickerOptions(dashboard.dungeonCatalog);
      case 'WeatherPresetList':
        return buildWeatherPresetOptions(t);
      default:
        return isMultiPicker || isSearchableSelect
          ? buildVariantPickerOptions(entry.selectOptions)
          : [];
    }
  });

  const listValues = $derived(isMultiPicker ? parseCsv(entry.value) : []);
  // Item/dungeon catalogs come from live game data; without them the picker cannot offer options.
  const catalogUnavailable = $derived(
    (entry.inputKind === 'ItemIdList' || entry.inputKind === 'DungeonIdList')
    && pickerOptions.length === 0,
  );
  const pickerDisabled = $derived(!editable || isSaving || catalogUnavailable);
  const pickerPlaceholder = $derived(
    entry.inputKind === 'VariantIdList' ? t('dashboard.picker_empty_means_all') : '',
  );
</script>

<div
  class="settings-entry {editable ? '' : 'settings-entry-readonly'} {showReset ? 'settings-entry-modified' : ''} {featureOff ? 'settings-entry-disabled' : ''} {isSaving ? 'settings-entry-saving' : ''}"
>
  <div class="settings-entry-main">
    <div class="settings-entry-header">
      <label class="settings-entry-title" for="{section.id}-{entry.key}">{entry.title}</label>
      <ScopeBadges scopes={entryScopes(entry)} size="sm" />
      {#if scope === 'save' && entry.isOverridden}
        <span class="badge bg-violet-50 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300">{t('dashboard.settings_overridden')}</span>
      {/if}
      {#if featureOff}
        <span class="badge bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300" title={t('dashboard.settings_feature_disabled_hint')}>
          {t('dashboard.settings_feature_disabled')}
        </span>
      {/if}
    </div>
    {#if entry.description}
      <p class="settings-entry-desc">{entry.description}</p>
    {/if}
    {#if catalogUnavailable}
      <p class="settings-entry-hint">{t('dashboard.picker_catalog_unavailable')}</p>
    {/if}
  </div>

  <div class="settings-entry-actions" title={hostReadOnlyHint}>
    {#if entry.type === 'Boolean'}
      <Toggle
        checked={boolChecked}
        disabled={!editable || isSaving}
        label={entry.title}
        onchange={(checked) => onsave(checked ? 'true' : 'false')}
      />
    {:else if isMultiPicker}
      <SearchablePicker
        id="{section.id}-{entry.key}"
        multiple
        reorderable={entry.inputKind === 'WeatherPresetList'}
        options={pickerOptions}
        values={listValues}
        disabled={pickerDisabled}
        placeholder={pickerPlaceholder}
        onsave={onsave}
      />
    {:else if isSearchableSelect}
      <SearchablePicker
        id="{section.id}-{entry.key}"
        options={pickerOptions}
        value={selectValue}
        disabled={!editable || isSaving || pickerOptions.length === 0}
        onsave={onsave}
      />
    {:else if entry.inputKind === 'Select'}
      <select id="{section.id}-{entry.key}" class="input max-w-md" value={selectValue} disabled={!editable || isSaving} onchange={onChange}>
        {#each entry.selectOptions as opt (opt.value)}
          <option value={opt.value}>{opt.label}</option>
        {/each}
      </select>
    {:else}
      <input
        id="{section.id}-{entry.key}"
        class="input max-w-md"
        type={entry.type === 'Int32' || entry.type === 'Single' || entry.type === 'Double' ? 'number' : 'text'}
        value={entry.value}
        min={entry.minValue}
        max={entry.maxValue}
        disabled={!editable || isSaving}
        onchange={onChange}
      />
    {/if}

    {#if showReset}
      <button
        type="button"
        class="btn btn-secondary btn-xs"
        disabled={isSaving}
        title={resetTitle}
        onclick={() => onreset()}
      >
        {t('dashboard.settings_reset')}
      </button>
    {/if}
  </div>
</div>
