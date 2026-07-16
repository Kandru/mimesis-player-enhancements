<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { wikiArticles, wikiById, wikiOverview } from '$lib/generated/wiki';
  import type { WikiArticle } from '$lib/generated/wiki';
  import { navigate } from '$lib/utils';
  import ScopeBadges from '$lib/components/ScopeBadges.svelte';
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
          <ScopeBadges scopes={article.scopes} size="sm" />
        </button>
      {/each}
    </nav>

    <div class="wiki-content settings-content">
      <WikiArticleView article={activeArticle} />
    </div>
  </div>
</div>
