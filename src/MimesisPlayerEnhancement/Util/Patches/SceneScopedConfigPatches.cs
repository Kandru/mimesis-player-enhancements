namespace MimesisPlayerEnhancement.Util.Patches
{
    internal static class SceneScopedConfigPatches
    {
        private const string Feature = "Config";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(SceneScopedConfigPatches)));
        }

        // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L372-409
        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
        internal static class VRoomManagerEnterWaitingRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Tram);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"EnterWaitingRoom scene transition failed — {ex.Message}");
                }
            }
        }

        // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L332-370
        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterMaintenenceRoom))]
        internal static class VRoomManagerEnterMaintenenceRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Maintenance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"EnterMaintenenceRoom scene transition failed — {ex.Message}");
                }
            }
        }

        // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L411-439
        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDungeon))]
        internal static class VRoomManagerEnterDungeonScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Dungeon);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"EnterDungeon scene transition failed — {ex.Message}");
                }
            }
        }

        // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L441-467
        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterDeathMatchRoom))]
        internal static class VRoomManagerEnterDeathMatchRoomScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    SceneScopedConfigGate.TransitionToScene(SceneScopeKind.Deathmatch);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"EnterDeathMatchRoom scene transition failed — {ex.Message}");
                }
            }
        }

        // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L607-697
        [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
        internal static class DungeonRoomSetDungeonStateScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix(DungeonState state)
            {
                try
                {
                    if (state != DungeonState.Success && state != DungeonState.Failed)
                    {
                        return;
                    }

                    SceneScopedConfigGate.CommitPendingOnSceneEnd();
                    SceneScopedConfigGate.FlushDeferredModuleSync(FeatureModules.SyncModuleByName);
                    SceneScopedConfigGate.InvokeDungeonRunEndCleanup();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SetDungeonState scene commit failed — {ex.Message}");
                }
            }
        }

        // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L583-600
        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnDungeonFinished))]
        internal static class VRoomManagerOnDungeonFinishedScenePatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                try
                {
                    SceneScopedConfigGate.EndScene();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnDungeonFinished scene end failed — {ex.Message}");
                }
            }
        }
    }
}
