<script lang="ts">
  import { t } from '$lib/i18n';
</script>

<article class="donation-page">
  <div class="donation-card">
    <h2 class="mb-1 text-xl font-semibold">{t('dashboard.donation_heading')}</h2>
    <p class="donation-lead">{t('dashboard.donation_lead')}</p>

    <div class="donation-body">
      <p>{t('dashboard.donation_body1')}</p>
      <p class="donation-note">{t('dashboard.donation_ai_note')}</p>
      <p>{t('dashboard.donation_body2')}</p>
    </div>

    <div class="donation-action">
      <a
        class="btn btn-primary"
        href="https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG"
        target="_blank"
        rel="noopener noreferrer"
      >
        {t('dashboard.donation_button')}
      </a>
    </div>

    <p class="donation-footnote">{t('dashboard.donation_footnote')}</p>
  </div>
</article>
