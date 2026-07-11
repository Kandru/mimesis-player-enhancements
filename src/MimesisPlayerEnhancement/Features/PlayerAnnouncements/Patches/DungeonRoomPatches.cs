using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
    internal static class DungeonRoomOnAllMemberEnteredAnnouncementPatch
    {
        private const string Feature = "Announcements";

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

    [HarmonyPatch(typeof(DungeonRoom), "OnActorEnter")]
    internal static class DungeonRoomOnActorEnterAnnouncementPatch
    {
        private const string Feature = "Announcements";

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
