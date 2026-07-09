<script lang="ts">
  import type { Snippet } from 'svelte';
  import Sidebar from './Sidebar.svelte';
  import Header from './Header.svelte';

  let { children }: { children: Snippet } = $props();
</script>

<div class="flex min-h-full">
  <Sidebar />
  <div class="flex min-h-full flex-1 flex-col lg:pl-[var(--sidebar-width)]">
    <Header />
    <main class="flex-1 p-4 md:p-6">
      {@render children()}
    </main>
  </div>
</div>
