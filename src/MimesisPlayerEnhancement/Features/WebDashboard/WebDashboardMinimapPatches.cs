using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapPatches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                "WebDashboard",
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(WebDashboardMinimapPatches)));
        }

        [HarmonyPatch(typeof(DungeonGenerator), "ChangeStatus")]
        internal static class DungeonGenerationCompletePatch
        {
            private static void Postfix(GenerationStatus status)
            {
                if (status == GenerationStatus.Complete)
                {
                    WebDashboardMinimapLayoutBuilder.RequestRebuild();
                }
            }
        }

        [HarmonyPatch(typeof(RuntimeDungeon), "BuildDungeonInfo")]
        internal static class RuntimeDungeonBuiltPatch
        {
            private static void Postfix()
            {
                WebDashboardMinimapLayoutBuilder.RequestRebuild();
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "InitSpawn")]
        internal static class DungeonRoomInitSpawnPatch
        {
            private static void Postfix()
            {
                WebDashboardMinimapLayoutBuilder.RequestRebuild();
            }
        }
    }
}
