using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/StatManager.cs:L663-672
    [HarmonyPatch(typeof(StatManager), nameof(StatManager.AddMutableStat))]
    internal static class BlockContaAddMutableStatPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(StatManager __instance, MutableStatType type, ref bool __result)
        {
            if (!GodModeContaFreeze.ShouldBlock(__instance, type))
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
        private static bool Prefix(StatManager __instance, MutableStatType type) =>
            !GodModeContaFreeze.ShouldBlock(__instance, type);
    }

    internal static class GodModeContaFreeze
    {
        private static readonly FieldInfo? SelfField = AccessTools.Field(typeof(StatManager), "_self");

        internal static bool ShouldBlock(StatManager instance, MutableStatType type)
        {
            if (type != MutableStatType.Conta
                || !WebDashboardHostCheatsRuntime.HasActiveGodMode
                || SelfField == null)
            {
                return false;
            }

            return SelfField.GetValue(instance) is VCreature creature
                && WebDashboardHostCheatsRuntime.ShouldFreezeConta(creature);
        }
    }
}
