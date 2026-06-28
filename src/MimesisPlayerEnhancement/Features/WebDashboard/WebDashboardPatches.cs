using System;
using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardPatches
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = HarmonyPatchHelper.ApplyPatchTypes(harmony, "WebDashboard", HarmonyPatchHelper.GetNestedPatchTypes(typeof(WebDashboardPatches)));
        }

        private static readonly ConcurrentDictionary<long, int> GradeByPlayerUid = new();

        internal static bool TryGetCachedGrade(long playerUid, out int grade)
        {
            return GradeByPlayerUid.TryGetValue(playerUid, out grade);
        }

        [HarmonyPatch]
        internal static class NetworkGradeSigPatch
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(NetworkGradeSig)])
                    ?? throw new InvalidOperationException("OnPacket(NetworkGradeSig) not found");
            }

            private static void Postfix(NetworkGradeSig sig)
            {
                if (sig?.grades == null)
                {
                    return;
                }

                foreach (System.Collections.Generic.KeyValuePair<long, ReluProtocol.Enum.NetworkGrade> pair in sig.grades)
                {
                    GradeByPlayerUid[pair.Key] = (int)pair.Value;
                }

                WebDashboardSnapshotCache.MarkDirty();
            }
        }
    }
}
