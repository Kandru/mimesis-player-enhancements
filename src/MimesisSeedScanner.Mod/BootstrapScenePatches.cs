using HarmonyLib;

namespace MimesisSeedScanner.Mod
{
    [HarmonyPatch(typeof(GamePlayScene), "Start")]
    internal static class SuppressGamePlaySceneStartDuringBootstrapPatch
    {
        private static bool Prefix() => !FlowCatalog.BootstrapInProgress;
    }

    [HarmonyPatch(typeof(testDunGenEntry), "Start")]
    internal static class SuppressTestDunGenEntryStartDuringBootstrapPatch
    {
        private static bool Prefix() => !FlowCatalog.BootstrapInProgress;
    }
}
