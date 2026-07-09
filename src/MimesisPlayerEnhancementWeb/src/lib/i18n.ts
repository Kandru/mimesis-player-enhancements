let locale = 'en';
let messages: Record<string, unknown> = {};

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

export async function loadLocale(lang: string) {
  const normalized = (lang || 'en').toLowerCase().startsWith('de') ? 'de' : 'en';
  const res = await fetch(`/api/locale/${normalized}`, { cache: 'no-store' });
  if (!res.ok) throw new Error(`locale ${normalized}`);
  messages = await res.json();
  locale = normalized;
}

export function getLocale() {
  return locale;
}

export function t(key: string, params?: Record<string, string | number>): string {
  const value = lookup(key);
  if (typeof value === 'string') return interpolate(value, params);
  return key;
}

export function resolveBrowserLocale() {
  const lang = navigator.language || 'en';
  return lang.toLowerCase().startsWith('de') ? 'de' : 'en';
}
