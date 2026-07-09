<script lang="ts">
  import { t } from '$lib/i18n';
</script>

<div class="flex min-h-[50vh] items-center justify-center">
  <div class="text-center">
    <div class="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-[var(--brand)]"></div>
    <p class="text-gray-600 dark:text-gray-300">{t('dashboard.waiting_for_session')}</p>
  </div>
</div>
