using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class SpawnMultiplierResolver
{
    private const int VanillaPlayerBaseline = 4;

    internal static bool IsAutoScaleEnabled(SpawnCategory category) =>
        category switch
        {
            SpawnCategory.Mimic => ModConfig.AutoScaleMimicSpawnsByPlayerCount.Value,
            SpawnCategory.Boss => ModConfig.AutoScaleBossSpawnsByPlayerCount.Value,
            SpawnCategory.Jako => ModConfig.AutoScaleJakoSpawnsByPlayerCount.Value,
            SpawnCategory.Special => ModConfig.AutoScaleSpecialSpawnsByPlayerCount.Value,
            SpawnCategory.Trap => ModConfig.AutoScaleTrapSpawnsByPlayerCount.Value,
            _ => ModConfig.AutoScaleOtherSpawnsByPlayerCount.Value,
        };

    internal static float GetPlayerScale(SpawnCategory category, int playerCount)
    {
        if (!IsAutoScaleEnabled(category) || playerCount <= VanillaPlayerBaseline)
            return 1f;

        return playerCount / (float)VanillaPlayerBaseline;
    }

    internal static float GetPerCategoryMultiplier(SpawnCategory category) =>
        category switch
        {
            SpawnCategory.Mimic => ModConfig.MimicSpawnMultiplier.Value,
            SpawnCategory.Boss => ModConfig.BossSpawnMultiplier.Value,
            SpawnCategory.Jako => ModConfig.JakoSpawnMultiplier.Value,
            SpawnCategory.Special => ModConfig.SpecialSpawnMultiplier.Value,
            SpawnCategory.Trap => ModConfig.TrapSpawnMultiplier.Value,
            _ => ModConfig.OtherSpawnMultiplier.Value,
        };

    internal static float GetEffectiveMultiplier(SpawnCategory category, int playerCount) =>
        GetPerCategoryMultiplier(category) * GetPlayerScale(category, playerCount);

    internal static float GetEffectiveMultiplier(int masterId, int playerCount)
    {
        SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
        return GetEffectiveMultiplier(category, playerCount);
    }

    internal static int ScaleCount(int vanilla, float multiplier)
    {
        if (vanilla == 0)
            return 0;

        if (multiplier <= 0f)
            return 0;

        return System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
    }

    /// <summary>
    /// Scale a count field; when <paramref name="vanilla"/> is 0, use <paramref name="implicitWhenZero"/> as the base (e.g. StackCount defaults to 1).
    /// </summary>
    internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
    {
        int baseCount = vanilla > 0 ? vanilla : implicitWhenZero;
        return ScaleCount(baseCount, multiplier);
    }
}
