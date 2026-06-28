using System.Reflection;
using Bifrost.ConstEnum;
using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class MonsterTypeLookup
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubDatamanProperty =
        typeof(Hub).GetProperty("dataman", InstanceFlags);

    internal static bool TryGetMonster(int masterId, out MonsterInfo info)
    {
        info = null!;
        if (masterId <= 0 || Hub.s == null)
            return false;

        if (HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            return false;

        MonsterInfo? found = dataman.ExcelDataManager.GetMonsterInfo(masterId);
        if (found == null)
            return false;

        info = found;
        return true;
    }

    internal static MonsterType GetMonsterType(int masterId)
    {
        if (!TryGetMonster(masterId, out MonsterInfo info))
            return default;

        return info.MonsterType;
    }

    internal static string GetDisplayName(int masterId, MonsterInfo? info = null)
    {
        if (info == null && !TryGetMonster(masterId, out info))
            return masterId.ToString();

        return string.IsNullOrWhiteSpace(info!.Name) ? masterId.ToString() : info.Name;
    }
}
