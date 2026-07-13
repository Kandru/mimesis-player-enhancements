import { easterEggRegistry, getEasterEggById } from './registry';
import type { EasterEggDefinition } from './types';

function resolveQueryOverride(): EasterEggDefinition | null {
  try {
    const id = new URLSearchParams(location.search).get('egg');
    if (!id) return null;
    return getEasterEggById(id);
  } catch {
    return null;
  }
}

export function resolveActiveEasterEgg(date: Date): EasterEggDefinition | null {
  const queryOverride = resolveQueryOverride();
  if (queryOverride) return queryOverride;

  const sorted = [...easterEggRegistry].sort((a, b) => a.priority - b.priority);
  return sorted.find((egg) => egg.isActive(date)) ?? null;
}
