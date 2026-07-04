using System;
using Mimic.Actors;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    public static class PlayerAnnouncementPatches
    {
        private const string Feature = "Announcements";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(PlayerAnnouncementPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
                ("OnPlayerDeath/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))),
                ("OnActorEnter/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnActorEnter")),
            ]);
        }

        [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
        public static class DungeonRoomOnAllMemberEnteredAnnouncementPatch
        {
            [HarmonyPostfix]
            public static void Postfix(DungeonRoom __instance)
            {
                try
                {
                    PlayerAnnouncements.OnAllMembersEnteredDungeon(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnAllMemberEntered announcement failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))]
        public static class GameMainDeathAnnouncementPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ProtoActor actor)
            {
                try
                {
                    MapRunStatsTracker.OnLocalPlayerDeath(actor);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Death announcement failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(DungeonRoom), "OnActorEnter")]
        public static class DungeonRoomOnActorEnterAnnouncementPatch
        {
            [HarmonyPostfix]
            public static void Postfix(VActor actor)
            {
                try
                {
                    if (actor is not VMonster monster)
                    {
                        return;
                    }

                    if (!monster.ActorType.Equals(ActorType.Monster))
                    {
                        return;
                    }

                    BossSpawnAnnouncer.RecordSpawn(monster.MasterID);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnActorEnter announcement failed — {ex.Message}");
                }
            }
        }
    }
}
