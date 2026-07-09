<script lang="ts">
  import Api from '$lib/api';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { navigate } from '$lib/utils';

  const profile = $derived(dashboard.saveProfile?.profile);
  const builtins = $derived(dashboard.quickPresets.filter((p) => p.isBuiltin));
  const userPresets = $derived(dashboard.quickPresets.filter((p) => !p.isBuiltin));

  async function applyMode(mode: string, presetId = '') {
    dashboard.applyingProfile = true;
    try {
      const result = await Api.updateSaveProfile({ mode, presetId });
      dashboard.saveProfile = result as typeof dashboard.saveProfile;
      dashboard.showToast((result as { message?: string }).message || t('dashboard.quick_profile_applied'));
      if (mode === 'custom') navigate('settings/customize');
    } catch (e) {
      dashboard.showToast(e instanceof Error ? e.message : String(e));
    } finally {
      dashboard.applyingProfile = false;
    }
  }
</script>

<div class="space-y-4">
  <div>
    <h2 class="text-lg font-semibold">{t('dashboard.settings_heading')}</h2>
    <p class="text-sm text-gray-600 dark:text-gray-400">{t('dashboard.settings_intro_profile')}</p>
    {#if profile}
      <p class="mt-2 text-sm">{t('dashboard.quick_active_profile', { label: profile.label || profile.mode })}</p>
    {/if}
  </div>

  {#if dashboard.loadingSaveProfile}
    <p class="text-sm text-gray-500">{t('dashboard.loading')}</p>
  {:else}
    <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      <button type="button" class="card p-3 text-left hover:ring-2 hover:ring-[var(--brand)]" onclick={() => applyMode('global')}>
        <div class="font-semibold">{t('quicksettings.profile.global')}</div>
      </button>
      {#each builtins as preset}
        <button type="button" class="card p-3 text-left hover:ring-2 hover:ring-[var(--brand)]" onclick={() => applyMode('quick', preset.id)}>
          <div class="font-semibold">{preset.name}</div>
        </button>
      {/each}
      {#each userPresets as preset}
        <button type="button" class="card p-3 text-left hover:ring-2 hover:ring-[var(--brand)]" onclick={() => applyMode('quick', preset.id)}>
          <div class="font-semibold">{preset.name}</div>
          <div class="text-xs text-gray-500">{t('dashboard.quick_preset_user')}</div>
        </button>
      {/each}
      <button type="button" class="card p-3 text-left hover:ring-2 hover:ring-[var(--brand)]" onclick={() => applyMode('custom')}>
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
      <button
        type="button"
        class="btn btn-secondary"
        onclick={() => { dashboard.shareModalMode = 'import'; dashboard.shareModalOpen = true; }}
      >{t('dashboard.quick_import')}</button>
    </div>
  {/if}
</div>

{#if dashboard.shareModalOpen}
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
    <div class="card max-w-lg w-full p-4">
      <h3 class="mb-3 font-semibold">{dashboard.shareModalMode === 'export' ? t('dashboard.quick_export') : t('dashboard.quick_import')}</h3>
      <textarea class="input min-h-32 font-mono text-xs" bind:value={dashboard.shareModalText}></textarea>
      <div class="mt-3 flex justify-end gap-2">
        <button type="button" class="btn btn-secondary" onclick={() => { dashboard.shareModalOpen = false; }}>Close</button>
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
          >Import</button>
        {/if}
      </div>
    </div>
  </div>
{/if}
