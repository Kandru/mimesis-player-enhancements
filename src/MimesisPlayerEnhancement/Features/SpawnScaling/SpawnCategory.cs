namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
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
            return info.IsMimic()
                ? SpawnCategory.Mimic
                : IsTrap(info)
                ? SpawnCategory.Trap
                : info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Boss)
                ? SpawnCategory.Boss
                : info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Jako)
                ? SpawnCategory.Jako
                : info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Special)
                ? SpawnCategory.Special
                : info.MonsterType.Equals(Bifrost.ConstEnum.MonsterType.Mimic) ? SpawnCategory.Mimic : SpawnCategory.Other;
        }

        internal static SpawnCategory GetCategory(int masterId)
        {
            return !MonsterTypeLookup.TryGetMonster(masterId, out MonsterInfo info) ? SpawnCategory.Other : GetCategory(info);
        }

        internal static string Format(SpawnCategory category)
        {
            return category.ToString();
        }

        private static bool IsTrap(MonsterInfo info)
        {
            return ContainsTrapHint(info.Name)
            || ContainsTrapHint(info.PuppetName)
            || ContainsTrapHint(info.BTName);
        }

        private static bool ContainsTrapHint(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
            && value.IndexOf("trap", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
