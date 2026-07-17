<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
</script>

{#if dashboard.toastVisible}
  <div
    class="fixed top-[calc(var(--header-height)+1.5rem)] right-6 z-50 max-w-sm rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm shadow-lg dark:border-gray-700 dark:bg-gray-700"
    role="status"
  >
    {dashboard.toastMessage}
  </div>
{/if}
