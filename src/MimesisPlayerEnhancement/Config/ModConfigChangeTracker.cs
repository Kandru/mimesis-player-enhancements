using System;
using System.Linq;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    public sealed class ModConfigKeyChange
    {
        public string SectionId { get; set; } = "";
        public string Key { get; set; } = "";
    }

    public sealed class ModConfigChangeInfo
    {
        public static ModConfigChangeInfo FullReload { get; } = new() { IsFullReload = true };

        public bool IsFullReload { get; set; }

        public IReadOnlyList<ModConfigKeyChange> ChangedKeys { get; set; } = [];

        public IEnumerable<string> ChangedSections =>
            ChangedKeys.Select(static change => change.SectionId).Distinct(StringComparer.OrdinalIgnoreCase);

        public bool AffectsSection(string sectionId)
        {
            if (IsFullReload)
            {
                return true;
            }

            return ChangedKeys.Any(change =>
                string.Equals(change.SectionId, sectionId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Tracks which config keys changed and batches notifications for multi-key updates.
    /// </summary>
    internal static class ModConfigChangeTracker
    {
        private static int _batchDepth;
        private static readonly HashSet<(string SectionId, string Key)> PendingChanges =
            new(ChangeKeyComparer.Instance);

        internal static event Action<ModConfigChangeInfo>? Changed;

        internal static void BeginBatch()
        {
            _batchDepth++;
        }

        internal static void EndBatch()
        {
            if (_batchDepth <= 0)
            {
                return;
            }

            _batchDepth--;
            if (_batchDepth > 0 || PendingChanges.Count == 0)
            {
                return;
            }

            FlushPending();
        }

        internal static void CancelBatch()
        {
            PendingChanges.Clear();
            _batchDepth = 0;
        }

        internal static void NotifyChange(string sectionId, string key)
        {
            if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (_batchDepth > 0)
            {
                PendingChanges.Add((sectionId, key));
                return;
            }

            Dispatch(BuildInfo([(sectionId, key)]));
        }

        internal static void NotifyEntryChanged(MelonPreferences_Entry entry)
        {
            NotifyChange(entry.Category.Identifier, entry.Identifier);
        }

        internal static void NotifyFullReload()
        {
            PendingChanges.Clear();
            _batchDepth = 0;
            Dispatch(ModConfigChangeInfo.FullReload);
        }

        private static void FlushPending()
        {
            if (PendingChanges.Count == 0)
            {
                return;
            }

            List<(string SectionId, string Key)> snapshot = [.. PendingChanges];
            PendingChanges.Clear();
            Dispatch(BuildInfo(snapshot));
        }

        private static ModConfigChangeInfo BuildInfo(
            IReadOnlyList<(string SectionId, string Key)> pendingChanges)
        {
            ModConfigKeyChange[] keys = pendingChanges
                .Select(static pair => new ModConfigKeyChange
                {
                    SectionId = pair.SectionId,
                    Key = pair.Key,
                })
                .ToArray();

            return new ModConfigChangeInfo { ChangedKeys = keys };
        }

        private static void Dispatch(ModConfigChangeInfo info)
        {
            ModConfigRegistry.NotifyRuntimeChange();
            Changed?.Invoke(info);
        }

        private sealed class ChangeKeyComparer : IEqualityComparer<(string SectionId, string Key)>
        {
            internal static ChangeKeyComparer Instance { get; } = new();

            public bool Equals((string SectionId, string Key) x, (string SectionId, string Key) y)
            {
                return string.Equals(x.SectionId, y.SectionId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string SectionId, string Key) obj)
            {
                return HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SectionId),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
            }
        }
    }
}
