<script lang="ts">
  import { onMount } from 'svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { changelogHtml } from '$lib/generated/changelog';

  let error = $state('');

  onMount(() => {
    if (!dashboard.changelogPending) return;
    void dashboard.acknowledgeChangelogIfPending().then((message) => {
      if (message) {
        error = message;
      }
    });
  });
</script>

<article class="changelog-page">
  <div class="changelog-layout">
    <article class="wiki-article-card card changelog-article">
      <header class="wiki-article-header">
        <h1 class="wiki-article-title">{t('dashboard.changelog_heading')}</h1>
        {#if dashboard.status.modVersion}
          <span class="wiki-scope-badge wiki-scope-host">v{dashboard.status.modVersion}</span>
        {/if}
      </header>

      {#if error}
        <p class="changelog-error" role="alert">{error}</p>
      {/if}

      <p class="changelog-lead">
        {t('dashboard.changelog_lead', { version: dashboard.status.modVersion })}
      </p>

      <div class="wiki-prose">
        {@html changelogHtml}
      </div>
    </article>
  </div>
</article>
