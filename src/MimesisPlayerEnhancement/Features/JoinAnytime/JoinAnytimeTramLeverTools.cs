using System.Collections.Generic;
using System.Collections.Immutable;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeTramLeverTools
    {
        internal static bool ShouldBlockDepartureLeverUse(IVroom room, ILevelObjectInfo levelObject, int toState)
        {
            if (room is not VWaitingRoom waitingRoom || waitingRoom.BackToMaintenance)
            {
                return false;
            }

            if (!JoinAnytimeRoomTools.ShouldBlockWaitingRoomStartGame())
            {
                return false;
            }

            if (!IsTramDepartureLever(levelObject))
            {
                return false;
            }

            return toState is (int)NewTramLeverState.Opening or (int)NewTramLeverState.Open;
        }

        internal static void TryResetTramDepartureLever(VWaitingRoom room, int actorId)
        {
            if (ReflectionHelper.GetFieldValue(room, "_levelObjects") is not Dictionary<int, ILevelObjectInfo> levelObjects)
            {
                return;
            }

            foreach (KeyValuePair<int, ILevelObjectInfo> entry in levelObjects)
            {
                if (entry.Value is not StateLevelObjectInfo stateInfo || !IsTramDepartureLever(entry.Value))
                {
                    continue;
                }

                if (stateInfo.CurrentState == (int)NewTramLeverState.Idle)
                {
                    continue;
                }

                _ = room.HandleLevelObject(
                    actorId,
                    entry.Key,
                    (int)NewTramLeverState.Idle,
                    occupy: false,
                    out _);

                ModLog.Debug("JoinAnytime", $"Reset tram lever object={entry.Key} to Idle after blocked departure");
                return;
            }
        }

        internal static bool TryGetLevelObject(IVroom room, int levelObjectId, out ILevelObjectInfo? levelObject)
        {
            levelObject = null;
            if (ReflectionHelper.GetFieldValue(room, "_levelObjects") is not Dictionary<int, ILevelObjectInfo> levelObjects)
            {
                return false;
            }

            return levelObjects.TryGetValue(levelObjectId, out levelObject);
        }

        private static bool IsTramDepartureLever(ILevelObjectInfo levelObject)
        {
            ImmutableList<IGameAction>? actions = levelObject.GetGameActions(
                (int)NewTramLeverState.Opening,
                (int)NewTramLeverState.Open);

            if (actions == null)
            {
                return false;
            }

            foreach (IGameAction action in actions)
            {
                if (action is GameActionMoveToNextRoom)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
