using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L2896-2902
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

    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L1273-1303
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

    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L1635-1667
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
