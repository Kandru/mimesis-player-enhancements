<script lang="ts">
  import type { ConfigEntryDto, ConfigSectionDto, SettingsDto } from '$lib/types';
  import Toggle from '$lib/components/Toggle.svelte';
  import { t } from '$lib/i18n';
  import {
    entryIsModified,
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
  const resetLabel = $derived(
    scope === 'save' ? t('dashboard.settings_reset_global') : t('dashboard.settings_reset_default'),
  );
  const resetTitle = $derived(
    scope === 'save' ? formatGlobalHint(entry) : formatDefaultHint(entry),
  );

  const boolChecked = $derived(entry.value === 'true' || entry.value === 'True');
  const featureOff = $derived(!featureEnabled(section, settings));
</script>

<div
  class="settings-entry {editable ? '' : 'settings-entry-readonly'} {featureOff ? 'settings-entry-disabled' : ''} {isSaving ? 'settings-entry-saving' : ''}"
>
  <div class="settings-entry-main">
    <div class="settings-entry-header">
      <label class="settings-entry-title" for="{section.id}-{entry.key}">{entry.title}</label>
      {#if entry.hasLocalEffect}
        <span class="badge badge-local" title={t('dashboard.settings_local_hint')}>{t('dashboard.settings_local_badge')}</span>
      {/if}
      {#if scope === 'save' && entry.isOverridden}
        <span class="badge bg-violet-50 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300">{t('dashboard.settings_overridden')}</span>
      {/if}
      {#if !editable}
        <span class="badge bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300" title={featureOff ? t('dashboard.settings_feature_disabled_hint') : t('dashboard.settings_host_only_hint')}>
          {featureOff ? t('dashboard.settings_feature_disabled') : t('dashboard.settings_host_only')}
        </span>
      {/if}
    </div>
    {#if entry.description}
      <p class="settings-entry-desc">{entry.description}</p>
    {/if}
  </div>

  <div class="settings-entry-actions">
    {#if entry.type === 'Boolean'}
      <Toggle
        checked={boolChecked}
        disabled={!editable || isSaving}
        label={entry.title}
        onchange={(checked) => onsave(checked ? 'true' : 'false')}
      />
    {:else if entry.inputKind === 'Select'}
      <select id="{section.id}-{entry.key}" class="input max-w-md" value={entry.value} disabled={!editable || isSaving} onchange={onChange}>
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
        {resetLabel}
      </button>
    {/if}
  </div>
</div>
