let localeVersion = $state(0);
let locale = $state('en');
let messages = $state<Record<string, unknown>>({});

function lookup(key: string): unknown {
  const parts = key.split('.');
  let node: unknown = messages;
  for (const part of parts) {
    if (node == null || typeof node !== 'object') return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return node;
}

function interpolate(template: string, params?: Record<string, string | number>) {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (_, name) => String(params[name] ?? `{${name}}`));
}

function normalizeLanguageTag(lang?: string): string {
  if (!lang) return '';
  const normalized = lang.trim().toLowerCase().replace('_', '-');
  const dash = normalized.indexOf('-');
  return dash > 0 ? normalized.slice(0, dash) : normalized;
}

function resolveLocaleCandidates(lang?: string): string[] {
  const candidates: string[] = [];
  const add = (value?: string) => {
    const code = normalizeLanguageTag(value);
    if (code && !candidates.includes(code)) candidates.push(code);
  };

  add(lang);
  if (typeof navigator !== 'undefined') {
    add(navigator.language);
    for (const pref of navigator.languages ?? []) add(pref);
  }
  add('en');
  return candidates;
}

export async function loadLocale(lang: string) {
  let lastError: Error | undefined;
  for (const candidate of resolveLocaleCandidates(lang)) {
    const res = await fetch(`/api/locale/${candidate}`, { cache: 'no-store' });
    if (res.ok) {
      messages = await res.json();
      locale = candidate;
      localeVersion++;
      return;
    }

    lastError = new Error(`locale ${candidate}`);
  }

  throw lastError ?? new Error('locale unavailable');
}

export function getLocale() {
  return locale;
}

export function t(key: string, params?: Record<string, string | number>): string {
  void localeVersion;
  const value = lookup(key);
  if (typeof value === 'string') return interpolate(value, params);
  return key;
}

export function resolveBrowserLocale() {
  return resolveLocaleCandidates()[0] || 'en';
}
