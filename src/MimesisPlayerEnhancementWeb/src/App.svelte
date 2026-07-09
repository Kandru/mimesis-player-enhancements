<script lang="ts">
  import { onMount } from 'svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import AppShell from '$lib/components/layout/AppShell.svelte';
  import WaitingPage from '$lib/routes/Waiting.svelte';
  import DonationPage from '$lib/routes/Donation.svelte';
  import PlayersPage from '$lib/routes/Players.svelte';
  import MinimapPage from '$lib/routes/MinimapPage.svelte';
  import LeaderboardPage from '$lib/routes/LeaderboardPage.svelte';
  import SettingsProfilePage from '$lib/routes/SettingsProfile.svelte';
  import SettingsCustomizePage from '$lib/routes/SettingsCustomize.svelte';
  import GlobalSettingsPage from '$lib/routes/GlobalSettingsPage.svelte';
  import PlayerDetailPage from '$lib/routes/PlayerDetail.svelte';
  import Toast from '$lib/components/Toast.svelte';
  import { t } from '$lib/i18n';

  onMount(() => dashboard.init());
</script>

<svelte:head>
  <title>{dashboard.status.lobbyName?.trim() || t('dashboard.title_default')}</title>
</svelte:head>

<AppShell>
  {#if dashboard.route === 'waiting'}
    <WaitingPage />
  {:else if dashboard.route === 'donation'}
    <DonationPage />
  {:else if dashboard.route === 'players'}
    <PlayersPage />
  {:else if dashboard.route === 'minimap'}
    <MinimapPage />
  {:else if dashboard.route === 'leaderboard'}
    <LeaderboardPage />
  {:else if dashboard.route === 'settings' && dashboard.settingsSubRoute === 'customize'}
    <SettingsCustomizePage />
  {:else if dashboard.route === 'settings'}
    <SettingsProfilePage />
  {:else if dashboard.route === 'global-settings'}
    <GlobalSettingsPage />
  {:else if dashboard.route === 'player'}
    <PlayerDetailPage />
  {:else}
    <WaitingPage />
  {/if}
</AppShell>

<Toast />
