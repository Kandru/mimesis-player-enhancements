<script lang="ts">
  import type { Snippet } from 'svelte';
  import { t } from '$lib/i18n';
  import { isValidSteamId, navigate, playerDisplayLabel, steamProfileUrl } from '$lib/utils';

  let {
    steamId,
    displayName,
    badges,
  }: {
    steamId: string | number;
    displayName?: string;
    badges?: Snippet;
  } = $props();

  const label = $derived(playerDisplayLabel(displayName, steamId));
  const canNavigate = $derived(isValidSteamId(steamId));
</script>

<div class="data-table-player-name">
  {#if canNavigate}
    <button
      type="button"
      class="text-[var(--brand)] hover:underline"
      onclick={() => navigate(`player/${steamId}`)}
    >
      {label}
    </button>
    <a
      class="badge badge-steam hover:opacity-80"
      href={steamProfileUrl(steamId)}
      target="_blank"
      rel="noopener noreferrer"
    >
      {t('dashboard.badge_steam')}
    </a>
  {:else}
    <span>{label}</span>
  {/if}
  {@render badges?.()}
</div>
