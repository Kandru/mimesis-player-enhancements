<script lang="ts">
  import { t } from '$lib/i18n';

  const reasons = [
    { titleKey: 'donation_reason_time_title', descKey: 'donation_reason_time' },
    { titleKey: 'donation_reason_ai_title', descKey: 'donation_reason_ai' },
    { titleKey: 'donation_reason_community_title', descKey: 'donation_reason_community' },
  ] as const;

  const paypalUrl = 'https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG';
  const kofiUrl = 'https://ko-fi.com/nerdiacs';
</script>

<article class="donation-page">
  <header class="donation-hero card">
    <div class="donation-hero-icon" aria-hidden="true">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
        <path
          d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"
        />
      </svg>
    </div>
    <h1 class="donation-heading">{t('dashboard.donation_heading')}</h1>
    <p class="donation-lead">{t('dashboard.donation_lead')}</p>
  </header>

  <section class="donation-reasons" aria-label={t('dashboard.donation_why_title')}>
    <h2 class="donation-reasons-title">{t('dashboard.donation_why_title')}</h2>
    <div class="donation-reasons-grid">
      {#each reasons as reason (reason.titleKey)}
        <div class="donation-reason-card card">
          <h3 class="donation-reason-title">{t(`dashboard.${reason.titleKey}`)}</h3>
          <p class="donation-reason-desc">{t(`dashboard.${reason.descKey}`)}</p>
        </div>
      {/each}
    </div>
  </section>

  <section class="donation-cta card">
    <p class="donation-cta-prompt">{t('dashboard.donation_cta')}</p>

    <div class="donation-buttons">
      <a
        class="donation-btn donation-btn-paypal"
        href={paypalUrl}
        target="_blank"
        rel="noopener noreferrer"
      >
        <svg class="donation-btn-icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path
            d="M7.076 21.337H2.47a.641.641 0 0 1-.632-.774l2.552-16.175a.765.765 0 0 1 .753-.646h7.064c3.132 0 5.405 1.117 6.586 3.213.59 1.093.807 2.354.64 3.748-.168 1.394-.716 2.592-1.63 3.563-.914.97-2.12 1.68-3.588 2.11-1.468.43-3.14.646-5.016.646H9.64l-.564 3.575z"
          />
        </svg>
        {t('dashboard.donation_button_paypal')}
      </a>

      <a
        class="donation-btn donation-btn-kofi"
        href={kofiUrl}
        target="_blank"
        rel="noopener noreferrer"
      >
        <svg class="donation-btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" aria-hidden="true">
          <path d="M18 8h1a4 4 0 0 1 0 8h-1" />
          <path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4V8z" />
          <line x1="6" y1="1" x2="6" y2="4" />
          <line x1="10" y1="1" x2="10" y2="4" />
          <line x1="14" y1="1" x2="14" y2="4" />
        </svg>
        {t('dashboard.donation_button_kofi')}
      </a>
    </div>

    <p class="donation-footnote">{t('dashboard.donation_footnote')}</p>
  </section>
</article>
