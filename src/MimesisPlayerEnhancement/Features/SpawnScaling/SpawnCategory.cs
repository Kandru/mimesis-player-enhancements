using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal enum SpawnCategory
{
    Mimic,
    Boss,
    Jako,
    Special,
    Trap,
    Other,
}

internal static class SpawnCategoryLookup
{
    internal static SpawnCategory GetCategory(MonsterInfo info)
    {
        if (info.IsMimic())
            return SpawnCategory.Mimic;

        if (IsTrap(info))
            return SpawnCategory.Trap;

        if (info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Boss))
            return SpawnCategory.Boss;
        if (info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Jako))
            return SpawnCategory.Jako;
        if (info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Special))
            return SpawnCategory.Special;
        if (info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Mimic))
            return SpawnCategory.Mimic;

        return SpawnCategory.Other;
    }

    internal static SpawnCategory GetCategory(int masterId)
    {
        if (!MonsterTypeLookup.TryGetMonster(masterId, out MonsterInfo info))
            return SpawnCategory.Other;

        return GetCategory(info);
    }

    internal static string Format(SpawnCategory category) =>
        category.ToString();

    private static bool IsTrap(MonsterInfo info) =>
        ContainsTrapHint(info.Name)
        || ContainsTrapHint(info.PuppetName)
        || ContainsTrapHint(info.BTName);

    private static bool ContainsTrapHint(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.IndexOf("trap", System.StringComparison.OrdinalIgnoreCase) >= 0;
}
