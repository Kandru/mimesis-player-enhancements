<script lang="ts">
  import type { Snippet } from 'svelte';
  import Sidebar from './Sidebar.svelte';
  import Header from './Header.svelte';

  let { children }: { children: Snippet } = $props();
</script>

<div class="app-shell min-h-full bg-gray-50 dark:bg-gray-950">
  <Sidebar />
  <div class="app-main">
    <Header />
    <main class="page-content">
      {@render children()}
    </main>
  </div>
</div>
