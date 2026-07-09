import { t } from './i18n';

export function getPageTitle(
  route: string,
  settingsSubRoute = '',
  playerName?: string | null,
): string {
  switch (route) {
    case 'home':
      return t('dashboard.home_title');
    case 'donation':
      return t('dashboard.nav_donation');
    case 'global-settings':
      return t('dashboard.settings_global_heading');
    case 'players':
      return t('dashboard.nav_players');
    case 'minimap':
      return t('dashboard.nav_minimap');
    case 'leaderboard':
      return t('dashboard.nav_statistics');
    case 'settings':
      return settingsSubRoute === 'customize'
        ? t('dashboard.settings_customize_heading')
        : t('dashboard.nav_settings');
    case 'player':
      return playerName?.trim() || t('dashboard.player');
    default:
      return t('dashboard.title_default');
  }
}
