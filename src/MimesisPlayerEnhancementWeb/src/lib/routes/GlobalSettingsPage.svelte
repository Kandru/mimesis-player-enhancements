<script lang="ts">
  import SettingsPanel from '$lib/components/settings/SettingsPanel.svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
</script>

<SettingsPanel settings={dashboard.settingsGlobal} scope="global" />
