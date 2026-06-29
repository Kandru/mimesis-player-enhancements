using System;
using System.Collections.Immutable;

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    internal static class RoomEntryDelayFilter
    {
        internal static bool IsRoomEntryLevelObject(LevelObject? origin)
        {
            if (origin == null)
            {
                return false;
            }

            return origin.LevelObjectType is LevelObjectClientType.Teleporter
                or LevelObjectClientType.RandomTeleporter;
        }

        internal static bool IsRoomEntryTeleportTransition(ILevelObjectInfo info, int fromState, int toState)
        {
            if (!IsRoomEntryLevelObject(info.DataOrigin))
            {
                return false;
            }

            return HasTeleportAction(info.GetGameActions(fromState, toState));
        }

        internal static bool IsRoomEntryTeleportTransition(StaticLevelObject levelObject, int fromState, int toState)
        {
            if (!IsRoomEntryLevelObject(levelObject))
            {
                return false;
            }

            if (!levelObject.HasStateActionTransition(fromState, toState, out LevelObject.StateActionInfo? stateActionInfo)
                || stateActionInfo == null)
            {
                return false;
            }

            return ContainsTeleportAction(stateActionInfo.action);
        }

        private static bool HasTeleportAction(ImmutableList<IGameAction>? actions)
        {
            if (actions == null)
            {
                return false;
            }

            foreach (IGameAction action in actions)
            {
                if (action is GameActionTeleport or GameActionRandomTeleport)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsTeleportAction(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return false;
            }

            foreach (string part in action.Split(','))
            {
                string trimmed = part.Trim();
                if (trimmed.StartsWith("TELEPORT(", StringComparison.Ordinal)
                    || trimmed.StartsWith("RANDOM_TELEPORT(", StringComparison.Ordinal)
                    || trimmed.Equals("TELEPORT", StringComparison.Ordinal)
                    || trimmed.Equals("RANDOM_TELEPORT", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
