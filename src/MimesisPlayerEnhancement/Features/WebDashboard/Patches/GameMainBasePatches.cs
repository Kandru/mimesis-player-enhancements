using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch]
    internal static class NetworkGradeSigPatch
    {
        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(NetworkGradeSig)]);

        [HarmonyPostfix]
        private static void Postfix(NetworkGradeSig sig)
        {
            if (sig?.grades == null)
            {
                return;
            }

            WebDashboardPatchHelpers.UpdateCachedGrades(sig.grades);
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))]
    internal static class PlayerDeathSnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardHostCheatsRuntime.SyncFromSession();
            WebDashboardSnapshotCache.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerRevive))]
    internal static class PlayerReviveSnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }
    }
}
