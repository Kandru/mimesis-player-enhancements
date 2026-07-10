<script lang="ts">
  import type { Snippet } from 'svelte';
  import Sidebar from './Sidebar.svelte';
  import Header from './Header.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';

  let { children }: { children: Snippet } = $props();
</script>

<div class="app-shell min-h-full bg-gray-50 dark:bg-gray-950" class:sidebar-open={dashboard.sidebarOpen}>
  <button
    type="button"
    class="sidebar-backdrop"
    aria-label="Close menu"
    onclick={() => (dashboard.sidebarOpen = false)}
  ></button>
  <Sidebar />
  <div class="app-main">
    <Header />
    <main class="page-content">
      <div class="page-content-inner">
        {@render children()}
      </div>
    </main>
  </div>
</div>
