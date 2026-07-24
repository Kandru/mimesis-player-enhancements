<script lang="ts">
  import Api from '$lib/api';
  import SettingsPanel from '$lib/components/settings/SettingsPanel.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';
  import { canEditSaveSettings } from '$lib/utils';
  import type { QuickPresetDto } from '$lib/types';

  const saveEditable = $derived(canEditSaveSettings(dashboard.status));
  const showReadonlySavePanel = $derived(!saveEditable && dashboard.settingsSave != null);

  const profile = $derived(dashboard.saveProfile?.profile);
  const builtins = $derived(dashboard.quickPresets.filter((p) => p.isBuiltin));
  const userPresets = $derived(dashboard.quickPresets.filter((p) => !p.isBuiltin));
  const showCustomizePanel = $derived(
    profile?.mode === 'custom' || dashboard.settingsSubRoute === 'customize',
  );
  const saveIntro = $derived(
    t('dashboard.settings_intro_save', {
      slot: Math.max(0, dashboard.status.saveSlotId),
    }),
  );

  let presetSaveModalOpen = $state(false);
  let presetSaveMode = $state<'new' | 'overwrite'>('new');
  let presetSaveName = $state('');
  let overwritePresetId = $state('');
  let savingPreset = $state(false);

  $effect(() => {
    if (userPresets.length > 0 && !overwritePresetId) {
      overwritePresetId = userPresets[0]?.id ?? '';
    }
  });

  $effect(() => {
    if (
      dashboard.settingsSubRoute === 'customize'
      && profile
      && profile.mode !== 'custom'
      && !dashboard.applyingProfile
    ) {
      void applyMode('custom');
    }
    if (dashboard.settingsSubRoute === 'customize' && profile?.mode === 'custom') {
      navigate('settings');
    }
  });

  function isProfileActive(mode: string, presetId = ''): boolean {
    if (!profile) return false;
    if (mode === 'global') return profile.mode === 'global';
    if (mode === 'custom') return profile.mode === 'custom';
    if (mode === 'quick') return profile.mode === 'quick' && profile.presetId === presetId;
    return false;
  }

  function profileCardClass(active: boolean): string {
    const base = 'card p-3 text-left transition-shadow hover:ring-2 hover:ring-[var(--brand)]';
    return active ? `${base} ring-2 ring-green-500 border-green-500` : base;
  }

  function openImportModal() {
    dashboard.shareModalMode = 'import';
    dashboard.shareModalText = '';
    dashboard.shareModalName = '';
    dashboard.shareModalOpen = true;
  }

  function openPresetSaveModal(mode: 'new' | 'overwrite') {
    presetSaveMode = mode;
    if (mode === 'overwrite') {
      const preset = userPresets.find((p) => p.id === overwritePresetId) ?? userPresets[0];
      overwritePresetId = preset?.id ?? '';
      presetSaveName = preset?.name ?? '';
    } else {
      presetSaveName = '';
    }
    presetSaveModalOpen = true;
  }

  async function applyMode(mode: string, presetId = '') {
    dashboard.applyingProfile = true;
    try {
      const result = await Api.updateSaveProfile({ mode, presetId });
      dashboard.saveProfile = result as typeof dashboard.saveProfile;
      dashboard.showToast((result as { message?: string }).message || t('dashboard.quick_profile_applied'));
      if (mode === 'custom') {
        await dashboard.loadSaveSettings(false, true);
      }
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      dashboard.applyingProfile = false;
    }
  }

  async function deletePreset(preset: QuickPresetDto, event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    if (preset.isBuiltin) return;
    if (!confirm(t('dashboard.quick_preset_delete_confirm', { name: preset.name }))) return;
    try {
      await Api.deleteQuickPreset(preset.id);
      if (overwritePresetId === preset.id) overwritePresetId = '';
      await dashboard.loadSaveProfileData(true);
      dashboard.showToast(t('dashboard.quick_preset_deleted'));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    }
  }

  async function submitPresetSave() {
    const name = presetSaveName.trim();
    if (!name) {
      dashboard.showToast(t('dashboard.quick_preset_name_prompt'));
      return;
    }
    savingPreset = true;
    try {
      const body =
        presetSaveMode === 'overwrite'
          ? {
              presetId: overwritePresetId,
              name,
              overwriteExisting: true,
              fromCurrentSave: true,
            }
          : {
              name,
              fromCurrentSave: true,
            };
      await Api.saveQuickPreset(body);
      presetSaveModalOpen = false;
      await dashboard.loadSaveProfileData(true);
      dashboard.showToast(t('dashboard.quick_preset_saved', { name }));
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      savingPreset = false;
    }
  }
