using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class GroupSpawnBonusWaveApplier
    {
        private const string Feature = "SpawnScaling";

        internal static void OnMemberDead(GroupSpawnData groupData, bool groupWiped)
        {
            if (!ModConfig.EnableSpawnScaling.Value || !groupWiped)
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (!TryFindRoomForGroup(groupData, out _, out RoomSpawnScalingState? state))
            {
                return;
            }

            if (!state.TryConsumeBonusGroupWave(groupData.GroupID))
            {
                return;
            }

            ResetGroupForBonusWave(groupData);

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Bonus group wave armed — groupId={groupData.GroupID}");
            }
        }

        private static void ResetGroupForBonusWave(GroupSpawnData groupData)
        {
            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;
            SpawnScalingFields.GroupSpawnCountBackingField.SetValue(groupData, 0);
            SpawnScalingFields.GroupDeathTimeBackingField.SetValue(groupData, 0L);
            SpawnScalingFields.LastGroupSpawnTimeBackingField.SetValue(
                groupData,
                now - groupData.SpawnWaitTime - 1);
        }

        private static bool TryFindRoomForGroup(
            GroupSpawnData groupData,
            out DungeonRoom room,
            out RoomSpawnScalingState state)
        {
            foreach (KeyValuePair<DungeonRoom, RoomSpawnScalingState> entry in RoomSpawnScalingRegistry.EnumerateAll())
            {
                if (SpawnScalingFields.GroupSpawnDatasField.GetValue(entry.Key) is not System.Collections.IDictionary groups)
                {
                    continue;
                }

                if (groups.Contains(groupData.GroupID))
                {
                    room = entry.Key;
                    state = entry.Value;
                    return true;
                }
            }

            room = null!;
            state = null!;
            return false;
        }
    }
}
