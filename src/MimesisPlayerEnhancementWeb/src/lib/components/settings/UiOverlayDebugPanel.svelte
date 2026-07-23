<script lang="ts">
  import { onMount } from 'svelte';
  import Api from '$lib/api';
  import Toggle from '$lib/components/Toggle.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import type { UiDebugStatusDto } from '$lib/types';

  let status = $state<UiDebugStatusDto | null>(null);

  const overlays = [
    { id: 'spectator', labelKey: 'dashboard.debug_overlay_spectator', descKey: 'dashboard.debug_overlay_spectator_desc' },
    { id: 'loadingWait', labelKey: 'dashboard.debug_overlay_loading_wait', descKey: 'dashboard.debug_overlay_loading_wait_desc' },
    { id: 'escMenu', labelKey: 'dashboard.debug_overlay_esc_menu', descKey: 'dashboard.debug_overlay_esc_menu_desc' },
    { id: 'survivalResult', labelKey: 'dashboard.debug_overlay_survival_result', descKey: 'dashboard.debug_overlay_survival_result_desc' },
  ] as const;

  type OverlayId = (typeof overlays)[number]['id'];

  let pending = $state<OverlayId | null>(null);

  const ingame = $derived(status?.ingame ?? !!dashboard.status.sessionScene);
  const alive = $derived(status?.alive ?? dashboard.getLocalPlayer()?.isAlive ?? false);
  const available = $derived(ingame && alive);

  async function refresh() {
    try {
      status = await Api.getUiDebugOverlays();
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      dashboard.showToast(message);
    }
  }

  async function setActive(id: OverlayId, active: boolean) {
    if (!available || pending || isActive(id) === active) return;
    pending = id;
    try {
      const result = await Api.toggleUiDebugOverlay(id);
      if (!result.success && result.message) {
        dashboard.showToast(result.message);
      }
      await refresh();
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
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

<div aria-label={t('dashboard.debug_overlays_heading')}>
  {#each overlays as overlay (overlay.id)}
    <div class="settings-entry {!available ? 'settings-entry-disabled' : ''}">
      <div class="settings-entry-main">
        <div class="settings-entry-header">
          <span class="settings-entry-title" id="ui-debug-{overlay.id}">{t(overlay.labelKey)}</span>
        </div>
        <p class="settings-entry-desc">{t(overlay.descKey)}</p>
      </div>
      <div class="settings-entry-actions">
        <div class="settings-entry-control">
          <Toggle
            checked={isActive(overlay.id)}
            disabled={!available || pending === overlay.id}
            label={t(overlay.labelKey)}
            onchange={(checked) => setActive(overlay.id, checked)}
          />
        </div>
      </div>
    </div>
  {/each}
</div>
