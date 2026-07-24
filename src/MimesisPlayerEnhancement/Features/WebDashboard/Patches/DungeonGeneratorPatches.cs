using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/DunGen/DungeonGenerator.cs:L406-428
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
