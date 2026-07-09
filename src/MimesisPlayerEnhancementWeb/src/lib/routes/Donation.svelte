<script lang="ts">
  import { t } from '$lib/i18n';
</script>

<div class="card mx-auto max-w-xl p-8 text-center">
  <h2 class="mb-2 text-xl font-semibold">{t('dashboard.donation_heading')}</h2>
  <p class="mb-6 text-sm text-gray-600 dark:text-gray-300">{t('dashboard.donation_intro')}</p>
  <a
    class="btn btn-primary"
    href="https://www.paypal.com/donate/?hosted_button_id=YOUR_BUTTON"
    target="_blank"
    rel="noopener noreferrer"
  >
    {t('dashboard.donation_button')}
  </a>
</div>
