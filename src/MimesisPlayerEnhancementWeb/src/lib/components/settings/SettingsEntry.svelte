<script lang="ts">
  import type { ConfigEntryDto, ConfigSectionDto } from '$lib/types';
  import Toggle from '$lib/components/Toggle.svelte';
  import { t } from '$lib/i18n';
  import { formatDefaultHint, formatGlobalHint, settingDiffersFromDefault, settingDiffersFromGlobal } from '$lib/settings';

  let {
    entry,
    section,
    scope,
    editable,
    onsave,
  }: {
    entry: ConfigEntryDto;
    section: ConfigSectionDto;
    scope: 'global' | 'save';
    editable: boolean;
    onsave: (value: string) => void;
  } = $props();

  function onChange(ev: Event) {
    const el = ev.currentTarget as HTMLInputElement | HTMLSelectElement;
    let value = el.value;
    if (entry.type === 'Boolean' && el instanceof HTMLInputElement && el.type === 'checkbox') {
      value = el.checked ? 'true' : 'false';
    }
    onsave(value);
  }

  const hint = $derived(
    scope === 'save'
      ? settingDiffersFromGlobal(entry)
        ? formatGlobalHint(entry)
        : ''
      : settingDiffersFromDefault(entry)
        ? formatDefaultHint(entry)
        : '',
  );

  const boolChecked = $derived(entry.value === 'true' || entry.value === 'True');
</script>

<div class="settings-entry {editable ? '' : 'settings-entry-readonly'}">
  <div class="settings-entry-header">
    <label class="settings-entry-title" for="{section.id}-{entry.key}">{entry.title}</label>
    {#if entry.hasLocalEffect}
      <span class="badge badge-local" title={t('dashboard.settings_local_hint')}>{t('dashboard.settings_local_badge')}</span>
    {/if}
    {#if scope === 'save' && entry.isOverridden}
      <span class="badge bg-violet-50 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300">{t('dashboard.settings_overridden')}</span>
    {/if}
    {#if !editable}
      <span class="badge bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400" title={t('dashboard.settings_host_only_hint')}>
        {t('dashboard.settings_host_only')}
      </span>
    {/if}
  </div>
  {#if entry.description}
    <p class="settings-entry-desc">{entry.description}</p>
  {/if}

  <div class="settings-entry-control">
    {#if entry.type === 'Boolean'}
      <Toggle
        checked={boolChecked}
        disabled={!editable}
        label={entry.title}
        onchange={(checked) => onsave(checked ? 'true' : 'false')}
      />
    {:else if entry.inputKind === 'Select'}
      <select id="{section.id}-{entry.key}" class="input max-w-md" value={entry.value} disabled={!editable} onchange={onChange}>
        {#each entry.selectOptions as opt}
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
        disabled={!editable}
        onchange={onChange}
      />
    {/if}
  </div>

  {#if hint}
    <p class="settings-entry-hint">{hint}</p>
  {/if}
</div>
