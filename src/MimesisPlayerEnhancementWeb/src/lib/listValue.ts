export function parseCsv(value: string | undefined | null): string[] {
  if (!value?.trim()) return [];
  return value
    .split(',')
    .map((part) => part.trim())
    .filter((part) => part.length > 0);
}

export function formatCsv(ids: readonly string[]): string {
  return ids.join(',');
}

export function reorderOrdered(values: readonly string[], fromIndex: number, toIndex: number): string[] {
  if (fromIndex < 0 || toIndex < 0 || fromIndex >= values.length || toIndex >= values.length) {
    return [...values];
  }
  const next = [...values];
  const [moved] = next.splice(fromIndex, 1);
  next.splice(toIndex, 0, moved);
  return next;
}
