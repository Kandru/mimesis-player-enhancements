<script lang="ts">
  import { t } from '$lib/i18n';

  let {
    open = $bindable(false),
    title = '',
    message = '',
    confirmLabel = '',
    cancelLabel = '',
    loading = false,
    onConfirm,
    onCancel,
  }: {
    open?: boolean;
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    loading?: boolean;
    onConfirm?: () => void | Promise<void>;
    onCancel?: () => void;
  } = $props();

  const resolvedConfirmLabel = $derived(confirmLabel || t('dashboard.dialog_confirm'));
  const resolvedCancelLabel = $derived(cancelLabel || t('dashboard.dialog_cancel'));

  function close() {
    if (loading) return;
    open = false;
    onCancel?.();
  }

  async function confirm() {
    if (loading) return;
    await onConfirm?.();
  }

  function onKeydown(e: KeyboardEvent) {
    if (!open || loading) return;
    if (e.key === 'Escape') {
      e.preventDefault();
      close();
    }
  }
</script>

<svelte:window onkeydown={onKeydown} />

{#if open}
  <div
    class="dialog-overlay"
    role="presentation"
    onclick={(e) => {
      if (e.target === e.currentTarget) close();
    }}
  >
    <div class="card dialog-panel" role="dialog" aria-modal="true" aria-labelledby="confirm-dialog-title">
      <h3 id="confirm-dialog-title" class="dialog-title">{title}</h3>
      <p class="dialog-message">{message}</p>
      <div class="dialog-actions">
        <button type="button" class="btn btn-danger" disabled={loading} onclick={close}>
          {resolvedCancelLabel}
        </button>
        <button type="button" class="btn btn-success" disabled={loading} onclick={confirm}>
          {resolvedConfirmLabel}
        </button>
      </div>
    </div>
  </div>
{/if}
