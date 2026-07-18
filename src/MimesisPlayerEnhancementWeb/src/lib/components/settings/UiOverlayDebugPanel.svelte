<script lang="ts">
  import { onMount } from 'svelte';
  import Api from '$lib/api';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import type { UiDebugStatusDto } from '$lib/types';

  let status = $state<UiDebugStatusDto | null>(null);
  let pending = $state<string | null>(null);
  let error = $state('');

  const overlays = [
    { id: 'spectator', labelKey: 'dashboard.debug_overlay_spectator' },
    { id: 'loadingWait', labelKey: 'dashboard.debug_overlay_loading_wait' },
    { id: 'escMenu', labelKey: 'dashboard.debug_overlay_esc_menu' },
    { id: 'survivalResult', labelKey: 'dashboard.debug_overlay_survival_result' },
  ] as const;

  const ingame = $derived(status?.ingame ?? !!dashboard.status.sessionScene);
  const alive = $derived(status?.alive ?? dashboard.getLocalPlayer()?.isAlive ?? false);
  const available = $derived(ingame && alive);

  async function refresh() {
    try {
      status = await Api.getUiDebugOverlays();
      error = '';
    } catch (err) {
      error = err instanceof Error ? err.message : String(err);
    }
  }

  async function toggle(id: string) {
    if (!available || pending) return;
    pending = id;
    error = '';
    try {
      const result = await Api.toggleUiDebugOverlay(id);
      if (!result.success && result.message) {
        error = result.message;
        dashboard.showToast(result.message);
      }
      await refresh();
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      error = message;
      dashboard.showToast(message);
    } finally {
      pending = null;
    }
  }

  function isActive(id: (typeof overlays)[number]['id']): boolean {
    if (!status) return false;
    switch (id) {
      case 'spectator':
        return status.spectator;
      case 'loadingWait':
        return status.loadingWait;
      case 'escMenu':
        return status.escMenu;
      case 'survivalResult':
        return status.survivalResult;
    }
  }

  onMount(() => {
    void refresh();
  });

  $effect(() => {
    dashboard.status.sessionScene;
    dashboard.players;
    void refresh();
  });
</script>

<div class="settings-debug-overlays" aria-label={t('dashboard.debug_overlays_heading')}>
  <p class="settings-debug-overlays-lead">{t('dashboard.debug_overlays_lead')}</p>
  {#if status}
    <p class="settings-debug-overlays-hint">
      {t('dashboard.debug_max_players_hint', { count: String(status.maxPlayers) })}
    </p>
  {/if}
  {#if !ingame}
    <p class="settings-debug-overlays-disabled">{t('dashboard.debug_not_ingame_hint')}</p>
  {:else if !alive}
    <p class="settings-debug-overlays-disabled">{t('dashboard.debug_not_alive_hint')}</p>
  {/if}
  {#if error}
    <p class="settings-debug-overlays-error">{error}</p>
  {/if}
  <div class="settings-debug-overlays-grid">
    {#each overlays as overlay (overlay.id)}
      <div class="settings-debug-overlay-row">
        <span class="settings-debug-overlay-label">{t(overlay.labelKey)}</span>
        <button
          type="button"
          class="btn btn-secondary btn-xs {isActive(overlay.id) ? 'btn-active' : ''}"
          disabled={!available || pending === overlay.id}
          onclick={() => toggle(overlay.id)}
        >
          {isActive(overlay.id) ? t('dashboard.debug_hide') : t('dashboard.debug_show')}
        </button>
      </div>
    {/each}
  </div>
</div>

<style>
  .settings-debug-overlays {
    margin-top: 0.75rem;
    padding-top: 0.75rem;
    border-top: 1px solid color-mix(in srgb, currentColor 12%, transparent);
  }

  .settings-debug-overlays-lead,
  .settings-debug-overlays-hint,
  .settings-debug-overlays-disabled,
  .settings-debug-overlays-error {
    margin: 0 0 0.5rem;
    font-size: 0.8125rem;
    line-height: 1.45;
  }

  .settings-debug-overlays-lead,
  .settings-debug-overlays-hint {
    color: var(--text-muted, #6b7280);
  }

  .settings-debug-overlays-disabled {
    color: #b45309;
  }

  .settings-debug-overlays-error {
    color: #dc2626;
  }

  .settings-debug-overlays-grid {
    display: grid;
    gap: 0.5rem;
  }

  .settings-debug-overlay-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
  }

  .settings-debug-overlay-label {
    font-size: 0.875rem;
  }
</style>
