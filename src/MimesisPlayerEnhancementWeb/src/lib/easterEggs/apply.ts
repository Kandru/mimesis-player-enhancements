import { dashboard } from '$lib/stores/dashboard.svelte';
import { applyOverlayToDom } from './overlayDom';
import { registerDevConsole } from './devConsole';
import { easterEggRegistry, getAllEasterEggIds } from './registry';
import { resolveActiveEasterEgg } from './resolver';
import type { EasterEggDefinition, EasterEggId } from './types';

let forcedEgg: EasterEggDefinition | null = null;
let appliedCssClass: string | null = null;

function applyToDom(egg: EasterEggDefinition | null) {
  if (appliedCssClass) {
    document.documentElement.classList.remove(appliedCssClass);
    appliedCssClass = null;
  }

  if (egg) {
    document.documentElement.classList.add(egg.cssClass);
    appliedCssClass = egg.cssClass;
  }

  dashboard.activeEasterEggId = egg?.id ?? null;
  dashboard.activeEasterEggOverlay = egg?.overlay ?? null;
  dashboard.activeEasterEggFlavorKey = egg?.flavorKey ?? null;
  applyOverlayToDom(egg?.overlay);
}

export function setEasterEgg(id: EasterEggId | string): boolean {
  const egg = easterEggRegistry.find((e) => e.id === id) ?? null;
  if (!egg) return false;
  forcedEgg = egg;
  applyToDom(egg);
  return true;
}

export function clearEasterEgg() {
  forcedEgg = null;
  const egg = resolveActiveEasterEgg(new Date());
  applyToDom(egg);
}

export function resetEasterEgg() {
  forcedEgg = null;
  applyToDom(null);
}

export function initEasterEgg() {
  registerDevConsole({
    set(id: string) {
      return setEasterEgg(id);
    },
    reset: resetEasterEgg,
    list: getAllEasterEggIds,
  });
  const egg = forcedEgg ?? resolveActiveEasterEgg(new Date());
  applyToDom(egg);
}
