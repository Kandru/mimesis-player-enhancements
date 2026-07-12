<script lang="ts">
  import type { Snippet } from 'svelte';
  import { t } from '$lib/i18n';
  import { isValidSteamId, navigate, steamProfileUrl } from '$lib/utils';

  let {
    steamId,
    displayName,
    badges,
  }: {
    steamId: string | number;
    displayName: string;
    badges?: Snippet;
  } = $props();

  const canNavigate = $derived(isValidSteamId(steamId));
</script>

<div class="player-identity">
  <div class="player-identity-primary">
    {#if canNavigate}
      <button
        type="button"
        class="player-identity-name"
        onclick={() => navigate(`player/${steamId}`)}
      >
        {displayName}
      </button>
      <a
        class="badge badge-steam badge-compact hover:opacity-80"
        href={steamProfileUrl(steamId)}
        target="_blank"
        rel="noopener noreferrer"
      >
        {t('dashboard.badge_steam')}
      </a>
    {:else}
      <span class="player-identity-name">{displayName}</span>
    {/if}
  </div>
  {#if badges}
    <div class="player-identity-badges">
      {@render badges()}
    </div>
  {/if}
</div>
