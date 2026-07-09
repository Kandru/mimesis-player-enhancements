<script lang="ts">
  import type { ConfigEntryDto, ConfigSectionDto } from '$lib/types';
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
</script>

<div class="rounded-lg border border-gray-100 p-3 dark:border-gray-700 {editable ? '' : 'settings-entry-host-only'}">
  <div class="mb-1 flex flex-wrap items-center gap-2">
    <label class="font-medium" for="{section.id}-{entry.key}">{entry.title}</label>
    {#if entry.hasLocalEffect}
      <span class="badge badge-local" title={t('dashboard.settings_local_hint')}>{t('dashboard.settings_local_badge')}</span>
    {/if}
    {#if scope === 'save' && entry.isOverridden}
      <span class="badge bg-violet-100 text-violet-800">{t('dashboard.settings_overridden')}</span>
    {/if}
    {#if !editable}
      <span class="text-xs text-gray-500" title={t('dashboard.settings_host_only_hint')}>{t('dashboard.settings_host_only')}</span>
    {/if}
  </div>
  {#if entry.description}
    <p class="mb-2 text-xs text-gray-500">{entry.description}</p>
  {/if}

  {#if entry.type === 'Boolean'}
    <input
      id="{section.id}-{entry.key}"
      type="checkbox"
      checked={entry.value === 'true' || entry.value === 'True'}
      disabled={!editable}
      onchange={onChange}
    />
  {:else if entry.inputKind === 'Select'}
    <select id="{section.id}-{entry.key}" class="input" value={entry.value} disabled={!editable} onchange={onChange}>
      {#each entry.selectOptions as opt}
        <option value={opt.value}>{opt.label}</option>
      {/each}
    </select>
  {:else}
    <input
      id="{section.id}-{entry.key}"
      class="input"
      type={entry.type === 'Int32' || entry.type === 'Single' || entry.type === 'Double' ? 'number' : 'text'}
      value={entry.value}
      min={entry.minValue}
      max={entry.maxValue}
      disabled={!editable}
      onchange={onChange}
    />
  {/if}

  {#if hint}
    <p class="mt-1 text-xs text-gray-500">{hint}</p>
  {/if}
</div>
