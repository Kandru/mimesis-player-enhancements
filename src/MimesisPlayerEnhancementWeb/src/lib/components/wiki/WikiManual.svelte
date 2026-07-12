<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { wikiArticles, wikiById, wikiOverview } from '$lib/generated/wiki';
  import type { WikiArticle } from '$lib/generated/wiki';
  import { navigate } from '$lib/utils';
  import WikiArticleView from './WikiArticleView.svelte';

  const activeArticle = $derived.by((): WikiArticle => {
    const id = dashboard.homeSubRoute || 'overview';
    return wikiById[id] ?? wikiOverview;
  });

  function selectArticle(id: string) {
    if (id === 'overview') {
      navigate('home');
      return;
    }
    navigate(`home/${id}`);
  }

  function scopeLabel(scope: WikiArticle['scope']): string | null {
    if (!scope) return null;
    if (scope === 'local') return 'Local';
    if (scope === 'host-process') return 'Host';
    return 'Host';
  }
</script>

<div class="wiki-manual">
  <div class="wiki-layout">
    <nav class="wiki-nav settings-nav" aria-label="Manual sections">
      <button
        type="button"
        class="wiki-nav-item settings-nav-item {activeArticle.id === 'overview' ? 'settings-nav-item-active' : ''}"
        onclick={() => selectArticle('overview')}
      >
        <span class="settings-nav-label">Overview</span>
      </button>

      {#each wikiArticles as article (article.id)}
        <button
          type="button"
          class="wiki-nav-item settings-nav-item {activeArticle.id === article.id ? 'settings-nav-item-active' : ''}"
          onclick={() => selectArticle(article.id)}
        >
          <span class="settings-nav-label">{article.title}</span>
          {#if scopeLabel(article.scope)}
            <span class="wiki-nav-scope">{scopeLabel(article.scope)}</span>
          {/if}
        </button>
      {/each}
    </nav>

    <div class="wiki-content settings-content">
      <WikiArticleView article={activeArticle} />
    </div>
  </div>
</div>
