using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(RuntimeDungeon), "BuildDungeonInfo")]
    internal static class RuntimeDungeonBuiltPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardMinimapLayoutBuilder.RequestRebuild();
        }
    }
}
