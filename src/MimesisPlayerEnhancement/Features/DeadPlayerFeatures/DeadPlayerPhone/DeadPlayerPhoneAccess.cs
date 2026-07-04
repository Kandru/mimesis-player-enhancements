using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneAccess
    {
        internal static bool TryGetLevelObject(IVroom room, int levelObjectId, out OccupiedLevelObjectInfo? phoneInfo)
        {
            phoneInfo = null;
            if (ReflectionHelper.GetFieldValue(room, "_levelObjects") is not Dictionary<int, ILevelObjectInfo> levelObjects)
            {
                return false;
            }

            if (!levelObjects.TryGetValue(levelObjectId, out ILevelObjectInfo? info)
                || info is not OccupiedLevelObjectInfo occupied)
            {
                return false;
            }

            if (occupied.DataOrigin?.LevelObjectType != LevelObjectClientType.Phone)
            {
                return false;
            }

            phoneInfo = occupied;
            return true;
        }

        internal static bool IsAnyAlivePlayerNearPhone(IVroom room, Vector3 phonePos, float maxDistanceMeters)
        {
            float maxSqr = maxDistanceMeters * maxDistanceMeters;
            if (ReflectionHelper.GetFieldValue(room, "_vPlayerDict") is not Dictionary<long, VPlayer> players)
            {
                return false;
            }

            foreach (VPlayer player in players.Values)
            {
                if (player == null || !player.IsAliveStatus())
                {
                    continue;
                }

                Vector3 delta = player.PositionVector - phonePos;
                delta.y = 0f;
                if (delta.sqrMagnitude <= maxSqr)
                {
                    return true;
                }
            }

            return false;
        }

        internal static long GetCurrentTimeMs()
        {
            return GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;
        }

        internal static PhoneLevelObject? TryFindClientPhone(int levelObjectId)
        {
            if (levelObjectId <= 0 || DeadPlayerPhoneGameAccess.TryGetMain() is not GamePlayScene scene)
            {
                return null;
            }

            foreach (LevelObject levelObject in scene.CollectLevelObjects())
            {
                if (levelObject is PhoneLevelObject phone
                    && DeadPlayerPhoneClient.GetLevelObjectId(phone) == levelObjectId)
                {
                    return phone;
                }
            }

            return null;
        }
    }
}
