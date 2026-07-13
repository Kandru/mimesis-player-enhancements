<script lang="ts">
  import { t } from '$lib/i18n';
  import { triggerDevVerify } from './devVerifyEffect';

  function handleClick(event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    triggerDevVerify();
  }
</script>

<button
  type="button"
  class="badge badge-dev"
  aria-label={t('dashboard.badge_dev_verify')}
  onclick={handleClick}
  ontouchstart={(event) => event.stopPropagation()}
>
  {t('dashboard.badge_dev')}
</button>
