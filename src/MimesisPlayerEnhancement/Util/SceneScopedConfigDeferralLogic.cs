using System.Linq;

namespace MimesisPlayerEnhancement.Util
{
    internal static class SceneScopedConfigDeferralLogic
    {
        internal static readonly HashSet<string> GatedModules =
        [
            "LootMultiplicator",
            "SpawnScaling",
            "Economy",
            "DungeonTime",
            "DungeonRandomizer",
        ];

        /// <summary>
        /// Keys that apply immediately even while a gameplay scene is active.
        /// They do not defer the module snapshot, and SyncFromConfig still runs when they change.
        /// </summary>
        private static readonly HashSet<(string Module, string Key)> LiveApplyKeys =
            new(LiveApplyKeyComparer.Instance)
            {
                ("DungeonTime", "EnableRealtimeTramClock"),
                ("DungeonTime", "TimeMultiplier"),
            };

        internal static bool ShouldDefer(
            string moduleName,
            ModConfigChangeInfo change,
            bool isGameplaySceneActive,
            bool isMasterEnabled,
            string? masterToggleKey)
        {
            if (!GatedModules.Contains(moduleName))
            {
                return false;
            }

            if (IsMasterToggleDisabledChange(moduleName, change, isMasterEnabled, masterToggleKey))
            {
                return false;
            }

            // Live-apply keys must still SyncFromConfig mid-scene (e.g. tram clock invalidate).
            if (HasLiveApplyKeyChange(moduleName, change))
            {
                return false;
            }

            if (!isGameplaySceneActive)
            {
                return false;
            }

            return IsModuleAffected(moduleName, change);
        }

        internal static bool IsModuleAffected(string moduleName, ModConfigChangeInfo change)
        {
            if (change.IsFullReload)
            {
                return true;
            }

            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";
            return change.ChangedKeys.Any(keyChange =>
                string.Equals(keyChange.SectionId, sectionId, StringComparison.OrdinalIgnoreCase)
                && !IsLiveApplyKey(moduleName, keyChange.Key));
        }

        internal static bool HasLiveApplyKeyChange(string moduleName, ModConfigChangeInfo change)
        {
            if (change.IsFullReload || !GatedModules.Contains(moduleName))
            {
                return false;
            }

            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";
            return change.ChangedKeys.Any(keyChange =>
                string.Equals(keyChange.SectionId, sectionId, StringComparison.OrdinalIgnoreCase)
                && IsLiveApplyKey(moduleName, keyChange.Key));
        }

        internal static bool IsLiveApplyKey(string moduleName, string key) =>
            LiveApplyKeys.Contains((moduleName, key));

        internal static bool IsMasterToggleDisabledChange(
            string moduleName,
            ModConfigChangeInfo change,
            bool isMasterEnabled,
            string? masterToggleKey)
        {
            if (!GatedModules.Contains(moduleName) || isMasterEnabled)
            {
                return false;
            }

            if (change.IsFullReload)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(masterToggleKey))
            {
                return false;
            }

            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";
            return change.ChangedKeys.Any(keyChange =>
                string.Equals(keyChange.SectionId, sectionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(keyChange.Key, masterToggleKey, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Snapshot deferred module names for SyncFromConfig, then clear the set.
        /// Must run after pending snapshots are applied; commit must not clear deferred names first.
        /// </summary>
        internal static string[] TakeModulesForDeferredSync(HashSet<string> deferredModules)
        {
            if (deferredModules.Count == 0)
            {
                return [];
            }

            string[] modules = [.. deferredModules];
            deferredModules.Clear();
            return modules;
        }

        private sealed class LiveApplyKeyComparer : IEqualityComparer<(string Module, string Key)>
        {
            internal static readonly LiveApplyKeyComparer Instance = new();

            public bool Equals((string Module, string Key) x, (string Module, string Key) y) =>
                string.Equals(x.Module, y.Module, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string Module, string Key) obj) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Module),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
        }
    }
}
