using System.Linq;

namespace MimesisPlayerEnhancement.Util
{
    internal enum SceneScopeKind
    {
        None,
        Maintenance,
        Tram,
        Dungeon,
        Deathmatch,
    }

    internal static class SceneScopedConfigGate
    {
        private const string Feature = "Config";

        private static readonly HashSet<string> GatedModules =
        [
            "LootMultiplicator",
            "SpawnScaling",
            "Economy",
            "DungeonTime",
            "DungeonRandomizer",
        ];

        private static readonly Dictionary<string, Func<bool>> EnableReaders = new(StringComparer.Ordinal)
        {
            ["LootMultiplicator"] = () => ModConfig.EnableLootMultiplicator.Value,
            ["SpawnScaling"] = () => ModConfig.EnableSpawnScaling.Value,
            ["Economy"] = () => ModConfig.EnableEconomy.Value,
            ["DungeonTime"] = () => ModConfig.EnableDungeonTime.Value,
            ["DungeonRandomizer"] = () => ModConfig.EnableDungeonRandomizer.Value,
        };

        private static SceneScopeKind _activeKind = SceneScopeKind.None;
        private static LootMultiplicatorSceneConfig _activeLoot;
        private static SpawnScalingSceneConfig _activeSpawn;
        private static EconomySceneConfig _activeEconomy;
        private static DungeonTimeSceneConfig _activeDungeonTime;
        private static DungeonRandomizerSceneConfig _activeDungeonRandomizer;
        private static LootMultiplicatorSceneConfig _pendingLoot;
        private static SpawnScalingSceneConfig _pendingSpawn;
        private static EconomySceneConfig _pendingEconomy;
        private static DungeonTimeSceneConfig _pendingDungeonTime;
        private static DungeonRandomizerSceneConfig _pendingDungeonRandomizer;
        private static readonly HashSet<string> DeferredModules = new(StringComparer.Ordinal);
        private static bool _deferInfoLogged;

        private static Action<string>? _deferredModuleSyncAction;

        internal static SceneScopeKind ActiveKind => _activeKind;

        internal static LootMultiplicatorSceneConfig Loot => _activeLoot;

        internal static SpawnScalingSceneConfig Spawn => _activeSpawn;

        internal static EconomySceneConfig Economy => _activeEconomy;

        internal static DungeonTimeSceneConfig DungeonTime => _activeDungeonTime;

        internal static DungeonRandomizerSceneConfig DungeonRandomizer => _activeDungeonRandomizer;

        internal static void SetDeferredModuleSyncAction(Action<string> syncAction)
        {
            _deferredModuleSyncAction = syncAction;
        }

        internal static void Initialize()
        {
            CaptureAllActiveFromModConfig();
            CaptureAllPendingFromModConfig();
            _activeKind = SceneScopeKind.None;
            DeferredModules.Clear();
            _deferInfoLogged = false;
        }

        internal static void OnConfigChangePrepared(ModConfigChangeInfo change)
        {
            CaptureAllPendingFromModConfig();

            if (!IsGameplaySceneActive())
            {
                ApplyPendingToActive();
                DeferredModules.Clear();
                _deferInfoLogged = false;
                return;
            }

            foreach (string moduleName in GatedModules)
            {
                if (!IsModuleAffected(moduleName, change))
                {
                    continue;
                }

                if (IsMasterToggleDisabledChange(moduleName, change))
                {
                    ApplyActiveSnapshotForModule(moduleName);
                    continue;
                }

                _ = DeferredModules.Add(moduleName);
            }

            if (DeferredModules.Count > 0 && !_deferInfoLogged)
            {
                _deferInfoLogged = true;
                ModLog.Info(Feature, $"Config change deferred until scene end — scene={_activeKind}, modules={string.Join(", ", DeferredModules)}");
            }
        }

        internal static bool ShouldDeferModuleSync(string moduleName, ModConfigChangeInfo change)
        {
            if (!GatedModules.Contains(moduleName))
            {
                return false;
            }

            if (IsMasterToggleDisabledChange(moduleName, change))
            {
                return false;
            }

            if (!IsGameplaySceneActive())
            {
                return false;
            }

            return IsModuleAffected(moduleName, change);
        }

        internal static void TransitionToScene(SceneScopeKind kind)
        {
            if (_activeKind != SceneScopeKind.None && _activeKind != kind)
            {
                EndScene();
            }

            _activeKind = kind;
            CaptureAllActiveFromModConfig();
            CaptureAllPendingFromModConfig();
            DeferredModules.Clear();
            _deferInfoLogged = false;
        }

