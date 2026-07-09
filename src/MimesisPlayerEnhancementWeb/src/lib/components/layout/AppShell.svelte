<script lang="ts">
  import type { Snippet } from 'svelte';
  import Sidebar from './Sidebar.svelte';
  import Header from './Header.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';

  let { children }: { children: Snippet } = $props();
</script>

<div class="app-shell min-h-full bg-gray-50 dark:bg-gray-950">
  <Sidebar mobileOpen={dashboard.mobileSidebarOpen} onclose={() => (dashboard.mobileSidebarOpen = false)} />
  {#if dashboard.mobileSidebarOpen}
    <button
      type="button"
      class="sidebar-backdrop lg:hidden"
      aria-label="Close menu"
      onclick={() => (dashboard.mobileSidebarOpen = false)}
    ></button>
  {/if}
  <div class="app-main">
    <Header />
    <main class="page-content">
      {@render children()}
    </main>
  </div>
</div>
