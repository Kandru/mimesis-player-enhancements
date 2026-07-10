namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Per-save-slot Steam ID → display name sidecar (MMGameData{N}.mpe-names.sav).
    /// Independent of the statistics feature so offline players keep a readable name
    /// even when statistics are disabled or their document was saved without one.
    /// </summary>
    internal static class WebDashboardPlayerNameStore
    {
        private const string Feature = "WebDashboard";

        private static readonly object Gate = new();
        private static Dictionary<ulong, string> _names = [];
        private static int _loadedSlotId = -1;
        private static bool _dirty;

        internal static void LoadForSlot(int slotId)
        {
            if (slotId < 0)
            {
                return;
            }

            lock (Gate)
            {
                _names = [];
                _loadedSlotId = slotId;
                _dirty = false;

                string? path = SaveSidecarPaths.GetPlayerNamesPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    string? json = AtomicFileIO.ReadText(path, Feature);
                    if (!string.IsNullOrEmpty(json)
                        && ModJson.Deserialize<Dictionary<ulong, string>>(json) is Dictionary<ulong, string> loaded)
                    {
                        _names = loaded;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to load player name sidecar — {ex.Message}");
                }
            }
        }

        internal static string ResolveDisplayName(int slotId, ulong steamId, string? displayName)
        {
            if (steamId == 0)
            {
                return "";
            }

            if (!string.IsNullOrWhiteSpace(displayName) && displayName != steamId.ToString())
            {
                return displayName;
            }

            string? remembered = TryGetName(slotId, steamId);
            if (!string.IsNullOrWhiteSpace(remembered) && remembered != steamId.ToString())
            {
                return remembered;
            }

            return steamId.ToString();
        }

        internal static string? TryGetName(int slotId, ulong steamId)
        {
            if (slotId < 0 || steamId == 0 || slotId != _loadedSlotId)
            {
                return null;
            }

            lock (Gate)
            {
                return _names.TryGetValue(steamId, out string? name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : null;
            }
        }

        internal static void RememberName(int slotId, ulong steamId, string name)
        {
            if (slotId < 0 || steamId == 0 || string.IsNullOrWhiteSpace(name) || name == steamId.ToString())
            {
                return;
            }

            if (slotId != _loadedSlotId)
            {
                return;
            }

            lock (Gate)
            {
                if (_names.TryGetValue(steamId, out string? existing) && existing == name)
                {
                    return;
                }

                _names[steamId] = name;
                _dirty = true;
            }
        }

        internal static void ForgetName(int slotId, ulong steamId, bool waitForCompletion = false)
        {
            if (slotId < 0 || steamId == 0 || slotId != _loadedSlotId)
            {
                return;
            }

            lock (Gate)
            {
                if (!_names.Remove(steamId))
                {
                    return;
                }

                _dirty = true;
            }

            FlushToDisk(slotId, waitForCompletion);
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

                string? path = SaveSidecarPaths.GetPlayerNamesPath(slotId);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    BackgroundFileWriteQueue.EnqueueText(
                        path,
                        ModJson.Serialize(_names),
                        Feature,
                        waitForCompletion);
                    _dirty = false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Failed to save player name sidecar — {ex.Message}");
                }
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                _names = [];
                _loadedSlotId = -1;
                _dirty = false;
            }
        }
    }
}
