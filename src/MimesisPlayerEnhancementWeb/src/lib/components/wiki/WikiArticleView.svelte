<script lang="ts">
  import type { WikiArticle } from '$lib/generated/wiki';
  import ScopeBadges from '$lib/components/ScopeBadges.svelte';

  let { article }: { article: WikiArticle } = $props();
</script>

<article class="wiki-article-card card">
  <header class="wiki-article-header">
    <h1 class="wiki-article-title">{article.title}</h1>
    <ScopeBadges scopes={article.scopes} />
  </header>
  <div class="wiki-prose">
    {@html article.html}
  </div>
</article>
