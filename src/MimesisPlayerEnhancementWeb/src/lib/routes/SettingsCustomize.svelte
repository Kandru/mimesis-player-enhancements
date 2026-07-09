<script lang="ts">
  import SettingsPanel from '$lib/components/settings/SettingsPanel.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
</script>

<SettingsPanel
  settings={dashboard.settingsSave}
  scope="save"
  heading={t('dashboard.settings_customize_heading')}
  intro={t('dashboard.settings_intro_save')}
/>
