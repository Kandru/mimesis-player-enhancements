import type { ItemOptionDto } from './types';

export interface ItemCatalogEntry {
  key: string;
  itemId: string;
  percent?: number;
  label: string;
  type: string;
}

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
export function defaultItemSelectionKey(items: ItemOptionDto[]) {
  return flattenItemCatalog(items)[0]?.key ?? '';
}
