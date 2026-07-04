using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    internal static class StatisticsCombatPatchAccess
    {
        internal static readonly FieldInfo? VPlayerDictField =
            AccessTools.Field(typeof(IVroom), "_vPlayerDict");
    }

    [HarmonyPatch(typeof(DungeonRoom), "SetDungeonState")]
    public static class DungeonRoomSurvivalOutcomePatches
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, DungeonState state)
        {
            StatisticsPatchGuard.Run("DungeonRoom.SetDungeonState", () =>
            {
                if (state != DungeonState.Success && state != DungeonState.Failed)
                {
                    return;
                }

                if (StatisticsCombatPatchAccess.VPlayerDictField?.GetValue(__instance) is not VActorDict<int, VPlayer> players)
                {
                    return;
                }

                StatisticsTracker.OnSurvivalDungeonEnded([.. players.Values]);
            });
        }
    }

    [HarmonyPatch(typeof(DungeonRoom), nameof(DungeonRoom.OnActorEvent))]
    public static class DungeonRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(DungeonRoom.OnActorEvent), () =>
            {
                if (args is GameActorDeadEventArgs deadArgs)
                {
                    StatisticsTracker.HandleActorDeath(__instance, deadArgs);
                }
            });
        }
    }

    [HarmonyPatch(typeof(DeathMatchRoom), nameof(DeathMatchRoom.OnActorEvent))]
    public static class DeathMatchRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(DeathMatchRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(DeathMatchRoom.OnActorEvent), () =>
            {
                if (args is GameActorDeadEventArgs deadArgs)
                {
                    StatisticsTracker.HandleActorDeath(__instance, deadArgs);
                }
            });
        }
    }

    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnActorEvent))]
    public static class MaintenanceRoomActorDeathPatches
    {
        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance, VActorEventArgs args)
        {
            StatisticsPatchGuard.Run(nameof(MaintenanceRoom.OnActorEvent), () =>
            {
                if (args is GameActorDeadEventArgs deadArgs)
                {
                    StatisticsTracker.HandleActorDeath(__instance, deadArgs);
                }
            });
        }
    }

    [HarmonyPatch(typeof(VCreature), nameof(VCreature.OnDying))]
    public static class VCreatureDyingPatches
    {
        [HarmonyPostfix]
        public static void Postfix(VCreature __instance, ActorDyingSig sig)
        {
            StatisticsPatchGuard.Run(nameof(VCreature.OnDying), () =>
            {
                if (__instance is not VPlayer player || __instance.VRoom == null)
                {
                    return;
                }

                StatisticsTracker.OnPlayerDying(player, sig, __instance.VRoom);
            });
        }
    }

    [HarmonyPatch(typeof(PlayReportManager), nameof(PlayReportManager.SetDeathMatchSurvivor))]
    public static class DeathMatchSurvivorPatches
    {
        [HarmonyPostfix]
        public static void Postfix(ulong steamID)
        {
            StatisticsPatchGuard.Run(nameof(PlayReportManager.SetDeathMatchSurvivor), () =>
            {
                StatisticsTracker.OnDeathmatchSurvivor(steamID);
            });
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.Revive))]
    public static class VPlayerRevivePatches
    {
        [HarmonyPostfix]
        public static void Postfix(VPlayer __instance, bool __result)
        {
            StatisticsPatchGuard.Run(nameof(VPlayer.Revive), () =>
            {
                if (!__result)
                {
                    return;
                }

                StatisticsTracker.OnPlayerRevived(__instance.SteamID);
            });
        }
    }
}
