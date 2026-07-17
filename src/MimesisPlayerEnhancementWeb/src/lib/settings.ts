import type { ConfigEntryDto, ConfigSectionDto, ItemOptionDto, SettingsDto } from './types';
import { t } from './i18n';
import { parseCsv } from './listValue';
import {
  buildDungeonPickerOptions,
  buildItemPickerOptions,
  buildVariantPickerOptions,
  buildWeatherPresetOptions,
  resolvePickerLabels,
} from './pickerOptions';

export interface ConfigEntryGroup {
  id: string;
  label: string;
  entries: ConfigEntryDto[];
}

export interface SettingsSearchContext {
  itemCatalog: ItemOptionDto[];
  dungeonCatalog: Array<{ id: string; label: string }>;
}

function entryPickerSearchLabels(
  entry: ConfigEntryDto,
  searchContext?: SettingsSearchContext,
): string[] {
  const ids = parseCsv(entry.value);
  if (ids.length === 0) {
    return [];
  }

  switch (entry.inputKind) {
    case 'ItemIdList':
      return resolvePickerLabels(
        buildItemPickerOptions(searchContext?.itemCatalog ?? [], t),
        ids,
      );
    case 'DungeonIdList':
      return resolvePickerLabels(
        buildDungeonPickerOptions(searchContext?.dungeonCatalog ?? []),
        ids,
      );
    case 'WeatherPresetList':
      return resolvePickerLabels(buildWeatherPresetOptions(t), ids);
    case 'VariantIdList':
      return resolvePickerLabels(buildVariantPickerOptions(entry.selectOptions), ids);
    default:
      if (entry.inputKind === 'Select' && entry.selectOptions.length > 0) {
        return resolvePickerLabels(buildVariantPickerOptions(entry.selectOptions), ids);
      }
      return [];
  }
}

export function settingsHaystack(
  entry: ConfigEntryDto,
  sectionTitle: string,
  searchContext?: SettingsSearchContext,
) {
  return [
    entry.key,
    entry.title,
    entry.description,
    entry.value,
    entry.type,
    entry.defaultValue,
    entry.globalValue,
    sectionTitle,
    ...entryPickerSearchLabels(entry, searchContext),
  ].map((v) => String(v ?? '').toLowerCase());
}

export function matchesSettingsQuery(
  entry: ConfigEntryDto,
  sectionTitle: string,
  query: string,
  searchContext?: SettingsSearchContext,
) {
  if (!query) return true;
  return settingsHaystack(entry, sectionTitle, searchContext).some((v) => v.includes(query));
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
  searchContext?: SettingsSearchContext,
) {
  const normalizedQuery = query.trim().toLowerCase();
  const titleMatches = normalizedQuery.length > 0 && (
    section.title.toLowerCase().includes(normalizedQuery)
    || (section.description?.toLowerCase().includes(normalizedQuery) ?? false)
  );

  if (section.featureToggle && matchesSettingsQuery(section.featureToggle, section.title, query, searchContext)) {
    return true;
  }

  if (!featureEnabled(section, settings)) {
    if (!section.featureToggle) {
      return section.entries.some((e) => entryVisible(section, e, settings));
    }
    return normalizedQuery.length === 0
      || matchesSettingsQuery(section.featureToggle, section.title, query, searchContext)
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

    if (matchesSettingsQuery(e, section.title, query, searchContext)) {
      return true;
    }

    if (!query || !e.entryGroup) {
      return false;
    }

    const label = configEntryGroupLabel(section.id, e.entryGroup);
    return label.toLowerCase().includes(query.trim().toLowerCase());
  });
}

export function entryIsModified(entry: ConfigEntryDto, scope: 'global' | 'save') {
  return scope === 'save' ? entry.isOverridden : settingDiffersFromDefault(entry);
}

export function sectionHasModifiedEntries(
  section: ConfigSectionDto,
  settings: SettingsDto | null,
  scope: 'global' | 'save',
) {
  const candidates = [
    ...(section.featureToggle ? [section.featureToggle] : []),
    ...section.entries.filter((entry) => entryVisible(section, entry, settings)),
  ];
  return candidates.some((entry) => entryIsModified(entry, scope));
}

export function sectionResettableEntries(
  section: ConfigSectionDto,
  settings: SettingsDto | null,
  scope: 'global' | 'save',
  query: string,
  isHost: boolean,
  isConnected: boolean,
  searchContext?: SettingsSearchContext,
) {
  const entries: ConfigEntryDto[] = [];
  const candidates = [
    ...(section.featureToggle ? [section.featureToggle] : []),
    ...section.entries.filter(
      (entry) => entryVisible(section, entry, settings) && matchesSettingsQuery(entry, section.title, query, searchContext),
    ),
  ];

  for (const entry of candidates) {
    if (!entryIsModified(entry, scope)) continue;
    if (!entryEditable(section, entry, settings, scope, isHost, isConnected)) continue;
    entries.push(entry);
  }

  return entries;
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

export function entryScopes(entry: ConfigEntryDto): ('host' | 'local')[] {
  return entry.hasLocalEffect ? ['local'] : ['host'];
}

export function sectionScopes(
  section: ConfigSectionDto,
  settings: SettingsDto | null,
): ('host' | 'local')[] {
  const candidates = [
    ...(section.featureToggle ? [section.featureToggle] : []),
    ...section.entries,
  ];
  const visible = candidates.filter((entry) => entryVisible(section, entry, settings));
  const hasLocal = visible.some((entry) => entry.hasLocalEffect);
  const hasHost = visible.some((entry) => !entry.hasLocalEffect);
  return [...(hasHost ? (['host'] as const) : []), ...(hasLocal ? (['local'] as const) : [])];
}

export function normalizeBoolInput(value: string) {
  return value === 'true' || value === 'True' || value === '1';
}

export function formatDefaultHint(entry: ConfigEntryDto) {
  return t('dashboard.settings_default_hint', { value: entry.defaultValue });
}

export function formatGlobalHint(entry: ConfigEntryDto) {
  return t('dashboard.settings_global_hint', { value: entry.globalValue });
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
  searchContext?: SettingsSearchContext,
): ConfigEntryGroup[] {
  const visible = section.entries.filter(
    (entry) => entryVisible(section, entry, settings) && matchesSettingsQuery(entry, section.title, query, searchContext),
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
        || matchesSettingsQuery(item, section.title, query, searchContext),
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
