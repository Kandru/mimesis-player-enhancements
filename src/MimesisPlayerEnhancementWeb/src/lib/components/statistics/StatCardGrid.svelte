<script lang="ts">
  import type { StatCountersDto } from '$lib/types';
  import { buildStatCards } from '$lib/statisticsHelpers';

  let { counters, extras = undefined }: {
    counters: StatCountersDto | undefined;
    extras?: Record<string, string | number>;
  } = $props();

  const cards = $derived(buildStatCards(counters, extras));
</script>

<div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
  {#each cards as [label, value] (label)}
    <div class="rounded-lg border border-gray-200 p-3 dark:border-gray-700">
      <div class="text-xs text-gray-500">{label}</div>
      <div class="text-lg font-semibold">{value}</div>
    </div>
  {/each}
</div>
