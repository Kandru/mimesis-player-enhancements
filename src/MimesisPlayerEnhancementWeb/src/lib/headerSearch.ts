import { t } from './i18n';

export function isHeaderSearchVisible(
  route: string,
  settingsSubRoute = '',
  saveProfileMode = '',
) {
  if (route === 'players') return true;
  if (route === 'leaderboard') return true;
  if (route === 'global-settings') return true;
  if (route === 'settings' && (settingsSubRoute === 'customize' || saveProfileMode === 'custom')) {
    return true;
  }
  return false;
}

export function getHeaderSearchPlaceholder(
  route: string,
  settingsSubRoute = '',
  saveProfileMode = '',
) {
  if (route === 'players') return t('dashboard.table_search');
  if (route === 'leaderboard') return t('dashboard.leaderboard_search');
  if (
    route === 'global-settings'
    || (route === 'settings' && (settingsSubRoute === 'customize' || saveProfileMode === 'custom'))
  ) {
    return t('dashboard.settings_search_placeholder');
  }
  return t('dashboard.header_search_placeholder');
}