        internal static void EndScene()
        {
            if (_activeKind == SceneScopeKind.None)
            {
                return;
            }

            CommitPendingOnSceneEnd();
            FlushDeferredModuleSyncIfConfigured();
            _activeKind = SceneScopeKind.None;
        }

        internal static void CommitPendingOnSceneEnd()
        {
            ApplyPendingToActive();
            DeferredModules.Clear();
            _deferInfoLogged = false;
        }

        internal static void FlushDeferredModuleSync(Action<string> syncAction)
        {
            if (DeferredModules.Count == 0)
            {
                return;
            }

            string[] modules = [.. DeferredModules];
            DeferredModules.Clear();
            _deferInfoLogged = false;

            foreach (string moduleName in modules)
            {
                syncAction(moduleName);
            }
        }

        private static void FlushDeferredModuleSyncIfConfigured()
        {
            if (_deferredModuleSyncAction == null)
            {
                DeferredModules.Clear();
                _deferInfoLogged = false;
                return;
            }

            FlushDeferredModuleSync(_deferredModuleSyncAction);
        }

        internal static bool IsModuleSyncDeferred(string moduleName)
        {
            return DeferredModules.Contains(moduleName);
        }

        internal static bool IsGameplaySceneActive()
        {
            return _activeKind != SceneScopeKind.None;
        }

        private static void ApplyPendingToActive()
        {
            _activeLoot = _pendingLoot;
            _activeSpawn = _pendingSpawn;
            _activeEconomy = _pendingEconomy;
            _activeDungeonTime = _pendingDungeonTime;
            _activeDungeonRandomizer = _pendingDungeonRandomizer;
            LootItemFilter.ReloadFromSceneSnapshot();
        }

        private static void CaptureAllActiveFromModConfig()
        {
            _activeLoot = LootMultiplicatorSceneConfig.CaptureFromModConfig();
            _activeSpawn = SpawnScalingSceneConfig.CaptureFromModConfig();
            _activeEconomy = EconomySceneConfig.CaptureFromModConfig();
            _activeDungeonTime = DungeonTimeSceneConfig.CaptureFromModConfig();
            _activeDungeonRandomizer = DungeonRandomizerSceneConfig.CaptureFromModConfig();
            LootItemFilter.ReloadFromSceneSnapshot();
        }

        private static void CaptureAllPendingFromModConfig()
        {
            _pendingLoot = LootMultiplicatorSceneConfig.CaptureFromModConfig();
            _pendingSpawn = SpawnScalingSceneConfig.CaptureFromModConfig();
            _pendingEconomy = EconomySceneConfig.CaptureFromModConfig();
            _pendingDungeonTime = DungeonTimeSceneConfig.CaptureFromModConfig();
            _pendingDungeonRandomizer = DungeonRandomizerSceneConfig.CaptureFromModConfig();
        }

        private static void ApplyActiveSnapshotForModule(string moduleName)
        {
            switch (moduleName)
            {
                case "LootMultiplicator":
                    _activeLoot = _pendingLoot;
                    LootItemFilter.ReloadFromSceneSnapshot();
                    break;
                case "SpawnScaling":
                    _activeSpawn = _pendingSpawn;
                    break;
                case "Economy":
                    _activeEconomy = _pendingEconomy;
                    break;
                case "DungeonTime":
                    _activeDungeonTime = _pendingDungeonTime;
                    break;
                case "DungeonRandomizer":
                    _activeDungeonRandomizer = _pendingDungeonRandomizer;
                    break;
            }

            _ = DeferredModules.Remove(moduleName);
        }

        private static bool IsModuleAffected(string moduleName, ModConfigChangeInfo change)
        {
            if (change.IsFullReload)
            {
                return true;
            }

            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";
            return change.AffectsSection(sectionId);
        }

        private static bool IsMasterToggleDisabledChange(string moduleName, ModConfigChangeInfo change)
        {
            if (!EnableReaders.TryGetValue(moduleName, out Func<bool>? readEnable) || readEnable())
            {
                return false;
            }

            if (change.IsFullReload)
            {
                return true;
            }

            string sectionId = $"MimesisPlayerEnhancement_{moduleName}";
            if (!ModConfigRegistry.TryGetFeatureToggleKey(sectionId, out string toggleKey))
            {
                return false;
            }

            return change.ChangedKeys.Any(keyChange =>
                string.Equals(keyChange.SectionId, sectionId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(keyChange.Key, toggleKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
