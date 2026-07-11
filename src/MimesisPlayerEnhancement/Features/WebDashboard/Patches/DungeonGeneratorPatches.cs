using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(DungeonGenerator), "ChangeStatus")]
    internal static class DungeonGenerationCompletePatch
    {
        [HarmonyPostfix]
        private static void Postfix(GenerationStatus status)
        {
            if (status == GenerationStatus.Complete)
            {
                WebDashboardMinimapLayoutBuilder.RequestRebuild();
            }
        }
    }
}
