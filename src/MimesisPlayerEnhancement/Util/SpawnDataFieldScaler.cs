using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Shared reflection-based scaling of the common spawn-data count fields
    /// (<c>StackCount</c>, <c>MaxRespawnCount</c>) used by LootMultiplicator and SpawnScaling.
    /// Callers own feature-specific gating and log formatting.
    /// </summary>
    internal static class SpawnDataFieldScaler
    {
        internal static bool TryScaleStackCount(object spawnData, float multiplier, out int before, out int after)
        {
            before = 0;
            after = 0;

            FieldInfo? field = ReflectionFieldCache.GetField(spawnData, "StackCount");
            if (field == null)
            {
                return false;
            }

            before = (int)(field.GetValue(spawnData) ?? 0);
            after = ScalingMath.ScaleCountWithImplicitBase(before, multiplier, implicitWhenZero: 1);
            field.SetValue(spawnData, after);
            return true;
        }

        internal static bool TryScaleMaxRespawnCount(object spawnData, float multiplier, out int before, out int after)
        {
            before = 0;
            after = 0;

            FieldInfo? field = ReflectionFieldCache.GetField(spawnData, "MaxRespawnCount");
            if (field == null)
            {
                return false;
            }

            before = (int)(field.GetValue(spawnData) ?? 0);
            if (before <= 0)
            {
                return false;
            }

            after = ScalingMath.ScaleCount(before, multiplier);
            field.SetValue(spawnData, after);
            return true;
        }
    }
}
