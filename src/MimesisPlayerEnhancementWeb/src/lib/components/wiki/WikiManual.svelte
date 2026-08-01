<script lang="ts">
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { wikiArticles, wikiById, wikiOverview } from '$lib/generated/wiki';
  import type { WikiArticle } from '$lib/generated/wiki';
  import { navigate } from '$lib/utils';
  import { t } from '$lib/i18n';
  import ScopeBadges from '$lib/components/ScopeBadges.svelte';
  import WikiArticleView from './WikiArticleView.svelte';

  const navArticles = $derived([wikiOverview, ...wikiArticles]);

  const navGroups = $derived.by(() => {
    const groups: { id: string; articles: WikiArticle[] }[] = [];
    for (const article of navArticles) {
      const id = article.groupId ?? '';
      const last = groups[groups.length - 1];
      if (last && last.id === id) {
        last.articles.push(article);
        continue;
      }
      groups.push({ id, articles: [article] });
    }
    return groups;
  });

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

  function articleLabel(article: WikiArticle): string {
    return article.id === 'overview' ? 'Overview' : article.title;
  }
</script>

<div class="wiki-manual">
  <div class="wiki-layout">
    <nav class="wiki-nav settings-nav" aria-label="Manual sections">
      {#each navGroups as group (group.id + ':' + group.articles[0].id)}
        {#if group.id}
          <p class="settings-nav-group-label">{t(`dashboard.settings_group_${group.id}`)}</p>
        {/if}
        {#each group.articles as article (article.id)}
          <button
            type="button"
            class="wiki-nav-item settings-nav-item {activeArticle.id === article.id
              ? 'settings-nav-item-active'
              : ''}"
            onclick={() => selectArticle(article.id)}
          >
            <span class="settings-nav-label">{articleLabel(article)}</span>
            {#if article.scopes.length > 0}
              <ScopeBadges scopes={article.scopes} size="sm" />
            {/if}
          </button>
        {/each}
      {/each}
    </nav>

    <div class="wiki-content settings-content">
      <WikiArticleView article={activeArticle} />
    </div>
  </div>
</div>
