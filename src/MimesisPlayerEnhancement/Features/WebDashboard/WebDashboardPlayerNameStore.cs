using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;

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

        internal static string? TryGetName(int slotId, ulong steamId)
        {
            if (slotId < 0 || steamId == 0)
            {
                return null;
            }

            lock (Gate)
            {
                EnsureLoaded(slotId);
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

            lock (Gate)
            {
                EnsureLoaded(slotId);
                if (_names.TryGetValue(steamId, out string? existing) && existing == name)
                {
                    return;
                }

                _names[steamId] = name;
                Save(slotId);
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                _names = [];
                _loadedSlotId = -1;
            }
        }

        private static void EnsureLoaded(int slotId)
        {
            if (slotId == _loadedSlotId)
            {
                return;
            }

            _names = [];
            _loadedSlotId = slotId;

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

        private static void Save(int slotId)
        {
            string? path = SaveSidecarPaths.GetPlayerNamesPath(slotId);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                AtomicFileIO.WriteText(path, ModJson.Serialize(_names), Feature);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to save player name sidecar — {ex.Message}");
            }
        }
    }
}
