<script lang="ts">
  import SettingsPanel from '$lib/components/settings/SettingsPanel.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
</script>

<SettingsPanel
  settings={dashboard.settingsGlobal}
  scope="global"
  heading={t('dashboard.settings_global_heading')}
  intro={t('dashboard.settings_intro_global')}
/>
