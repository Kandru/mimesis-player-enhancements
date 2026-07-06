(function () {
  let messages = {};
  let locale = 'en';
  const supportedLocales = ['en', 'de'];

  function normalizeLocale(lang) {
    const normalized = String(lang || 'en').split('-')[0].toLowerCase();
    return supportedLocales.includes(normalized) ? normalized : 'en';
  }

  function resolveBrowserLocale() {
    const candidates = Array.isArray(navigator.languages) && navigator.languages.length
      ? navigator.languages
      : [navigator.language || 'en'];
    for (const candidate of candidates) {
      const normalized = normalizeLocale(candidate);
      if (supportedLocales.includes(normalized)) {
        return normalized;
      }
    }
    return 'en';
  }

  function lookup(key) {
    const parts = String(key || '').split('.');
    let current = messages;
    for (const part of parts) {
      if (!current || typeof current !== 'object' || !(part in current)) {
        return null;
      }
      current = current[part];
    }
    return typeof current === 'string' ? current : null;
  }

  function interpolate(template, params) {
    if (!params) return template;
    return template.replace(/\{(\w+)\}/g, (_, name) => {
      if (params[name] == null) return '';
      return String(params[name]);
    });
  }

  function t(key, params) {
    const template = lookup(key);
    if (!template) return key;
    return interpolate(template, params);
  }

  function bumpStore() {
    if (window.Alpine && typeof window.Alpine.store === 'function') {
      const store = window.Alpine.store('i18n');
      if (store) {
        store.locale = locale;
        store.version = (store.version || 0) + 1;
      }
    }
  }

  async function loadLocale(lang) {
    const normalized = normalizeLocale(lang);
    try {
      const res = await fetch('/api/locale/' + encodeURIComponent(normalized));
      if (res.ok) {
        messages = await res.json();
        locale = normalized;
      } else {
        const fallback = await fetch('/api/locale/en');
        messages = fallback.ok ? await fallback.json() : {};
        locale = 'en';
      }
    } catch {
      messages = {};
      locale = 'en';
    }
    document.documentElement.lang = locale;
    bumpStore();
    return locale;
  }

  function getLocale() {
    return locale;
  }

  window.DashboardI18n = { t, loadLocale, getLocale, resolveBrowserLocale };

  document.addEventListener('alpine:init', () => {
    if (!window.Alpine) {
      return;
    }
    window.Alpine.store('i18n', { version: 0, locale: 'en' });
    window.Alpine.magic('t', () => (key, params) => {
      window.Alpine.store('i18n').version;
      return t(key, params);
    });
  });
})();
