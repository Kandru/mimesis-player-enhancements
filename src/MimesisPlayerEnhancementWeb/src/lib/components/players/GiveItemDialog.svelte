<script lang="ts">
  import { onMount } from 'svelte';
  import Api from '$lib/api';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { PlayerDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import {
    defaultItemSelectionKey,
    getItemCatalogGroups,
    parseItemSelection,
  } from '$lib/itemCatalogHelpers';

  let {
    open = $bindable(false),
    eligiblePlayers,
    initialRecipients,
  }: {
    open?: boolean;
    eligiblePlayers: PlayerDto[];
    initialRecipients: string[];
  } = $props();

  let selectionKey = $state(defaultItemSelectionKey(dashboard.itemCatalog));
  let selectedSteamIds = $state<string[]>([]);
  let submitting = $state(false);

  onMount(() => {
    selectedSteamIds = [...initialRecipients];
  });

  const itemCatalogGroups = $derived(getItemCatalogGroups(dashboard.itemCatalog, t));
  const canSubmit = $derived(
    !submitting && selectedSteamIds.length > 0 && !!selectionKey && dashboard.itemCatalog.length > 0,
  );

  function toggleRecipient(steamId: string) {
    const key = String(steamId);
    if (selectedSteamIds.includes(key)) {
      selectedSteamIds = selectedSteamIds.filter((id) => id !== key);
    } else {
      selectedSteamIds = [...selectedSteamIds, key];
    }
  }

  function close() {
    if (submitting) return;
    open = false;
  }

  function onKeydown(e: KeyboardEvent) {
    if (!open || submitting) return;
    if (e.key === 'Escape') {
      e.preventDefault();
      close();
    }
  }

  async function submit(closeAfter = false) {
    if (!canSubmit) return;
    const { itemId, percent } = parseItemSelection(selectionKey);
    if (!itemId) return;

    submitting = true;
    try {
      const results = await Promise.allSettled(
        selectedSteamIds.map((steamId) => Api.spawnItem(steamId, itemId, percent)),
      );
      const failures = results.filter((r) => r.status === 'rejected') as PromiseRejectedResult[];
      const successes = results.filter((r) => r.status === 'fulfilled').length;

      if (failures.length === 0) {
        dashboard.showToast(t('dashboard.give_item_result', { count: successes }));
        if (closeAfter) open = false;
        return;
      }

      const firstError = failures[0].reason;
      dashboard.showToast(
        firstError instanceof Error ? firstError.message : String(firstError),
      );
    } finally {
      submitting = false;
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
    <div class="card dialog-panel dialog-panel-wide" role="dialog" aria-modal="true" aria-labelledby="give-item-dialog-title">
      <h3 id="give-item-dialog-title" class="dialog-title">{t('dashboard.give_item_title')}</h3>

      <div class="dialog-section">
        <span class="dialog-section-label">{t('dashboard.give_item_recipients')}</span>
        <div class="recipient-chip-list">
          {#each eligiblePlayers as player (player.steamId)}
            {@const steamKey = String(player.steamId)}
            {@const selected = selectedSteamIds.includes(steamKey)}
            <button
              type="button"
              class="recipient-chip {selected ? 'recipient-chip-selected' : ''}"
              aria-pressed={selected}
              onclick={() => toggleRecipient(steamKey)}
            >
              {#if selected}
                <span class="recipient-chip-check" aria-hidden="true">✓</span>
              {/if}
              {player.displayName}
            </button>
          {/each}
        </div>
        {#if selectedSteamIds.length === 0}
          <p class="dialog-hint">{t('dashboard.give_item_none_selected')}</p>
        {/if}
      </div>

      <div class="dialog-section">
        <label class="dialog-section-label" for="give-item-select">{t('dashboard.give_item_select')}</label>
        {#if dashboard.itemCatalog.length > 0}
          <select id="give-item-select" class="input" bind:value={selectionKey} disabled={submitting}>
            {#each itemCatalogGroups as group (group.id)}
              <optgroup label={group.label}>
                {#each group.entries as entry (entry.key)}
                  <option value={entry.key}>{entry.label}</option>
                {/each}
              </optgroup>
            {/each}
          </select>
        {:else}
          <p class="dialog-hint">{t('dashboard.loading')}</p>
        {/if}
      </div>

      <div class="dialog-actions">
        <button type="button" class="btn btn-danger" disabled={submitting} onclick={close}>
          {t('dashboard.dialog_cancel')}
        </button>
        <button type="button" class="btn btn-success" disabled={!canSubmit} onclick={() => submit()}>
          {t('dashboard.give_item_submit')}
        </button>
        <button type="button" class="btn btn-success" disabled={!canSubmit} onclick={() => submit(true)}>
          {t('dashboard.give_item_submit_and_close')}
        </button>
      </div>
    </div>
  </div>
{/if}
