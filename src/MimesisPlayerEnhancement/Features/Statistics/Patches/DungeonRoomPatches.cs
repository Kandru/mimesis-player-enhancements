using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    internal static class DungeonRoomStatisticsAccess
    {
        internal static readonly FieldInfo? VPlayerDictField =
            AccessTools.Field(typeof(IVroom), "_vPlayerDict");
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L607-697
    [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
    public static class DungeonRoomSurvivalOutcomePatches
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, DungeonState state)
        {
            StatisticsPatchGuard.Run("DungeonRoom.SetDungeonState", () =>
            {
                if (state == DungeonState.OnPlaying)
                {
                    StatisticsTracker.OnDungeonStarted();
                    return;
                }

                if (state != DungeonState.Success && state != DungeonState.Failed)
                {
                    return;
                }

                if (DungeonRoomStatisticsAccess.VPlayerDictField?.GetValue(__instance) is not VActorDict<int, VPlayer> players)
                {
                    return;
                }

                StatisticsTracker.OnSurvivalDungeonEnded(players.Values);
            });
        }
    }

    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L768-778
    [HarmonyPatch(typeof(DungeonRoom), nameof(DungeonRoom.OnActorEvent))]
    public static class DungeonRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(DungeonRoom.OnActorEvent), () =>
            {
                if (args is not GameActorDeadEventArgs deadArgs)
                {
                    return;
                }

                StatisticsDeathHandler.HandleActorDeath(__instance, deadArgs);
            });
        }
    }
}
