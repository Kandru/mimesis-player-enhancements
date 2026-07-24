using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerTuning.Patches
{
    // game@0.3.1 Assembly-CSharp/MappedStats.cs:L12-49
    [HarmonyPatch(typeof(MappedStats), nameof(MappedStats.LoadBaseStats))]
    internal static class MappedStatsLoadBaseStatsPatch
    {
        private const string Feature = "PlayerTuning";

        [HarmonyPostfix]
        public static void Postfix(MappedStats __instance, ActorType type, bool __result)
        {
            try
            {
                if (!PlayerTuningApplier.ShouldApply || !__result || type != ActorType.Player)
                {
                    return;
                }

                PlayerTuningApplier.ApplyMappedPlayerStats(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"LoadBaseStats postfix failed — {ex.Message}");
            }
        }
    }
}
