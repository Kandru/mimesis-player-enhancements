import type { ConfigEntryDto, ConfigSectionDto, SettingsDto } from './types';
import { t } from './i18n';

export interface ConfigEntryGroup {
  id: string;
  label: string;
  entries: ConfigEntryDto[];
}

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

export function entryEditable(
  section: ConfigSectionDto,
  entry: ConfigEntryDto,
  settings: SettingsDto | null,
  scope: 'global' | 'save',
  isHost: boolean,
  isConnected: boolean,
) {
  return featureEnabled(section, settings) && canEditEntry(entry, scope, isHost, isConnected);
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
    if (!section.featureToggle) {
      return section.entries.some((e) => entryVisible(section, e, settings));
    }
    return normalizedQuery.length === 0
      || matchesSettingsQuery(section.featureToggle, section.title, query)
      || titleMatches
      || section.entries.some((e) => entryVisible(section, e, settings));
  }

  if (titleMatches) {
    return section.entries.some((e) => entryVisible(section, e, settings));
  }

  return section.entries.some((e) => {
    if (!entryVisible(section, e, settings)) {
      return false;
    }

    if (matchesSettingsQuery(e, section.title, query)) {
      return true;
    }

    if (!query || !e.entryGroup) {
      return false;
    }

    const label = configEntryGroupLabel(section.id, e.entryGroup);
    return label.toLowerCase().includes(query.trim().toLowerCase());
  });
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

export function configEntryGroupId(entryGroup: string) {
  const separator = entryGroup.indexOf('::');
  return separator >= 0 ? entryGroup.slice(separator + 2) : entryGroup;
}

export function configEntryGroupLabel(sectionId: string, entryGroup: string) {
  const groupId = configEntryGroupId(entryGroup);
  const key = `config.${sectionId}._groups.${groupId}`;
  const label = t(key);
  return label !== key ? label : groupId;
}

export function groupConfigEntries(
  section: ConfigSectionDto,
  settings: SettingsDto | null,
  query: string,
): ConfigEntryGroup[] {
  const visible = section.entries.filter(
    (entry) => entryVisible(section, entry, settings) && matchesSettingsQuery(entry, section.title, query),
  );
  if (visible.length === 0) {
    return [];
  }

  const grouped = new Map<string, ConfigEntryDto[]>();
  const ungrouped: ConfigEntryDto[] = [];

  for (const entry of visible) {
    if (!entry.entryGroup) {
      ungrouped.push(entry);
      continue;
    }

    const list = grouped.get(entry.entryGroup) ?? [];
    list.push(entry);
    grouped.set(entry.entryGroup, list);
  }

  const groups: ConfigEntryGroup[] = [];
  const seen = new Set<string>();

  for (const entry of visible) {
    if (!entry.entryGroup || seen.has(entry.entryGroup)) {
      continue;
    }

    seen.add(entry.entryGroup);
    const label = configEntryGroupLabel(section.id, entry.entryGroup);
    const groupMatches = !query || label.toLowerCase().includes(query.trim().toLowerCase());
    const entries = (grouped.get(entry.entryGroup) ?? []).filter(
      (item) =>
        groupMatches
        || matchesSettingsQuery(item, section.title, query),
    );
    if (entries.length === 0) {
      continue;
    }

    groups.push({
      id: entry.entryGroup,
      label,
      entries,
    });
  }

  if (ungrouped.length > 0) {
    groups.push({
      id: '',
      label: '',
      entries: ungrouped,
    });
  }

  return groups;
}
