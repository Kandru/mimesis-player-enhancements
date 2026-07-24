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
            return change.AffectsSection(sectionId);
        }

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
    }
}
