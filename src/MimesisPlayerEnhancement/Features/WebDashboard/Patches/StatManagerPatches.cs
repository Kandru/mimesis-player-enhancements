using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/StatManager.cs:L663-672
    [HarmonyPatch(typeof(StatManager), nameof(StatManager.AddMutableStat))]
    internal static class BlockContaAddMutableStatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(StatManager __instance, MutableStatType type, long delta, ref bool __result)
        {
            if (!GodModeContaFreeze.ShouldBlock(__instance, type, __instance.GetCurrentConta() + delta))
            {
                return true;
            }

            __result = true;
            return false;
        }
    }

    // game@0.3.1 Assembly-CSharp/StatManager.cs:L655-661
    [HarmonyPatch(typeof(StatManager), nameof(StatManager.SetMutableStat))]
    internal static class BlockContaSetMutableStatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(StatManager __instance, MutableStatType type, long value) =>
            !GodModeContaFreeze.ShouldBlock(__instance, type, value);
    }

    internal static class GodModeContaFreeze
    {
        private static readonly FieldInfo? SelfField = AccessTools.Field(typeof(StatManager), "_self");

        internal static bool IsContaIncrease(long current, long proposed) =>
            proposed > current;

        internal static bool ShouldBlock(StatManager instance, MutableStatType type, long proposedConta)
        {
            if (type != MutableStatType.Conta
                || !WebDashboardHostCheatsRuntime.HasActiveGodMode
                || SelfField == null)
            {
                return false;
            }

            if (SelfField.GetValue(instance) is not VCreature creature
                || !WebDashboardHostCheatsRuntime.ShouldFreezeConta(creature))
            {
                return false;
            }

            return IsContaIncrease(instance.GetCurrentConta(), proposedConta);
        }
    }
}
