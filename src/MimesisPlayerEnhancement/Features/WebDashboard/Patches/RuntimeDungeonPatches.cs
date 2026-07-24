using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/DunGen/RuntimeDungeon.cs:L48-61
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
