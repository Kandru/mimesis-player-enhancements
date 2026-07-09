<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
</script>

{#if dashboard.toastVisible}
  <div
    class="fixed bottom-6 right-6 z-50 max-w-sm rounded-xl border border-gray-200 bg-white px-4 py-3 text-sm shadow-lg dark:border-gray-700 dark:bg-gray-800"
    role="status"
  >
    {dashboard.toastMessage}
  </div>
{/if}
