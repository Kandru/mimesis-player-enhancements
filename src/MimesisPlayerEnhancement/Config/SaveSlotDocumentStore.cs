using MimesisPlayerEnhancement.Config.Models;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Per-save-slot unified mod document (MMGameData{N}.mpe-slot.sav):
    /// lobby, settings profile, sparse config overrides, and player roster.
    /// </summary>
    internal static class SaveSlotDocumentStore
    {
        private const string Feature = "SaveSlotDocument";

        private static readonly object Gate = new();
        private static SaveSlotDocument _document = new();
        private static int _loadedSlotId = -1;
        private static bool _dirty;

        internal static int LoadedSlotId => _loadedSlotId;

        internal static int LoadedPlayerCount
        {
            get
            {
                lock (Gate)
                {
                    return _document.Players?.Count ?? 0;
                }
            }
        }

        internal static string? GetDocumentPath(int slotId) => SaveSidecarPaths.GetSlotDocumentPath(slotId);

        internal static void LoadForSlot(int slotId)
        {
            if (slotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                _document = new SaveSlotDocument();
                _loadedSlotId = slotId;
                _dirty = false;

                string? path = GetDocumentPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    string? json = AtomicFileIO.ReadText(path, Feature);
                    if (!string.IsNullOrEmpty(json)
                        && ModJson.Deserialize<SaveSlotDocument>(json) is SaveSlotDocument loaded)
                    {
                        _document = NormalizeLoaded(loaded);
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to load slot document — {ex.Message}");
                }
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                _document = new SaveSlotDocument();
                _loadedSlotId = -1;
                _dirty = false;
            }
        }

        internal static void FlushToDisk(int slotId, bool waitForCompletion = false)
        {
            if (slotId < 0 || slotId != _loadedSlotId)
            {
                return;
            }

            lock (Gate)
            {
                if (!_dirty)
                {
                    return;
                }

                string? path = GetDocumentPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    SaveSlotDocument snapshot = CloneDocument(_document);
                    snapshot.Version = SaveSlotDocument.CurrentVersion;
                    if (!HasPersistedContent(snapshot))
                    {
                        BackgroundFileWriteQueue.EnqueueDelete(path, Feature, waitForCompletion);
                    }
                    else
                    {
                        BackgroundFileWriteQueue.EnqueueText(
                            path,
                            ModJson.Serialize(snapshot),
                            Feature,
                            waitForCompletion);
                    }

                    _dirty = false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to save slot document — {ex.Message}");
                }
            }
        }

        internal static bool TryReadFromDisk(int slotId, out SaveSlotDocument? document)
        {
            document = null;
            if (slotId < 0)
            {
                return false;
            }

            lock (Gate)
            {
                if (slotId == _loadedSlotId)
                {
                    document = CloneDocument(_document);
                    return HasPersistedContent(document);
                }
            }

            string? path = GetDocumentPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                string? json = AtomicFileIO.ReadText(path, Feature);
                if (string.IsNullOrEmpty(json)
                    || ModJson.Deserialize<SaveSlotDocument>(json) is not SaveSlotDocument loaded)
                {
                    return false;
                }

                document = NormalizeLoaded(loaded);
                return HasPersistedContent(document);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Failed to read slot document for slot {slotId} — {ex.Message}");
                return false;
            }
        }

        internal static SaveConfigProfileState GetSettingsProfile()
        {
            lock (Gate)
            {
                return CloneProfile(_document.SettingsProfile);
            }
        }

        internal static void SetSettingsProfile(SaveConfigProfileState profile)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                SaveConfigProfileState normalized = CloneProfile(profile);
                if (ProfilesEqual(_document.SettingsProfile, normalized))
                {
                    return;
                }

                _document.SettingsProfile = normalized;
                _dirty = true;
            }
        }

        internal static Dictionary<string, Dictionary<string, string>> GetConfigOverridesCopy()
        {
            lock (Gate)
            {
                return CloneOverrides(_document.ConfigOverrides);
            }
        }

        internal static void SetConfigOverrides(Dictionary<string, Dictionary<string, string>>? overrides)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                Dictionary<string, Dictionary<string, string>> normalized = CloneOverrides(overrides);
                if (OverridesEqual(_document.ConfigOverrides, normalized))
                {
                    return;
                }

                _document.ConfigOverrides = normalized.Count > 0 ? normalized : null;
                _dirty = true;
            }
        }

        internal static SparseTomlConfig.Document BuildRuntimeConfigDoc()
        {
            lock (Gate)
            {
                SparseTomlConfig.Document doc = new();
                SaveSlotConfigProfile.WriteProfileSection(doc, _document.SettingsProfile);
                MergeOverridesIntoDoc(doc, _document.ConfigOverrides);
                return doc;
            }
        }

        internal static void WriteRuntimeConfigDoc(SparseTomlConfig.Document doc, SaveConfigProfileState profile)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                _document.SettingsProfile = CloneProfile(profile);
                _document.ConfigOverrides = ExtractGameplayOverrides(doc);
                if (_document.ConfigOverrides is { Count: 0 })
                {
                    _document.ConfigOverrides = null;
                }

                _dirty = true;
            }
        }

        internal static void MarkDirty()
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                _dirty = true;
            }
        }

        internal static string? TryReadLobbyNameForSlot(int slotId)
        {
            if (!TryReadLobbySection(slotId, out SaveSlotLobbySection? lobby)
                || string.IsNullOrWhiteSpace(lobby?.BaseLobbyName))
            {
                return null;
            }

            return lobby.BaseLobbyName.Trim();
        }

        internal static bool HasPersistedPublicPreference(int slotId)
        {
            return TryReadLobbySection(slotId, out SaveSlotLobbySection? lobby)
                && lobby?.IsPublicLobby != null;
        }

        internal static void RememberLobbyRuntimeState(
            int slotId,
            string? baseLobbyName = null,
            bool? isPublicLobby = null)
        {
            if (slotId < 0)
            {
                return;
            }

            EnsureSlotBound(slotId);
            if (slotId != _loadedSlotId)
            {
                return;
            }

            lock (Gate)
            {
                _document.Lobby ??= new SaveSlotLobbySection();
                bool changed = false;

                if (baseLobbyName != null)
                {
                    string trimmed = baseLobbyName.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        return;
                    }

                    if (!string.Equals(_document.Lobby.BaseLobbyName, trimmed, StringComparison.Ordinal))
                    {
                        _document.Lobby.BaseLobbyName = trimmed;
                        changed = true;
                    }
                }

                if (isPublicLobby.HasValue && _document.Lobby.IsPublicLobby != isPublicLobby)
                {
                    _document.Lobby.IsPublicLobby = isPublicLobby;
                    changed = true;
                }

                if (changed)
                {
                    _dirty = true;
                }
            }
        }

        internal static void CaptureLobbyFromController(int slotId)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || !Features.JoinAnytime.JoinAnytimeLobbyController.TryExportLobbyState(
                    out string? baseLobbyName,
                    out bool isPublicLobby))
            {
                return;
            }

            RememberLobbyRuntimeState(slotId, baseLobbyName, isPublicLobby);
        }

        internal static string ResolveDisplayName(int slotId, ulong steamId, string? fallback)
        {
            if (steamId == 0)
            {
                return "";
            }

            if (TryGetName(slotId, steamId, out string? name) && IsUsableName(name, steamId))
            {
                return name!;
            }

            if (IsUsableName(fallback, steamId))
            {
                return fallback!;
            }

            return steamId.ToString();
        }

        internal static bool TryGetName(int slotId, ulong steamId, out string? name)
        {
            name = null;
            if (slotId < 0 || steamId == 0)
            {
                return false;
            }

            if (slotId == _loadedSlotId)
            {
                lock (Gate)
                {
                    return TryGetPlayerEntryLocked(steamId, out SaveSlotPlayerEntry? entry)
                        && IsUsableName(entry!.DisplayName, steamId)
                        && (name = entry.DisplayName) != null;
                }
            }

            if (!TryReadFromDisk(slotId, out SaveSlotDocument? document) || document?.Players == null)
            {
                return false;
            }

            string key = steamId.ToString();
            if (!document.Players.TryGetValue(key, out SaveSlotPlayerEntry? storedEntry)
                || !IsUsableName(storedEntry.DisplayName, steamId))
            {
                return false;
            }

            name = storedEntry.DisplayName;
            return true;
        }

        internal static bool TryGetVoiceId(int slotId, ulong steamId, out string? voiceId)
        {
            voiceId = null;
            if (slotId < 0 || steamId == 0)
            {
                return false;
            }

            if (slotId == _loadedSlotId)
            {
                lock (Gate)
                {
                    if (!TryGetPlayerEntryLocked(steamId, out SaveSlotPlayerEntry? entry)
                        || string.IsNullOrWhiteSpace(entry!.VoiceId))
                    {
                        return false;
                    }

                    voiceId = entry.VoiceId;
                    return true;
                }
            }

            if (!TryReadFromDisk(slotId, out SaveSlotDocument? document) || document?.Players == null)
            {
                return false;
            }

            if (!document.Players.TryGetValue(steamId.ToString(), out SaveSlotPlayerEntry? storedEntry)
                || string.IsNullOrWhiteSpace(storedEntry.VoiceId))
            {
                return false;
            }

            voiceId = storedEntry.VoiceId;
            return true;
        }

        internal static bool UpsertPlayer(ulong steamId, string displayName, string? voiceId = null)
        {
            if (_loadedSlotId < 0 || steamId == 0 || !IsUsableName(displayName, steamId))
            {
                return false;
            }

            lock (Gate)
            {
                _document.Players ??= [];
                string key = steamId.ToString();
                if (!_document.Players.TryGetValue(key, out SaveSlotPlayerEntry? entry))
                {
                    entry = new SaveSlotPlayerEntry();
                    _document.Players[key] = entry;
                }

                bool changed = entry.DisplayName != displayName;
                entry.DisplayName = displayName;

                if (!string.IsNullOrWhiteSpace(voiceId) && entry.VoiceId != voiceId)
                {
                    entry.VoiceId = voiceId;
                    changed = true;
                }

                if (changed)
                {
                    _dirty = true;
                }

                return changed;
            }
        }

        internal static bool UpsertVoiceId(ulong steamId, string voiceId)
        {
            if (_loadedSlotId < 0 || steamId == 0 || string.IsNullOrWhiteSpace(voiceId))
            {
                return false;
            }

            lock (Gate)
            {
                _document.Players ??= [];
                string key = steamId.ToString();
                if (!_document.Players.TryGetValue(key, out SaveSlotPlayerEntry? entry))
                {
                    return false;
                }

                if (entry.VoiceId == voiceId)
                {
                    return false;
                }

                entry.VoiceId = voiceId;
                _dirty = true;
                return true;
            }
        }

        internal static bool RemovePlayer(ulong steamId)
        {
            if (_loadedSlotId < 0 || steamId == 0)
            {
                return false;
            }

            lock (Gate)
            {
                if (_document.Players == null
                    || !_document.Players.Remove(steamId.ToString()))
                {
                    return false;
                }

                if (_document.Players.Count == 0)
                {
                    _document.Players = null;
                }

                _dirty = true;
                return true;
            }
        }

        internal static bool TryGetPlayerEntry(ulong steamId, out SaveSlotPlayerEntry? entry)
        {
            entry = null;
            if (_loadedSlotId < 0 || steamId == 0)
            {
                return false;
            }

            lock (Gate)
            {
                return TryGetPlayerEntryLocked(steamId, out entry);
            }
        }

        internal static void ApplyVoiceMappingsToRuntime(Action<ulong, string> applyMapping)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                if (_document.Players == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, SaveSlotPlayerEntry> kvp in _document.Players)
                {
                    if (!ulong.TryParse(kvp.Key, out ulong steamId)
                        || steamId == 0
                        || string.IsNullOrWhiteSpace(kvp.Value.VoiceId))
                    {
                        continue;
                    }

                    applyMapping(steamId, kvp.Value.VoiceId);
                }
            }
        }

        internal static void SyncVoiceMappingsFromRuntime(IReadOnlyDictionary<ulong, string> mappings)
        {
            if (_loadedSlotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                foreach (KeyValuePair<ulong, string> kvp in mappings)
                {
                    if (kvp.Key == 0 || string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        continue;
                    }

                    _document.Players ??= [];
                    string key = kvp.Key.ToString();
                    if (!_document.Players.TryGetValue(key, out SaveSlotPlayerEntry? entry))
                    {
                        continue;
                    }

                    if (entry.VoiceId != kvp.Value)
                    {
                        entry.VoiceId = kvp.Value;
                        _dirty = true;
                    }
                }
            }
        }

        internal static bool IsUsableName(string? name, ulong steamId)
        {
            return !string.IsNullOrWhiteSpace(name) && name != steamId.ToString();
        }

        private static void EnsureSlotBound(int slotId)
        {
            if (slotId < 0 || !MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            if (slotId == _loadedSlotId)
            {
                return;
            }

            LoadForSlot(slotId);
        }

        private static bool TryReadLobbySection(int slotId, out SaveSlotLobbySection? lobby)
        {
            lobby = null;
            if (slotId < 0)
            {
                return false;
            }

            if (slotId == _loadedSlotId)
            {
                lock (Gate)
                {
                    if (_document.Lobby == null)
                    {
                        return false;
                    }

                    lobby = CloneLobby(_document.Lobby);
                    return true;
                }
            }

            if (!TryReadFromDisk(slotId, out SaveSlotDocument? document) || document?.Lobby == null)
            {
                return false;
            }

            lobby = CloneLobby(document.Lobby);
            return true;
        }

        private static bool TryGetPlayerEntryLocked(ulong steamId, out SaveSlotPlayerEntry? entry)
        {
            entry = null;
            if (_document.Players == null)
            {
                return false;
            }

            return _document.Players.TryGetValue(steamId.ToString(), out entry);
        }

        private static Dictionary<string, Dictionary<string, string>>? ExtractGameplayOverrides(
            SparseTomlConfig.Document doc)
        {
            Dictionary<string, Dictionary<string, string>> overrides =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (string sectionId in doc.SectionOrder)
            {
                if (SaveSlotConfigProfile.IsProfileSection(sectionId))
                {
                    continue;
                }

                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) || keys.Count == 0)
                {
                    continue;
                }

                overrides[sectionId] = new Dictionary<string, string>(keys, StringComparer.OrdinalIgnoreCase);
            }

            return overrides.Count > 0 ? overrides : null;
        }

        private static void MergeOverridesIntoDoc(
            SparseTomlConfig.Document doc,
            Dictionary<string, Dictionary<string, string>>? overrides)
        {
            if (overrides == null)
            {
                return;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> section in overrides)
            {
                if (!doc.Sections.TryGetValue(section.Key, out Dictionary<string, string>? keys))
                {
                    doc.SectionOrder.Add(section.Key);
                    keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    doc.Sections[section.Key] = keys;
                }

                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    keys[pair.Key] = pair.Value;
                }
            }
        }

        private static bool HasPersistedContent(SaveSlotDocument document)
        {
            if (document.Lobby != null
                && (!string.IsNullOrWhiteSpace(document.Lobby.BaseLobbyName)
                    || document.Lobby.IsPublicLobby.HasValue))
            {
                return true;
            }

            if (document.SettingsProfile.Mode != SaveConfigProfileMode.Global)
            {
                return true;
            }

            if (document.ConfigOverrides is { Count: > 0 })
            {
                return true;
            }

            if (document.Players is { Count: > 0 })
            {
                return true;
            }

            return false;
        }

        private static SaveSlotDocument NormalizeLoaded(SaveSlotDocument loaded)
        {
            if (loaded.Version <= 0)
            {
                loaded.Version = SaveSlotDocument.CurrentVersion;
            }

            if (loaded.Lobby != null && string.IsNullOrWhiteSpace(loaded.Lobby.BaseLobbyName))
            {
                loaded.Lobby.BaseLobbyName = null;
            }
            else if (loaded.Lobby?.BaseLobbyName != null)
            {
                loaded.Lobby.BaseLobbyName = loaded.Lobby.BaseLobbyName.Trim();
            }

            if (loaded.Players is { Count: 0 })
            {
                loaded.Players = null;
            }

            if (loaded.ConfigOverrides is { Count: 0 })
            {
                loaded.ConfigOverrides = null;
            }

            if (loaded.SettingsProfile.Mode == SaveConfigProfileMode.Quick
                && string.IsNullOrWhiteSpace(loaded.SettingsProfile.PresetId))
            {
                loaded.SettingsProfile.Mode = SaveConfigProfileMode.Custom;
            }

            return loaded;
        }

        private static SaveSlotDocument CloneDocument(SaveSlotDocument source)
        {
            return new SaveSlotDocument
            {
                Version = source.Version <= 0 ? SaveSlotDocument.CurrentVersion : source.Version,
                Lobby = source.Lobby == null ? null : CloneLobby(source.Lobby),
                SettingsProfile = CloneProfile(source.SettingsProfile),
                ConfigOverrides = CloneOverrides(source.ConfigOverrides),
                Players = ClonePlayers(source.Players),
            };
        }

        private static SaveSlotLobbySection CloneLobby(SaveSlotLobbySection source)
        {
            return new SaveSlotLobbySection
            {
                BaseLobbyName = source.BaseLobbyName,
                IsPublicLobby = source.IsPublicLobby,
            };
        }

        private static SaveConfigProfileState CloneProfile(SaveConfigProfileState source)
        {
            return new SaveConfigProfileState
            {
                Mode = source.Mode,
                PresetId = source.PresetId ?? "",
                PresetRevision = source.PresetRevision,
            };
        }

        private static Dictionary<string, Dictionary<string, string>> CloneOverrides(
            Dictionary<string, Dictionary<string, string>>? source)
        {
            Dictionary<string, Dictionary<string, string>> clone =
                new(StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return clone;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> section in source)
            {
                clone[section.Key] = new Dictionary<string, string>(
                    section.Value,
                    StringComparer.OrdinalIgnoreCase);
            }

            return clone;
        }

        private static Dictionary<string, SaveSlotPlayerEntry>? ClonePlayers(
            Dictionary<string, SaveSlotPlayerEntry>? source)
        {
            if (source == null)
            {
                return null;
            }

            Dictionary<string, SaveSlotPlayerEntry> clone = [];
            foreach (KeyValuePair<string, SaveSlotPlayerEntry> kvp in source)
            {
                clone[kvp.Key] = new SaveSlotPlayerEntry
                {
                    DisplayName = kvp.Value.DisplayName,
                    VoiceId = kvp.Value.VoiceId,
                };
            }

            return clone.Count > 0 ? clone : null;
        }

        private static bool ProfilesEqual(SaveConfigProfileState a, SaveConfigProfileState b)
        {
            return a.Mode == b.Mode
                && a.PresetId == b.PresetId
                && a.PresetRevision == b.PresetRevision;
        }

        private static bool OverridesEqual(
            Dictionary<string, Dictionary<string, string>>? a,
            Dictionary<string, Dictionary<string, string>>? b)
        {
            if (a == null || a.Count == 0)
            {
                return b == null || b.Count == 0;
            }

            if (b == null || a.Count != b.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> section in a)
            {
                if (!b.TryGetValue(section.Key, out Dictionary<string, string>? bKeys)
                    || section.Value.Count != bKeys.Count)
                {
                    return false;
                }

                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    if (!bKeys.TryGetValue(pair.Key, out string? bValue) || bValue != pair.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
