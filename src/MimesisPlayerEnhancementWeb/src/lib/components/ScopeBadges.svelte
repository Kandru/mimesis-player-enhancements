<script lang="ts">
  import { t } from '$lib/i18n';

  export type ScopeBadge = 'host' | 'local';

  let {
    scopes = [],
    size = 'md',
  }: {
    scopes: ScopeBadge[];
    size?: 'sm' | 'md';
  } = $props();

  function label(scope: ScopeBadge): string {
    if (scope === 'local') return t('dashboard.settings_local_badge');
    return t('dashboard.settings_host_badge');
  }

  function hint(scope: ScopeBadge): string {
    if (scope === 'local') return t('dashboard.settings_local_hint');
    return t('dashboard.settings_host_badge_hint');
  }

  function badgeClass(scope: ScopeBadge): string {
    return scope === 'local' ? 'badge-local' : 'badge-host';
  }
</script>

{#if scopes.length > 0}
  <span class="scope-badges scope-badges-{size}">
    {#each scopes as scope (scope)}
      <span class="badge {badgeClass(scope)}" title={hint(scope)}>{label(scope)}</span>
    {/each}
  </span>
{/if}
