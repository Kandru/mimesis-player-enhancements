import type { ConfigEntryDto, ConfigSectionDto, SettingsDto } from './types';

export function settingsHaystack(entry: ConfigEntryDto, sectionTitle: string) {
  return [
    entry.key,
    entry.title,
    entry.description,
    entry.value,
    entry.type,
    entry.defaultValue,
    entry.globalValue,
    sectionTitle,
  ].map((v) => String(v ?? '').toLowerCase());
}

export function matchesSettingsQuery(
  entry: ConfigEntryDto,
  sectionTitle: string,
  query: string,
) {
  if (!query) return true;
  return settingsHaystack(entry, sectionTitle).some((v) => v.includes(query));
}

export function featureEnabled(section: ConfigSectionDto, settings: SettingsDto | null) {
  if (!section.featureToggle) return true;
  const toggle = section.featureToggle;
  const val = toggle.value ?? toggle.defaultValue;
  return val === 'true' || val === 'True' || val === '1';
}

export function entryVisible(
  section: ConfigSectionDto,
  entry: ConfigEntryDto,
  settings: SettingsDto | null,
) {
  if (entry.isHidden) return false;
  if (!featureEnabled(section, settings)) return false;
  if (!entry.dependsOnKey) return true;
  const all = [
    ...(section.featureToggle ? [section.featureToggle] : []),
    ...section.entries,
  ];
  const dep = all.find((e) => e.key === entry.dependsOnKey);
  if (!dep) return true;
  if (entry.dependsOnValue != null) return dep.value === entry.dependsOnValue;
  return dep.value === 'true' || dep.value === 'True';
}

export function sectionHasVisibleEntries(
  section: ConfigSectionDto,
  settings: SettingsDto | null,
  query: string,
) {
  const normalizedQuery = query.trim().toLowerCase();
  const titleMatches = normalizedQuery.length > 0 && section.title.toLowerCase().includes(normalizedQuery);

  if (section.featureToggle && matchesSettingsQuery(section.featureToggle, section.title, query)) {
    return true;
  }

  if (!featureEnabled(section, settings)) {
    if (!section.featureToggle) return false;
    return normalizedQuery.length === 0
      || matchesSettingsQuery(section.featureToggle, section.title, query)
      || titleMatches;
  }

  if (titleMatches) {
    return section.entries.some((e) => entryVisible(section, e, settings));
  }

  return section.entries.some(
    (e) => entryVisible(section, e, settings) && matchesSettingsQuery(e, section.title, query),
  );
}

export function guestSectionVisible(section: ConfigSectionDto) {
  if (section.featureToggle?.hasLocalEffect) return true;
  return section.entries.some((e) => e.hasLocalEffect);
}

export function canEditEntry(
  entry: ConfigEntryDto,
  scope: 'global' | 'save',
  isHost: boolean,
  isConnected: boolean,
) {
  if (scope === 'save') return isConnected && isHost;
  if (!isConnected || isHost) return true;
  return entry.hasLocalEffect;
}

export function normalizeBoolInput(value: string) {
  return value === 'true' || value === 'True' || value === '1';
}

export function formatDefaultHint(entry: ConfigEntryDto) {
  return `Default: ${entry.defaultValue}`;
}

export function formatGlobalHint(entry: ConfigEntryDto) {
  return `Global: ${entry.globalValue}`;
}

export function settingDiffersFromDefault(entry: ConfigEntryDto) {
  return entry.value !== entry.defaultValue;
}

export function settingDiffersFromGlobal(entry: ConfigEntryDto) {
  return entry.value !== entry.globalValue;
}
