import type { ItemOptionDto } from './types';

export interface ItemCatalogEntry {
  key: string;
  itemId: string;
  percent?: number;
  label: string;
  type: string;
}

const CATEGORY_ORDER = ['Consumable', 'Equipment', 'Miscellany', 'Developer'] as const;

const CATEGORY_LABEL_KEYS: Record<string, string> = {
  Consumable: 'dashboard.spawn_item_category_consumable',
  Equipment: 'dashboard.spawn_item_category_equipment',
  Miscellany: 'dashboard.spawn_item_category_miscellany',
  Developer: 'dashboard.spawn_item_category_developer',
};

export function encodeItemSelection(itemId: string, percent?: number) {
  return percent != null ? `${itemId}:${percent}` : itemId;
}

export function parseItemSelection(key: string): { itemId: string; percent?: number } {
  const idx = key.lastIndexOf(':');
  if (idx <= 0) return { itemId: key };
  const itemId = key.slice(0, idx);
  const percent = Number.parseInt(key.slice(idx + 1), 10);
  return {
    itemId,
    percent: Number.isFinite(percent) ? percent : undefined,
  };
}

export function flattenItemCatalog(items: ItemOptionDto[]): ItemCatalogEntry[] {
  const entries: ItemCatalogEntry[] = [];
  for (const item of items) {
    const type = item.type || 'Miscellany';
    if (item.variants?.length) {
      for (const variant of item.variants) {
        entries.push({
          key: encodeItemSelection(item.id, variant.percent),
          itemId: item.id,
          percent: variant.percent,
          label: `${item.label} (${variant.percent}%)`,
          type,
        });
      }
    } else {
      entries.push({
        key: item.id,
        itemId: item.id,
        label: item.label,
        type,
      });
    }
  }
  return entries;
}

export function getItemCatalogGroups(
  items: ItemOptionDto[],
  t: (key: string, params?: Record<string, string | number>) => string,
) {
  const buckets: Record<string, ItemCatalogEntry[]> = {};
  for (const entry of flattenItemCatalog(items)) {
    (buckets[entry.type] ??= []).push(entry);
  }
  return CATEGORY_ORDER.filter((id) => buckets[id]?.length).map((id) => ({
    id,
    label: t(CATEGORY_LABEL_KEYS[id] || id),
    entries: buckets[id],
  }));
}

export function defaultItemSelectionKey(items: ItemOptionDto[]) {
  return flattenItemCatalog(items)[0]?.key ?? '';
}
