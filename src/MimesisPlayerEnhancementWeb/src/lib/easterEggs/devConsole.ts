import { getAllEasterEggIds } from './registry';

export interface EasterEggDevApi {
  set: (id: string) => boolean;
  reset: () => void;
  list: () => string[];
}

declare global {
  interface Window {
    easteregg?: (id?: string) => boolean | string[];
  }
}

export function registerDevConsole(api: EasterEggDevApi) {
  if (window.easteregg) return;

  window.easteregg = (id?: string) => {
    if (id === undefined) {
      api.reset();
      return api.list();
    }
    const ok = api.set(id);
    if (!ok) console.warn('unknown id');
    return ok;
  };
}
