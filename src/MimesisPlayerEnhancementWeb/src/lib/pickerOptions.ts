import type { ConfigEntryDto, ConfigSelectOption, ItemOptionDto } from './types';
import { flattenItemCatalog } from './itemCatalogHelpers';

export interface PickerOption {
  value: string;
  label: string;
  group?: string;
}

const CATEGORY_ORDER = ['Consumable', 'Equipment', 'Miscellany', 'Developer'] as const;

const CATEGORY_LABEL_KEYS: Record<string, string> = {
  Consumable: 'dashboard.spawn_item_category_consumable',
  Equipment: 'dashboard.spawn_item_category_equipment',
  Miscellany: 'dashboard.spawn_item_category_miscellany',
  Developer: 'dashboard.spawn_item_category_developer',
};

function categoryLabel(type: string | undefined, t: (key: string) => string): string {
  const key = type || 'Miscellany';
  return t(CATEGORY_LABEL_KEYS[key] || key);
}

function sortByCategoryThenLabel(options: PickerOption[]): PickerOption[] {
  const order = (group?: string) => {
    const index = CATEGORY_ORDER.indexOf(group as (typeof CATEGORY_ORDER)[number]);
    return index >= 0 ? index : CATEGORY_ORDER.length;
  };
  return options.sort((a, b) => {
    const diff = order(a.group) - order(b.group);
    if (diff !== 0) return diff;
    return a.label.localeCompare(b.label, undefined, { sensitivity: 'base' });
  });
}

/** Options for config allowlists/blocklists — values are numeric master IDs. */
export function buildItemPickerOptions(
  items: ItemOptionDto[],
  t: (key: string) => string,
): PickerOption[] {
  const options: PickerOption[] = [];

  for (const item of items) {
    const group = categoryLabel(item.type, t);
    if (item.variants?.length) {
      for (const variant of item.variants) {
        options.push({
          value: String(variant.masterId),
          label: `${item.label} (${variant.percent}%)`,
          group,
        });
      }
    } else {
      options.push({
        value: item.masterId != null ? String(item.masterId) : item.id,
        label: item.label,
        group,
      });
    }
  }

  return sortByCategoryThenLabel(options);
}

/** Options for the give-item dialog — values are catalog selection keys (`itemId[:percent]`). */
export function buildGiveItemPickerOptions(
  items: ItemOptionDto[],
  t: (key: string) => string,
): PickerOption[] {
  return sortByCategoryThenLabel(
    flattenItemCatalog(items).map((entry) => ({
      value: entry.key,
      label: entry.label,
      group: categoryLabel(entry.type, t),
    })),
  );
}

export function buildDungeonPickerOptions(
  dungeons: Array<{ id: string; label: string }>,
): PickerOption[] {
  return dungeons
    .map((dungeon) => ({ value: dungeon.id, label: dungeon.label }))
    .sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: 'base' }));
}

const WEATHER_PRESET_IDS = ['Sunny', 'Rain', 'HeavyRain', 'Squall'] as const;

export function buildWeatherPresetOptions(
  t: (key: string) => string,
): PickerOption[] {
  return WEATHER_PRESET_IDS.map((id) => ({
    value: id,
    label: t(`config.MimesisPlayerEnhancement_Weather.FixedWeatherPreset.options.${id}`),
  }));
}

export function buildVariantPickerOptions(selectOptions: ConfigSelectOption[]): PickerOption[] {
  return selectOptions.map((opt) => ({
    value: opt.value,
    label: opt.label,
  }));
}

export function resolvePickerLabels(options: PickerOption[], ids: readonly string[]): string[] {
  return ids.map((id) => options.find((opt) => opt.value === id)?.label ?? id);
}

export function matchesPickerQuery(option: PickerOption, query: string): boolean {
  if (!query) return true;
  const q = query.toLowerCase();
  return option.value.toLowerCase().includes(q) || option.label.toLowerCase().includes(q);
}

/** Variant selects are always searchable; other selects only when they have many options. */
const SEARCHABLE_VARIANT_KEYS = new Set([
  'RoundStartSoundVariant',
  'CustomLoadingScreenVariant',
]);

const SEARCHABLE_SELECT_MIN_OPTIONS = 10;

export function isSearchableSelectEntry(entry: ConfigEntryDto): boolean {
  if (entry.inputKind !== 'Select') return false;
  return SEARCHABLE_VARIANT_KEYS.has(entry.key)
    || entry.selectOptions.length >= SEARCHABLE_SELECT_MIN_OPTIONS;
}
