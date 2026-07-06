function createSettingsProfileMixin() {
  return {
    saveProfile: null,
    quickPresets: [],
    loadingSaveProfile: false,
    applyingProfile: false,
    savingQuickPreset: false,
    shareModalOpen: false,
    shareModalMode: 'export',
    shareModalText: '',
    shareModalName: '',
    selectedLoadPresetId: '',

    showSettingsProfilePage() {
      return this.route === 'settings' && this.settingsSubRoute !== 'customize' && this.status.isConnected;
    },

    showSettingsCustomizePage() {
      return this.route === 'settings' && this.settingsSubRoute === 'customize' && this.status.isConnected;
    },

    isProfileActive(preset) {
      if (!this.saveProfile?.profile) return false;
      const profile = this.saveProfile.profile;
      if (preset.mode === 'global') {
        return profile.mode === 'global';
      }
      return profile.mode === 'quick' && profile.presetId === preset.id;
    },

    presetCardClass(preset) {
      const classes = {
        'is-active': this.isProfileActive(preset),
      };
      if (preset.mode === 'global') classes['is-global'] = true;
      if (!preset.isBuiltin) classes['is-user'] = true;
      if (preset.isBuiltin) {
        const slug = String(preset.id || '').replace('builtin:', '');
        classes['is-builtin-' + slug] = true;
      }
      return classes;
    },

    userPresets() {
      return (this.quickPresets || []).filter((preset) => !preset.isBuiltin);
    },

    builtinPresets() {
      return (this.quickPresets || []).filter((preset) => preset.isBuiltin);
    },

    loadablePresets() {
      return this.quickPresets || [];
    },

    selectedPreset() {
      if (!this.selectedLoadPresetId) return null;
      return this.loadablePresets().find((p) => p.id === this.selectedLoadPresetId) ?? null;
    },

    selectedPresetIsBuiltin() {
      return !!this.selectedPreset()?.isBuiltin;
    },

    canOverwriteSelectedPreset() {
      return !!this.selectedLoadPresetId && !this.selectedPresetIsBuiltin();
    },

    canDeleteSelectedPreset() {
      return this.canOverwriteSelectedPreset();
    },

    async loadSaveProfileData(force) {
      if (!this.showSettingsProfilePage() && !this.showSettingsCustomizePage()) {
        this.saveProfile = null;
        this.quickPresets = [];
        return;
      }
      if (!this.status.isHost) return;

      const initial = this.saveProfile === null || force;
      if (initial) this.loadingSaveProfile = true;
      try {
        const [profile, presets] = await Promise.all([
          Api.getSaveProfile(),
          Api.getQuickPresets(),
        ]);
        this.saveProfile = profile;
        this.quickPresets = presets.presets || [];
      } finally {
        if (initial) this.loadingSaveProfile = false;
      }
    },

    async applySaveProfileMode(mode, presetId) {
      if (this.applyingProfile) return;
      this.applyingProfile = true;
      try {
        const result = await Api.updateSaveProfile({ mode, presetId: presetId || '' });
        this.saveProfile = result;
        this.settingsSave = null;
        this.showToast(result.message || this.t('dashboard.quick_profile_applied'));
        if (mode === 'custom') {
          location.hash = '#/settings/customize';
          this.parseRoute();
          await this.loadPageData(true);
        }
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_save'));
      } finally {
        this.applyingProfile = false;
      }
    },

    async goToCustomizeSettings() {
      if (this.saveProfile?.profile?.mode === 'custom') {
        location.hash = '#/settings/customize';
        this.parseRoute();
        await this.loadPageData(true);
        return;
      }
      await this.applySaveProfileMode('custom');
    },

    backToProfilePicker() {
      location.hash = '#/settings';
      this.parseRoute();
      this.loadPageData(true);
    },

    async enterCustomMode() {
      await this.applySaveProfileMode('custom');
    },

    async loadSelectedPreset() {
      if (!this.selectedLoadPresetId) return;
      await this.applySaveProfileMode('quick', this.selectedLoadPresetId);
      this.selectedLoadPresetId = '';
      this.settingsSave = null;
      await this.loadPageData(true);
    },

    async saveCurrentAsPreset(overwriteExisting) {
      if (overwriteExisting && this.selectedPresetIsBuiltin()) {
        this.showToast(this.t('dashboard.quick_preset_builtin_readonly'));
        return;
      }

      const name = window.prompt(
        overwriteExisting
          ? this.t('dashboard.quick_preset_overwrite_prompt')
          : this.t('dashboard.quick_preset_name_prompt')
      );
      if (!name || !name.trim()) return;

      this.savingQuickPreset = true;
      try {
        const presetId = overwriteExisting && this.selectedLoadPresetId
          ? this.selectedLoadPresetId
          : '';
        const saved = await Api.saveQuickPreset({
          name: name.trim(),
          presetId,
          overwriteExisting: !!overwriteExisting,
          fromCurrentSave: true,
        });
        await this.loadSaveProfileData(true);
        this.showToast(this.t('dashboard.quick_preset_saved', { name: saved.name || name.trim() }));
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_save'));
      } finally {
        this.savingQuickPreset = false;
      }
    },

    async deleteSelectedUserPreset() {
      if (!this.selectedLoadPresetId) return;
      const preset = this.selectedPreset();
      if (!preset) return;
      if (preset.isBuiltin) {
        this.showToast(this.t('dashboard.quick_preset_builtin_readonly'));
        return;
      }
      if (!window.confirm(this.t('dashboard.quick_preset_delete_confirm', { name: preset.name }))) return;

      try {
        await Api.deleteQuickPreset(preset.id);
        this.selectedLoadPresetId = '';
        await this.loadSaveProfileData(true);
        this.showToast(this.t('dashboard.quick_preset_deleted'));
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_save'));
      }
    },

    async openExportShareModal() {
      try {
        const data = await Api.exportQuickPreset('current');
        this.shareModalMode = 'export';
        this.shareModalText = data.shareString || '';
        this.shareModalName = data.name || '';
        this.shareModalOpen = true;
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_save'));
      }
    },

    openImportShareModal() {
      this.shareModalMode = 'import';
      this.shareModalText = '';
      this.shareModalName = '';
      this.shareModalOpen = true;
    },

    closeShareModal() {
      this.shareModalOpen = false;
    },

    async copyShareString() {
      if (!this.shareModalText) return;
      try {
        await navigator.clipboard.writeText(this.shareModalText);
        this.showToast(this.t('dashboard.quick_share_copied'));
      } catch {
        this.showToast(this.t('dashboard.quick_share_copy_failed'));
      }
    },

    async importShareString() {
      if (!this.shareModalText.trim()) return;
      try {
        const result = await Api.importQuickPreset({
          shareString: this.shareModalText.trim(),
          name: this.shareModalName.trim(),
          saveOnly: true,
          overwriteExisting: false,
        });
        if (!result.success) {
          this.showToast(result.message || this.t('dashboard.failed_save'));
          return;
        }
        this.shareModalOpen = false;
        await this.loadSaveProfileData(true);
        this.showToast(result.message || this.t('dashboard.quick_preset_imported'));
      } catch (e) {
        this.showToast(e.message || this.t('dashboard.failed_save'));
      }
    },

    customizeBannerText() {
      const profile = this.settingsSave?.profile || this.saveProfile?.profile;
      if (!profile || profile.mode !== 'quick') return '';
      return this.t('dashboard.quick_customize_banner', { label: profile.label || profile.presetId });
    },
  };
}

function applySettingsProfileMixin(target) {
  Object.defineProperties(target, Object.getOwnPropertyDescriptors(createSettingsProfileMixin()));
  return target;
}
