namespace MimesisPlayerEnhancement.Util.Patches
{
    internal static class SceneScopedConfigPatches
    {
        private const string Feature = "Config";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SceneScopedConfigPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("EnterWaitingRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))),
                ("EnterMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))),
                ("EnterDungeon/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))),
                ("EnterDeathMatchRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.EnterDeathMatchRoom))),
                ("SetDungeonState/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "SetDungeonState")),
                ("OnDungeonFinished/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.OnDungeonFinished))),
            ]);
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
        internal static class VRoomManagerEnterWaitingRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Tram);
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))]
        internal static class VRoomManagerEnterMaintenenceRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Maintenance);
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))]
        internal static class VRoomManagerEnterDungeonScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDeathMatchRoom))]
        internal static class VRoomManagerEnterDeathMatchRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Deathmatch);
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
        internal static class DungeonRoomSetDungeonStateScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix(DungeonState state)
            {
                if (state != DungeonState.Success && state != DungeonState.Failed)
                {
                    return;
                }

                SceneScopedConfigGate.CommitPendingOnSceneEnd();
                SceneScopedConfigGate.FlushDeferredModuleSync(SceneScopedConfigGateSync.SyncModuleByName);
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnDungeonFinished))]
        internal static class VRoomManagerOnDungeonFinishedScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                SceneScopedConfigGate.EndScene();
            }
        }
    }

    internal static class SceneScopedConfigGateSync
    {
        internal static void SyncModuleByName(string moduleName)
        {
            foreach (IFeatureModule module in FeatureModules.All)
            {
                if (string.Equals(module.Name, moduleName, StringComparison.Ordinal))
                {
                    module.SyncFromConfig();
                    break;
                }
            }
        }
    }
}
