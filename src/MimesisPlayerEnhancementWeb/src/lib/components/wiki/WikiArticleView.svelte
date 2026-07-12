<script lang="ts">
  import type { WikiArticle } from '$lib/generated/wiki';

  let { article }: { article: WikiArticle } = $props();
</script>

<article class="wiki-article-card card">
  <header class="wiki-article-header">
    <h1 class="wiki-article-title">{article.title}</h1>
    {#if article.scope}
      <span class="wiki-scope-badge wiki-scope-{article.scope}">
        {article.scope === 'local'
          ? 'Local'
          : article.scope === 'host-process'
            ? 'Host process'
            : 'Host only'}
      </span>
    {/if}
  </header>
  <div class="wiki-prose">
    {@html article.html}
  </div>
</article>
