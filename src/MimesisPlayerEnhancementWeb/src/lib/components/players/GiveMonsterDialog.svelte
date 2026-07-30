<script lang="ts">
  import { onMount } from 'svelte';
  import Api from '$lib/api';
  import SearchablePicker from '$lib/components/settings/SearchablePicker.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import type { PlayerDto } from '$lib/types';
  import { t } from '$lib/i18n';
  import { buildGiveMonsterPickerOptions } from '$lib/pickerOptions';

  let {
    open = $bindable(false),
    eligiblePlayers,
    initialRecipients,
  }: {
    open?: boolean;
    eligiblePlayers: PlayerDto[];
    initialRecipients: string[];
  } = $props();

  let selectionKey = $state('');
  let selectedSteamIds = $state<string[]>([]);
  let submitting = $state(false);

  onMount(() => {
    selectedSteamIds = [...initialRecipients];
  });

  const giveMonsterOptions = $derived(buildGiveMonsterPickerOptions(dashboard.monsterCatalog, t));
  const canSubmit = $derived(
    !submitting && selectedSteamIds.length > 0 && !!selectionKey && dashboard.monsterCatalog.length > 0,
  );

  $effect(() => {
    if (giveMonsterOptions.length === 0) {
      selectionKey = '';
      return;
    }
    if (!giveMonsterOptions.some((opt) => opt.value === selectionKey)) {
      selectionKey = giveMonsterOptions[0].value;
    }
  });

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
    if (!canSubmit || !selectionKey) return;

    submitting = true;
    try {
      const results = await Promise.allSettled(
        selectedSteamIds.map((steamId) => Api.spawnMonster(steamId, selectionKey)),
      );
      const failures = results.filter((r) => r.status === 'rejected') as PromiseRejectedResult[];
      const successes = results.filter((r) => r.status === 'fulfilled').length;

      if (failures.length === 0) {
        dashboard.showToast(t('dashboard.spawn_monster_result', { count: successes }));
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
    <div class="card dialog-panel dialog-panel-wide" role="dialog" aria-modal="true" aria-labelledby="spawn-monster-dialog-title">
      <h3 id="spawn-monster-dialog-title" class="dialog-title">{t('dashboard.spawn_monster_title')}</h3>

      <div class="dialog-section">
        <span class="dialog-section-label">{t('dashboard.spawn_monster_recipients')}</span>
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
          <p class="dialog-hint">{t('dashboard.spawn_monster_none_selected')}</p>
        {/if}
      </div>

      <div class="dialog-section">
        <label class="dialog-section-label" for="spawn-monster-select">{t('dashboard.spawn_monster_select')}</label>
        {#if dashboard.monsterCatalog.length > 0}
          <SearchablePicker
            id="spawn-monster-select"
            options={giveMonsterOptions}
            value={selectionKey}
            disabled={submitting}
            onsave={(value) => {
              selectionKey = value;
            }}
          />
        {:else}
          <p class="dialog-hint">{t('dashboard.loading')}</p>
        {/if}
      </div>

      <div class="dialog-actions">
        <button type="button" class="btn btn-danger" disabled={submitting} onclick={close}>
          {t('dashboard.dialog_cancel')}
        </button>
        <button type="button" class="btn btn-success" disabled={!canSubmit} onclick={() => submit()}>
          {t('dashboard.spawn_monster_submit')}
        </button>
        <button type="button" class="btn btn-success" disabled={!canSubmit} onclick={() => submit(true)}>
          {t('dashboard.spawn_monster_submit_and_close')}
        </button>
      </div>
    </div>
  </div>
{/if}
