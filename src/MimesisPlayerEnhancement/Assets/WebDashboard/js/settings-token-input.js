function parseIdList(csv) {
  const seen = new Set();
  const ids = [];
  for (const part of String(csv ?? '').split(',')) {
    const id = part.trim();
    if (!id || seen.has(id)) continue;
    seen.add(id);
    ids.push(id);
  }
  return ids;
}

function serializeIdList(ids) {
  return ids.filter((id) => String(id ?? '').trim()).join(',');
}

function resolveCatalogMasterId(option) {
  if (!option) return '';
  if (option.masterId != null && option.masterId > 0) {
    return String(option.masterId);
  }
  const variants = option.variants || [];
  if (variants.length > 0) {
    const preferred = variants.find((v) => v.percent === 100) || variants[0];
    if (preferred?.masterId > 0) {
      return String(preferred.masterId);
    }
  }
  return String(option.id ?? '').trim();
}

function formatSellPriceRange(min, max) {
  if (min == null && max == null) return '';
  const lo = min ?? max;
  const hi = max ?? min;
  if (lo == null || hi == null) return '';
  if (lo === hi) return '$' + lo;
  return '$' + lo + '–$' + hi;
}

function resolveCatalogEntry(id, catalog) {
  const textId = String(id ?? '').trim();
  if (!textId || !catalog?.length) return null;

  for (const option of catalog) {
    if (String(option.id) === textId || String(option.masterId) === textId) {
      return option;
    }

    for (const variant of option.variants || []) {
      if (String(variant.masterId) === textId) {
        return {
          label: option.label,
          sellPriceMin: variant.sellPriceMin,
          sellPriceMax: variant.sellPriceMax,
        };
      }
    }
  }

  return null;
}

function resolveOptionSellPrice(option, masterId) {
  if (!option) return '';
  const variants = option.variants || [];
  if (variants.length > 0) {
    const variant = variants.find((v) => String(v.masterId) === String(masterId));
    if (variant) {
      return formatSellPriceRange(variant.sellPriceMin, variant.sellPriceMax);
    }
  }

  return formatSellPriceRange(option.sellPriceMin, option.sellPriceMax);
}