</script>

<div class="space-y-6">
  <p class="text-sm text-gray-600 dark:text-gray-300">{t('dashboard.settings_intro_profile')}</p>

  {#if profile?.label}
    <p class="text-sm font-medium text-gray-700 dark:text-gray-300">
      {t('dashboard.quick_active_profile', { label: profile.label })}
    </p>
  {/if}

  {#if dashboard.loadingSaveProfile}
    <p class="text-sm text-gray-500 dark:text-gray-300">{t('dashboard.loading')}</p>
  {:else if saveEditable}
    <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      <button
        type="button"
        class={profileCardClass(isProfileActive('global'))}
        onclick={() => applyMode('global')}
      >
        <div class="font-semibold">{t('quicksettings.profile.global')}</div>
      </button>
      {#each builtins as preset (preset.id)}
        <button
          type="button"
          class={profileCardClass(isProfileActive('quick', preset.id))}
          onclick={() => applyMode('quick', preset.id)}
        >
          <div class="font-semibold">{preset.name}</div>
          {#if preset.description}
            <div class="mt-1 text-xs text-gray-500 dark:text-gray-300">{preset.description}</div>
          {/if}
        </button>
      {/each}
      {#each userPresets as preset (preset.id)}
        <div
          class="{profileCardClass(isProfileActive('quick', preset.id))} flex items-stretch gap-0 p-0 overflow-hidden"
        >
          <button
            type="button"
            class="min-w-0 flex-1 p-3 text-left"
            onclick={() => applyMode('quick', preset.id)}
          >
            <div class="font-semibold">{preset.name}</div>
            <div class="text-xs text-gray-500 dark:text-gray-300">{t('dashboard.quick_preset_user')}</div>
          </button>
          <button
            type="button"
            class="flex shrink-0 items-center px-3 text-red-600 hover:bg-red-50 dark:hover:bg-red-950/40"
            aria-label={t('dashboard.quick_preset_delete')}
            title={t('dashboard.quick_preset_delete')}
            onclick={(event) => deletePreset(preset, event)}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" class="h-4 w-4" aria-hidden="true">
              <path d="M3 6h18" /><path d="M8 6V4h8v2" /><path d="M19 6l-1 14H6L5 6" /><path d="M10 11v6" /><path d="M14 11v6" />
            </svg>
          </button>
        </div>
      {/each}
      <button
        type="button"
        class={profileCardClass(isProfileActive('custom'))}
        onclick={() => applyMode('custom')}
      >
        <div class="font-semibold">{t('quicksettings.profile.custom')}</div>
      </button>
    </div>

    <div class="flex flex-wrap gap-2">
      <button
        type="button"
        class="btn btn-secondary"
        onclick={async () => {
          try {
            const data = await Api.exportQuickPreset('current');
            dashboard.shareModalMode = 'export';
            dashboard.shareModalText = data.shareString || '';
            dashboard.shareModalName = data.name || '';
            dashboard.shareModalOpen = true;
          } catch (e) {
            dashboard.showToast(e instanceof Error ? e.message : String(e));
          }
        }}
      >{t('dashboard.quick_export')}</button>
      <button type="button" class="btn btn-secondary" onclick={openImportModal}>{t('dashboard.quick_import')}</button>
    </div>
  {/if}

  {#if showReadonlySavePanel}
    <SettingsPanel settings={dashboard.settingsSave} scope="save" intro={saveIntro} />
  {/if}

  {#if showCustomizePanel && saveEditable}
    <div class="space-y-4 border-t border-gray-200 pt-6 dark:border-gray-700">
      {#if profile?.mode === 'quick'}
        <p class="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-900 dark:bg-amber-950/40 dark:text-amber-100">
          {t('dashboard.quick_customize_banner', { label: profile.label })}
        </p>
      {/if}

      <div class="flex flex-wrap items-end gap-2">
        <button type="button" class="btn btn-secondary" onclick={() => openPresetSaveModal('new')}>
          {t('dashboard.quick_preset_save_new')}
        </button>
        {#if userPresets.length > 0}
          <label class="flex flex-col gap-1 text-sm">
            <span class="text-gray-600 dark:text-gray-300">{t('dashboard.quick_preset_overwrite')}</span>
            <div class="flex flex-wrap gap-2">
              <select class="input min-w-40" bind:value={overwritePresetId}>
                {#each userPresets as preset (preset.id)}
                  <option value={preset.id}>{preset.name}</option>
                {/each}
              </select>
              <button
                type="button"
                class="btn btn-secondary"
                disabled={!overwritePresetId}
                onclick={() => openPresetSaveModal('overwrite')}
              >
                {t('dashboard.quick_preset_overwrite')}
              </button>
            </div>
          </label>
        {/if}
      </div>

      <SettingsPanel settings={dashboard.settingsSave} scope="save" intro={saveIntro} />
    </div>
  {/if}
</div>

{#if dashboard.shareModalOpen}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
    <div class="card max-w-lg w-full p-4">
      <h3 class="mb-2 font-semibold">
        {dashboard.shareModalMode === 'export' ? t('dashboard.quick_export') : t('dashboard.quick_import')}
      </h3>
      <p class="mb-3 text-sm text-gray-600 dark:text-gray-300">
        {dashboard.shareModalMode === 'export' ? t('dashboard.quick_export_help') : t('dashboard.quick_import_help')}
      </p>
      <textarea
        class="input min-h-32 font-mono text-xs"
        bind:value={dashboard.shareModalText}
        readonly={dashboard.shareModalMode === 'export'}
        placeholder={dashboard.shareModalMode === 'import' ? t('dashboard.quick_share_placeholder') : undefined}
      ></textarea>
      <div class="mt-3 flex justify-end gap-2">
        <button type="button" class="btn btn-secondary" onclick={() => { dashboard.shareModalOpen = false; }}>
          {t('dashboard.quick_share_close')}
        </button>
        {#if dashboard.shareModalMode === 'import'}
          <button
            type="button"
            class="btn btn-primary"
            onclick={async () => {
              try {
                const result = await Api.importQuickPreset({ shareString: dashboard.shareModalText.trim(), saveOnly: true });
                dashboard.shareModalOpen = false;
                await dashboard.loadSaveProfileData(true);
                dashboard.showToast((result as { message?: string }).message || t('dashboard.saved'));
              } catch (e) {
                dashboard.showToast(e instanceof Error ? e.message : String(e));
              }
            }}
          >{t('dashboard.quick_import')}</button>
        {/if}
      </div>
    </div>
  </div>
{/if}

{#if presetSaveModalOpen}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
    <div class="card max-w-md w-full p-4">
      <h3 class="mb-2 font-semibold">
        {presetSaveMode === 'new'
          ? t('dashboard.quick_preset_save_new')
          : t('dashboard.quick_preset_overwrite')}
      </h3>
      <p class="mb-3 text-sm text-gray-600 dark:text-gray-300">
        {presetSaveMode === 'new'
          ? t('dashboard.quick_preset_name_prompt')
          : t('dashboard.quick_preset_overwrite_prompt')}
      </p>
      {#if presetSaveMode === 'overwrite'}
        <select class="input mb-3 w-full" bind:value={overwritePresetId}>
          {#each userPresets as preset (preset.id)}
            <option value={preset.id}>{preset.name}</option>
          {/each}
        </select>
      {/if}
      <input
        class="input w-full"
        type="text"
        bind:value={presetSaveName}
        placeholder={t('dashboard.quick_preset_name_prompt')}
      />
      <div class="mt-3 flex justify-end gap-2">
        <button type="button" class="btn btn-secondary" onclick={() => { presetSaveModalOpen = false; }}>
          {t('dashboard.quick_share_close')}
        </button>
        <button type="button" class="btn btn-primary" disabled={savingPreset} onclick={submitPresetSave}>
          {presetSaveMode === 'new'
            ? t('dashboard.quick_preset_save_new')
            : t('dashboard.quick_preset_overwrite')}
        </button>
      </div>
    </div>
  </div>
{/if}
