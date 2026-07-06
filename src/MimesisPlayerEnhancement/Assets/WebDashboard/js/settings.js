function settingsHaystack(entry, sectionTitle) {
  return [
    entry.key,
    entry.title,
    entry.description,
    entry.value,
    entry.type,
    entry.defaultValue,
    entry.globalValue,
    sectionTitle,
  ].map((value) => String(value ?? '').toLowerCase());
}

function matchesSettingsQuery(entry, sectionTitle, query) {
  if (!query) return true;
  return settingsHaystack(entry, sectionTitle).some((value) => value.includes(query));
}

function createSettingsMixin() {
  return {
    settingsGlobal: null,
    settingsSave: null,
    settingsQuery: '',
    settingsSearchOpen: false,
    settingsSearchBlurTimer: null,
    savingSettingKey: '',
    resettingSectionId: '',
    lastLoadedSaveSlotId: -1,

    get activeSettings() {
      return this.route === 'global-settings' ? this.settingsGlobal : this.settingsSave;
    },

    get activeSettingsScope() {
      return this.route === 'global-settings' ? 'global' : 'save';
    },

    get settingsSearchSuggestions() {
      const query = this.settingsQuery.trim().toLowerCase();
      if (!query) return [];

      const results = [];
      for (const section of this.activeSettings?.sections ?? []) {
        if (section.featureToggle && matchesSettingsQuery(section.featureToggle, section.title, query)) {
          results.push(this.buildSettingsSuggestion(section, section.featureToggle, true));
        }
        if (!this.featureEnabled(section.id)) continue;
        for (const entry of section.entries ?? []) {
          if (matchesSettingsQuery(entry, section.title, query)) {
            results.push(this.buildSettingsSuggestion(section, entry));
          }
        }
      }

      return results
        .sort((a, b) => (a.priority - b.priority) || a.title.localeCompare(b.title))
        .slice(0, 8);
    },

    get filteredSections() {
      const query = this.settingsQuery.trim().toLowerCase();
      return (this.activeSettings?.sections ?? []).filter((section) => {
        if (this.sectionEntryCount(section.id) > 0) return true;
        if (!section.featureToggle) return false;
        if (!query) return true;
        return matchesSettingsQuery(section.featureToggle, section.title, query);
      });
    },

    showSettingsPage() {
      if (this.route === 'global-settings') return true;
      return this.route === 'settings' && this.status.isConnected;
    },

    canEditCurrentSettings() {
      if (this.route === 'global-settings') {
        return !this.status.isConnected || this.status.isHost;
      }
      return this.status.isConnected && this.status.isHost;
    },

    settingsPageHeading() {
      return this.route === 'global-settings'
        ? this.t('dashboard.settings_global_heading')
        : this.t('dashboard.settings_heading');
    },

    settingsPathLabel() {
      return this.activeSettingsScope === 'save'
        ? this.t('dashboard.settings_override_file_label')
        : this.t('dashboard.settings_config_file_label');
    },

    settingsPathValue() {
      return this.activeSettings?.configPath || '—';
    },

    settingsSearchSuggestionsId() {
      return this.activeSettingsScope + '-settings-suggestions';
    },

    settingsDataReady() {
      return this.activeSettings != null;
    },

    showOverrideBadge() {
      return this.activeSettingsScope === 'save';
    },

    settingEntryClass(entry) {
      const classes = {};
      if (entry.isHidden) classes['is-hidden-entry'] = true;
      if (this.activeSettingsScope === 'save' && !entry.isOverridden) {
        classes['is-global-default'] = true;
      }
      return classes;
    },

    settingHintVisible(entry) {
      return this.activeSettingsScope === 'save'
        ? this.settingDiffersFromGlobal(entry)
        : this.settingDiffersFromDefault(entry);
    },

    settingHintText(entry) {
      return this.activeSettingsScope === 'save'
        ? this.formatGlobalHint(entry)
        : this.formatDefaultHint(entry);
    },

    clearSettingsOnRouteChange(wasOnSettings, isOnSettings, prevRoute) {
      if (wasOnSettings && (!isOnSettings || prevRoute !== this.route)) {
        this.settingsQuery = '';
        this.settingsSearchOpen = false;
      }
    },

    handleSettingsOnDisconnect() {
      this.settingsSave = null;
      this.lastLoadedSaveSlotId = -1;
    },

    handleSettingsOnSnapshot() {
      if (this.route !== 'settings' || !this.status.isHost) return;
      const slotId = this.status.saveSlotId;
      if (this.lastLoadedSaveSlotId >= 0 && slotId !== this.lastLoadedSaveSlotId) {
        this.settingsSave = null;
        this.loadPageData(true);
      }
    },

    async loadSettingsInPageData(onGlobalSettings, onSaveSettings) {
      if (onGlobalSettings) {
        const initialLoad = this.settingsGlobal === null;
        if (initialLoad) this.loadingSettings = true;
        try {
          this.settingsGlobal = await Api.getGlobalSettings();
        } finally {
          if (initialLoad) this.loadingSettings = false;
        }
      } else if (this.route !== 'global-settings') {
        this.settingsGlobal = null;
      }

      if (onSaveSettings) {
        const initialLoad = this.settingsSave === null;
        if (initialLoad) this.loadingSettings = true;
        try {
          this.settingsSave = await Api.getSaveSettings();
          this.lastLoadedSaveSlotId = this.status.saveSlotId;
        } finally {
          if (initialLoad) this.loadingSettings = false;
        }
      } else if (this.route !== 'settings') {
        this.settingsSave = null;
        if (this.route !== 'global-settings') {
          this.settingsQuery = '';
          this.settingsSearchOpen = false;
        }
      }
    },

    settingsIntro() {
      if (this.route === 'global-settings') {
        return this.t('dashboard.settings_intro_global');
      }
      const slot = this.status.saveSlotId >= 0 ? this.status.saveSlotId : (this.settingsSave?.saveSlotId ?? '—');
      const intro = this.t('dashboard.settings_intro_save', { slot });
      const persistHint = this.t('dashboard.settings_save_persist_hint');
      return intro + ' ' + persistHint;
    },

    settingsSectionResetTitle() {
      return this.activeSettingsScope === 'save'
        ? this.t('dashboard.settings_reset_all_global_title')
        : this.t('dashboard.settings_reset_all_defaults_title');
    },

    settingsEntryResetTitle() {
      return this.activeSettingsScope === 'save'
        ? this.t('dashboard.settings_reset_global')
        : this.t('dashboard.settings_reset_default');
    },

    settingsKeysLabel(count) {
      return this.t('dashboard.settings_keys_count', { count });
    },

    formatDefaultHint(entry) {
      return this.t('dashboard.settings_default_hint', {
        value: formatSettingValue({ type: entry.type, value: entry.defaultValue }),
      });
    },

    formatGlobalHint(entry) {
      return this.t('dashboard.settings_global_hint', {
        value: formatSettingValue({ type: entry.type, value: entry.globalValue }),
      });
    },

    onSettingsSearchBlur() {
      if (this.settingsSearchBlurTimer) clearTimeout(this.settingsSearchBlurTimer);
      this.settingsSearchBlurTimer = setTimeout(() => {
        this.settingsSearchOpen = false;
      }, 150);
    },

    findSettingsSection(sectionId) {
      return this.activeSettings?.sections?.find((section) => section.id === sectionId) ?? null;
    },

    featureEnabled(sectionId) {
      const toggle = this.findSettingsSection(sectionId)?.featureToggle;
      if (!toggle) return true;
      return parseBool(toggle.value);
    },

    sectionEntries(sectionId) {
      const section = this.findSettingsSection(sectionId);
      if (!section?.entries?.length || !this.featureEnabled(sectionId)) return [];
      const query = this.settingsQuery.trim().toLowerCase();
      return section.entries.filter((entry) => matchesSettingsQuery(entry, section.title, query));
    },

    sectionEntryCount(sectionId) {
      return this.sectionEntries(sectionId).length;
    },

    buildSettingsSuggestion(section, entry, isFeatureToggle = false) {
      const title = String(entry.title || entry.key || '');
      const key = String(entry.key || '');
      const query = this.settingsQuery.trim().toLowerCase();
      const startsWith = title.toLowerCase().startsWith(query) || key.toLowerCase().startsWith(query);
      return {
        id: section.id + '/' + key,
        sectionId: section.id,
        sectionTitle: section.title,
        key,
        title,
        priority: startsWith ? 0 : 1,
        isFeatureToggle,
      };
    },

    selectSettingsSuggestion(item) {
      this.settingsQuery = item.key;
      this.settingsSearchOpen = false;
      this.$nextTick(() => {
        const domId = this.settingsDomId(
          item.sectionId,
          item.isFeatureToggle ? null : item.key
        );
        const el = document.getElementById(domId);
        if (!el) return;
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.classList.add('settings-entry-highlight');
        setTimeout(() => el.classList.remove('settings-entry-highlight'), 1600);
      });
    },

    settingsDomId(sectionId, entryKey = null) {
      const prefix = this.activeSettingsScope + '-' + sectionId;
      return entryKey
        ? 'setting-entry-' + prefix + '--' + entryKey
        : 'feature-toggle-' + prefix;
    },

    async toggleFeature(sectionId) {
      const toggle = this.findSettingsSection(sectionId)?.featureToggle;
      if (!toggle || this.isSavingSetting(sectionId, toggle)) return;
      await this.saveSetting(sectionId, toggle, parseBool(toggle.value) ? 'false' : 'true');
    },

    settingValuesEqual(entry, a, b) {
      if (entry.type === 'Single') {
        return formatFloatSettingValue(a) === formatFloatSettingValue(b);
      }
      if (entry.type === 'Boolean') {
        return parseBool(a) === parseBool(b);
      }
      return String(a ?? '') === String(b ?? '');
    },

    settingDiffersFromDefault(entry) {
      if (this.activeSettingsScope === 'save') {
        return this.settingDiffersFromGlobal(entry);
      }
      return !this.settingValuesEqual(entry, entry.value, entry.defaultValue);
    },

    settingDiffersFromGlobal(entry) {
      return !this.settingValuesEqual(entry, entry.value, entry.globalValue);
    },

    settingResetTarget(entry) {
      return this.activeSettingsScope === 'save'
        ? entry.globalValue
        : entry.defaultValue;
    },

    settingCanReset(entry) {
      return !this.settingValuesEqual(entry, entry.value, this.settingResetTarget(entry));
    },

    sectionResettableEntries(sectionId) {
      const section = this.findSettingsSection(sectionId);
      if (!section?.entries?.length) return [];
      return section.entries.filter((entry) => this.settingCanReset(entry));
    },

    sectionHasResettableEntries(sectionId) {
      return this.sectionResettableEntries(sectionId).length > 0;
    },

    isResettingSection(sectionId) {
      return this.resettingSectionId === sectionId;
    },

    async resetSetting(sectionId, entry) {
      if (!this.settingCanReset(entry) || this.isSavingSetting(sectionId, entry)) return;
      await this.saveSetting(sectionId, entry, this.settingResetTarget(entry));
    },

    async resetSectionSettings(sectionId) {
      if (this.isResettingSection(sectionId)) return;
      const entries = this.sectionResettableEntries(sectionId);
      if (!entries.length) return;

      this.resettingSectionId = sectionId;
      let resetCount = 0;
      try {
        for (const entry of entries) {
          if (!this.settingCanReset(entry)) continue;
          await this.saveSetting(sectionId, entry, this.settingResetTarget(entry), true);
          resetCount++;
        }
        if (resetCount > 0) {
          const target = this.activeSettingsScope === 'save'
            ? this.t('dashboard.reset_target_global')
            : this.t('dashboard.reset_target_defaults');
          this.showToast(this.t('dashboard.reset_settings_toast', { count: resetCount, target }));
        }
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_reset'));
        await this.loadPageData(true);
      } finally {
        if (this.resettingSectionId === sectionId) {
          this.resettingSectionId = '';
        }
      }
    },

    settingInputId(sectionId, entry) {
      return this.activeSettingsScope + '--' + sectionId + '--' + entry.key;
    },

    isSavingSetting(sectionId, entry) {
      if (!entry?.key) return false;
      return this.savingSettingKey === this.activeSettingsScope + '/' + sectionId + '/' + entry.key;
    },

    isSavingFeatureToggle(sectionId) {
      const toggle = this.findSettingsSection(sectionId)?.featureToggle;
      return toggle ? this.isSavingSetting(sectionId, toggle) : false;
    },

    settingDraftValue(entry) {
      if (entry.type === 'Boolean') {
        return parseBool(entry.value) ? 'true' : 'false';
      }
      return entry.value == null ? '' : String(entry.value);
    },

    async saveSetting(sectionId, entry, rawValue, quiet = false) {
      const scope = this.activeSettingsScope;
      const saveKey = scope + '/' + sectionId + '/' + entry.key;
      if (this.savingSettingKey === saveKey) return;

      const previousValue = entry.value;
      const wasOverridden = entry.isOverridden;
      const nextValue = String(rawValue);
      entry.value = nextValue;
      this.savingSettingKey = saveKey;

      try {
        const res = scope === 'global'
          ? await Api.updateGlobalSetting(sectionId, entry.key, nextValue)
          : await Api.updateSaveSetting(sectionId, entry.key, nextValue);
        if (res.value != null && res.value !== '') {
          entry.value = res.value;
        }
        if (scope === 'global') {
          this.settingsSave = null;
        } else {
          entry.isOverridden = res.isOverridden ?? this.settingDiffersFromGlobal(entry);
        }
        if (!quiet) {
          this.showToast(res.message || this.t('dashboard.saved'));
        }
      } catch (e) {
        entry.value = previousValue;
        entry.isOverridden = wasOverridden;
        if (!quiet) {
          this.showToast(e.message || this.t('dashboard.failed_save'));
        }
        await this.loadPageData(true);
        throw e;
      } finally {
        if (this.savingSettingKey === saveKey) {
          this.savingSettingKey = '';
        }
      }
    },

    onBooleanSettingChange(sectionId, entry, event) {
      this.saveSetting(sectionId, entry, event.target.value);
    },

    onTextSettingCommit(sectionId, entry, event) {
      let nextValue = clampSettingValue(entry, event.target.value);
      if (entry.type === 'Single' || entry.type === 'Int32') {
        event.target.value = nextValue;
      }
      if (this.settingValuesEqual(entry, nextValue, entry.value)) {
        return;
      }
      this.saveSetting(sectionId, entry, nextValue);
    },
  };
}

function applySettingsMixin(target) {
  Object.defineProperties(target, Object.getOwnPropertyDescriptors(createSettingsMixin()));
  return target;
}