function createTokenInputMixin() {
  return {
    tokenInputState: {},
    tokenInputBlurTimers: {},

    tokenInputKey(sectionId, entry) {
      return this.activeSettingsScope + '/' + sectionId + '/' + entry.key;
    },

    getTokenCatalog(entry) {
      if (entry.inputKind === 'ItemIdList') return this.itemCatalog;
      if (entry.inputKind === 'DungeonIdList') return this.dungeonCatalog;
      return [];
    },

    resolveTokenLabel(id, catalog) {
      const textId = String(id ?? '').trim();
      if (!textId) return '';
      const option = (catalog || []).find((item) => String(item.id) === textId
        || String(item.masterId) === textId);
      if (option) {
        const label = String(option.label || '').trim();
        return label || textId;
      }

      for (const item of catalog || []) {
        const variant = (item.variants || []).find((v) => String(v.masterId) === textId);
        if (variant) {
          const label = String(item.label || '').trim();
          return label || textId;
        }
      }

      return textId;
    },

    resolveTokenPriceText(id, entry) {
      if (entry.inputKind !== 'ItemIdList') return '';
      const match = resolveCatalogEntry(id, this.getTokenCatalog(entry));
      return formatSellPriceRange(match?.sellPriceMin, match?.sellPriceMax);
    },

    tokenInputQueryFor(sectionId, entry) {
      const key = this.tokenInputKey(sectionId, entry);
      return this.tokenInputState[key]?.query ?? '';
    },

    tokenInputOpenFor(sectionId, entry) {
      const key = this.tokenInputKey(sectionId, entry);
      return this.tokenInputState[key]?.open ?? false;
    },

    tokenInputFocusedFor(sectionId, entry) {
      const key = this.tokenInputKey(sectionId, entry);
      return this.tokenInputState[key]?.focused ?? false;
    },

    setTokenInputState(sectionId, entry, patch) {
      const key = this.tokenInputKey(sectionId, entry);
      this.tokenInputState[key] = Object.assign({}, this.tokenInputState[key] || {}, patch);
    },

    tokenInputSuggestions(sectionId, entry) {
      const query = this.tokenInputQueryFor(sectionId, entry).trim().toLowerCase();
      if (!query) return [];

      const catalog = this.getTokenCatalog(entry);
      const currentIds = new Set(parseIdList(this.settingDraftValue(entry)));
      const results = [];

      for (const option of catalog) {
        const id = entry.inputKind === 'ItemIdList'
          ? resolveCatalogMasterId(option)
          : String(option.id ?? '').trim();
        if (!id || currentIds.has(id)) continue;

        const label = String(option.label || '').toLowerCase();
        const idText = id.toLowerCase();
        if (!label.includes(query) && !idText.includes(query)) continue;

        results.push({
          id,
          label: option.label || id,
          priceText: entry.inputKind === 'ItemIdList'
            ? resolveOptionSellPrice(option, id)
            : '',
          priority: idText.startsWith(query) || label.startsWith(query) ? 0 : 1,
        });
      }

      return results
        .sort((a, b) => (a.priority - b.priority) || a.label.localeCompare(b.label))
        .slice(0, 8);
    },

    tokenSuggestionsId(sectionId, entry) {
      return 'token-suggestions-' + this.settingInputId(sectionId, entry);
    },

    onTokenInputFocus(sectionId, entry) {
      this.setTokenInputState(sectionId, entry, { focused: true, open: true });
    },

    onTokenInputBlur(sectionId, entry) {
      const key = this.tokenInputKey(sectionId, entry);
      if (this.tokenInputBlurTimers[key]) {
        clearTimeout(this.tokenInputBlurTimers[key]);
      }
      this.tokenInputBlurTimers[key] = setTimeout(() => {
        this.setTokenInputState(sectionId, entry, { focused: false, open: false });
      }, 150);
    },

    onTokenInputInput(sectionId, entry, event) {
      this.setTokenInputState(sectionId, entry, {
        query: event.target.value,
        open: true,
        focused: true,
      });
    },

    normalizeTokenId(raw, entry) {
      const text = String(raw ?? '').trim();
      if (!text) return '';

      if (entry.inputKind === 'DungeonIdList') {
        return /^\d+$/.test(text) ? text : '';
      }

      if (entry.inputKind === 'ItemIdList') {
        if (/^\d+$/.test(text)) return text;
        const catalog = this.getTokenCatalog(entry);
        const match = catalog.find((option) => {
          const label = String(option.label || '').toLowerCase();
          const id = String(option.id || '').toLowerCase();
          const needle = text.toLowerCase();
          return label === needle || id === needle;
        });
        return match ? resolveCatalogMasterId(match) : '';
      }

      return text;
    },

    async addToken(sectionId, entry, rawId, options = {}) {
      const id = this.normalizeTokenId(rawId, entry);
      if (!id) return false;

      const ids = parseIdList(this.settingDraftValue(entry));
      if (ids.includes(id)) {
        if (!options.quiet) {
          this.setTokenInputState(sectionId, entry, { query: '', open: false });
        }
        return false;
      }

      ids.push(id);
      await this.commitTokenList(sectionId, entry, ids, options);
      this.setTokenInputState(sectionId, entry, { query: '', open: false });
      return true;
    },

    async removeToken(sectionId, entry, id) {
      const ids = parseIdList(this.settingDraftValue(entry)).filter((value) => value !== id);
      await this.commitTokenList(sectionId, entry, ids);
    },

    async commitTokenList(sectionId, entry, ids, options = {}) {
      const nextValue = serializeIdList(ids);
      if (this.settingValuesEqual(entry, nextValue, entry.value)) {
        return;
      }
      await this.saveSetting(sectionId, entry, nextValue, options.quiet !== false);
    },

    async onTokenSuggestionPick(sectionId, entry, suggestion) {
      await this.addToken(sectionId, entry, suggestion.id);
      this.setTokenInputState(sectionId, entry, { query: '', open: false, focused: true });
    },

    async onTokenInputKeydown(sectionId, entry, event) {
      const query = this.tokenInputQueryFor(sectionId, entry).trim();

      if (event.key === 'ArrowDown' && this.tokenInputSuggestions(sectionId, entry).length > 0) {
        event.preventDefault();
        this.setTokenInputState(sectionId, entry, { open: true });
        return;
      }

      if (event.key === 'Escape') {
        this.setTokenInputState(sectionId, entry, { open: false, query: '' });
        event.target.value = '';
        return;
      }

      if (event.key === 'Enter' || event.key === ',') {
        if (!query) return;
        event.preventDefault();
        await this.addToken(sectionId, entry, query);
        event.target.value = '';
        return;
      }

      if (event.key === 'Backspace' && !query) {
        const ids = parseIdList(this.settingDraftValue(entry));
        if (!ids.length) return;
        event.preventDefault();
        ids.pop();
        await this.commitTokenList(sectionId, entry, ids);
      }
    },

    async onTokenInputPaste(sectionId, entry, event) {
      const text = event.clipboardData?.getData('text') || '';
      if (!text.includes(',') && !/\s/.test(text)) return;

      event.preventDefault();
      const parts = text.split(/[,\s]+/).map((part) => part.trim()).filter(Boolean);
      if (!parts.length) return;

      const ids = parseIdList(this.settingDraftValue(entry));
      let changed = false;
      for (const part of parts) {
        const id = this.normalizeTokenId(part, entry);
        if (!id || ids.includes(id)) continue;
        ids.push(id);
        changed = true;
      }

      if (changed) {
        await this.commitTokenList(sectionId, entry, ids, { quiet: true });
      }
      this.setTokenInputState(sectionId, entry, { query: '', open: false });
    },

    isTokenInputKind(entry) {
      return entry.inputKind === 'ItemIdList' || entry.inputKind === 'DungeonIdList';
    },
  };
}

function applyTokenInputMixin(target) {
  Object.defineProperties(target, Object.getOwnPropertyDescriptors(createTokenInputMixin()));
  return target;
}
