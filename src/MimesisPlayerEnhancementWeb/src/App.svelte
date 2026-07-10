<script lang="ts">
  import { onMount } from 'svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import AppShell from '$lib/components/layout/AppShell.svelte';
  import LobbyShell from '$lib/components/layout/LobbyShell.svelte';
  import HomePage from '$lib/routes/HomePage.svelte';
  import DonationPage from '$lib/routes/Donation.svelte';
  import PlayersPage from '$lib/routes/Players.svelte';
  import MinimapPage from '$lib/routes/MinimapPage.svelte';
  import LeaderboardPage from '$lib/routes/LeaderboardPage.svelte';
  import SettingsProfilePage from '$lib/routes/SettingsProfile.svelte';
  import GlobalSettingsPage from '$lib/routes/GlobalSettingsPage.svelte';
  import PlayerDetailPage from '$lib/routes/PlayerDetail.svelte';
  import Toast from '$lib/components/Toast.svelte';
  import { isLobbyRoute } from '$lib/playerHelpers';
  import { getPageTitle } from '$lib/pageTitles';

  const pageTitle = $derived.by(() =>
    getPageTitle(
      dashboard.route,
      dashboard.settingsSubRoute,
      dashboard.playerStats?.displayName || dashboard.playerStats?.steamId,
    ),
  );

  onMount(() => dashboard.init());
</script>

<svelte:head>
  <title>{pageTitle}</title>
</svelte:head>

<AppShell>
  {#if dashboard.route === 'home'}
    <HomePage />
  {:else if dashboard.route === 'donation'}
    <DonationPage />
  {:else if dashboard.route === 'global-settings'}
    <GlobalSettingsPage />
  {:else if isLobbyRoute(dashboard.route)}
    <LobbyShell>
      {#if dashboard.route === 'players'}
        <PlayersPage />
      {:else if dashboard.route === 'minimap'}
        <MinimapPage />
      {:else if dashboard.route === 'leaderboard'}
        <LeaderboardPage />
      {:else if dashboard.route === 'settings'}
        <SettingsProfilePage />
      {:else if dashboard.route === 'player'}
        <PlayerDetailPage />
      {/if}
    </LobbyShell>
  {:else}
    <HomePage />
  {/if}
</AppShell>

<Toast />
