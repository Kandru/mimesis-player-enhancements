using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Per-save-slot Join Anytime lobby sidecar (MMGameData{N}.mpe-lobby.sav).
    /// </summary>
    internal static class JoinAnytimeLobbyStore
    {
        private const string Feature = "JoinAnytime";

        private static readonly JsonSerializerSettings WriteSettings = CreateWriteSettings();

        private static readonly object Gate = new();
        private static JoinAnytimeLobbySidecarData _data = new();
        private static int _loadedSlotId = -1;
        private static bool _loadedHadPublicPreference;
        private static bool _dirty;

        internal static void LoadForSlot(int slotId)
        {
            if (slotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                _data = new JoinAnytimeLobbySidecarData();
                _loadedSlotId = slotId;
                _loadedHadPublicPreference = false;
                _dirty = false;

                string? path = SaveSidecarPaths.GetLobbyPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    string? json = AtomicFileIO.ReadText(path, Feature);
                    if (!string.IsNullOrEmpty(json)
                        && ModJson.Deserialize<JoinAnytimeLobbySidecarData>(json) is JoinAnytimeLobbySidecarData loaded)
                    {
                        _data = NormalizeLoaded(loaded);
                        _loadedHadPublicPreference = _data.IsPublicLobby.HasValue;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to load lobby sidecar — {ex.Message}");
                }
            }
        }

        internal static bool HasPersistedPublicPreference(int slotId)
        {
            if (slotId < 0)
            {
                return false;
            }

            lock (Gate)
            {
                if (slotId == _loadedSlotId && _loadedHadPublicPreference)
                {
                    return true;
                }
            }

            return TryReadSidecarFromDisk(slotId)?.IsPublicLobby != null;
        }

        internal static bool TryReadSidecarForSlot(int slotId, out JoinAnytimeLobbySidecarData? data)
        {
            data = null;
            if (slotId < 0)
            {
                return false;
            }

            lock (Gate)
            {
                if (slotId == _loadedSlotId && HasPersistedContent(_data))
                {
                    data = CloneDocument(_data);
                    return true;
                }
            }

            JoinAnytimeLobbySidecarData? fromDisk = TryReadSidecarFromDisk(slotId);
            if (fromDisk == null || !HasPersistedContent(fromDisk))
            {
                return false;
            }

            data = fromDisk;
            lock (Gate)
            {
                if (slotId == _loadedSlotId)
                {
                    _data = NormalizeLoaded(fromDisk);
                    _loadedHadPublicPreference = _data.IsPublicLobby.HasValue;
                }
            }

            return true;
        }

        internal static void EnsureSlotBound(int slotId)
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
            JoinAnytimeLobbyController.OnSaveSlotSidecarLoaded(slotId);
        }

        internal static void CaptureFromController(int slotId)
        {
            if (!ModConfig.EnableJoinAnytime.Value
                || !JoinAnytimeLobbyController.TryExportLobbyState(out string? baseLobbyName, out bool isPublicLobby))
            {
                return;
            }

            RememberRuntimeState(slotId, baseLobbyName, isPublicLobby);
        }

        /// <summary>
        /// Reads the persisted base lobby name for a save slot (in-memory store or disk sidecar).
        /// Used by the save-slot picker where no active host session is loaded.
        /// </summary>
        internal static string? TryReadBaseLobbyNameForSlot(int slotId)
        {
            return TryReadSidecarForSlot(slotId, out JoinAnytimeLobbySidecarData? data)
                && !string.IsNullOrWhiteSpace(data?.BaseLobbyName)
                ? data.BaseLobbyName.Trim()
                : null;
        }

        internal static void RememberRuntimeState(int slotId, string? baseLobbyName = null, bool? isPublicLobby = null)
        {
            if (!ModConfig.EnableJoinAnytime.Value || slotId < 0)
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
                bool changed = false;

                if (baseLobbyName != null)
                {
                    string trimmed = baseLobbyName.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        return;
                    }

                    if (!string.Equals(_data.BaseLobbyName, trimmed, StringComparison.Ordinal))
                    {
                        _data.BaseLobbyName = trimmed;
                        changed = true;
                    }
                }

                if (isPublicLobby.HasValue && _data.IsPublicLobby != isPublicLobby)
                {
                    _data.IsPublicLobby = isPublicLobby;
                    changed = true;
                }

                if (changed)
                {
                    _dirty = true;
                }
            }
        }

        internal static void SetCustomEntry(int slotId, string key, string value)
        {
            if (!ModConfig.EnableJoinAnytime.Value || slotId < 0 || slotId != _loadedSlotId)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            lock (Gate)
            {
                _data.Custom ??= [];
                if (_data.Custom.TryGetValue(key, out string? existing) && existing == value)
                {
                    return;
                }

                _data.Custom[key] = value;
                _dirty = true;
            }
        }

        internal static bool TryGetCustomEntry(int slotId, string key, out string? value)
        {
            value = null;
            if (slotId < 0 || string.IsNullOrWhiteSpace(key) || slotId != _loadedSlotId)
            {
                return false;
            }

            lock (Gate)
            {
                return _data.Custom != null
                    && _data.Custom.TryGetValue(key, out value)
                    && !string.IsNullOrWhiteSpace(value);
            }
        }

        internal static void FlushToDisk(int slotId, bool waitForCompletion = false)
        {
            if (slotId < 0 || !MimesisSaveManager.IsValidSaveSlotId(slotId))
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
                if (!_dirty || !HasPersistedContent(_data))
                {
                    return;
                }

                string? path = SaveSidecarPaths.GetLobbyPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    JoinAnytimeLobbySidecarData snapshot = CloneDocument(_data);
                    BackgroundFileWriteQueue.EnqueueText(
                        path,
                        SerializeDocument(snapshot),
                        Feature,
                        waitForCompletion);
                    _dirty = false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to save lobby sidecar — {ex.Message}");
                }
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                _data = new JoinAnytimeLobbySidecarData();
                _loadedSlotId = -1;
                _loadedHadPublicPreference = false;
                _dirty = false;
            }
        }

        private static bool HasPersistedContent(JoinAnytimeLobbySidecarData data)
        {
            return !string.IsNullOrWhiteSpace(data.BaseLobbyName)
                || data.IsPublicLobby.HasValue
                || data.Custom is { Count: > 0 };
        }

        private static JoinAnytimeLobbySidecarData? TryReadSidecarFromDisk(int slotId)
        {
            string? path = SaveSidecarPaths.GetLobbyPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                string? json = AtomicFileIO.ReadText(path, Feature);
                if (string.IsNullOrEmpty(json)
                    || ModJson.Deserialize<JoinAnytimeLobbySidecarData>(json) is not JoinAnytimeLobbySidecarData loaded)
                {
                    return null;
                }

                return NormalizeLoaded(loaded);
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Failed to read lobby sidecar for slot {slotId} — {ex.Message}");
                return null;
            }
        }

        private static JoinAnytimeLobbySidecarData NormalizeLoaded(JoinAnytimeLobbySidecarData loaded)
        {
            if (loaded.Version <= 0)
            {
                loaded.Version = JoinAnytimeLobbySidecarData.CurrentVersion;
            }

            if (string.IsNullOrWhiteSpace(loaded.BaseLobbyName))
            {
                loaded.BaseLobbyName = null;
            }
            else
            {
                loaded.BaseLobbyName = loaded.BaseLobbyName.Trim();
            }

            if (loaded.Custom is { Count: 0 })
            {
                loaded.Custom = null;
            }

            return loaded;
        }

        private static JoinAnytimeLobbySidecarData CloneDocument(JoinAnytimeLobbySidecarData source)
        {
            JoinAnytimeLobbySidecarData clone = new()
            {
                Version = source.Version <= 0 ? JoinAnytimeLobbySidecarData.CurrentVersion : source.Version,
                BaseLobbyName = source.BaseLobbyName,
                IsPublicLobby = source.IsPublicLobby,
                Custom = source.Custom == null ? null : new Dictionary<string, string>(source.Custom),
            };

            if (clone.Custom is { Count: 0 })
            {
                clone.Custom = null;
            }

            return clone;
        }

        private static string SerializeDocument(JoinAnytimeLobbySidecarData data)
        {
            data.Version = JoinAnytimeLobbySidecarData.CurrentVersion;
            return JsonConvert.SerializeObject(data, WriteSettings);
        }

        private static JsonSerializerSettings CreateWriteSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new FieldCamelCaseContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };
        }

        private sealed class FieldCamelCaseContractResolver : DefaultContractResolver
        {
            public FieldCamelCaseContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy();
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, MemberSerialization.Fields);
            }
        }
    }
}
