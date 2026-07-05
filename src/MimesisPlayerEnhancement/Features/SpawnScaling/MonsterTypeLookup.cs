using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class MonsterTypeLookup
    {
        internal static bool TryGetMonster(int masterId, out MonsterInfo info)
        {
            info = null!;
            if (masterId <= 0)
            {
                return false;
            }

            MonsterInfo? found = HubGameDataAccess.Excel?.GetMonsterInfo(masterId);
            if (found == null)
            {
                return false;
            }

            info = found;
            return true;
        }

        internal static string GetDisplayName(int masterId, MonsterInfo? info = null)
        {
            return info == null && !TryGetMonster(masterId, out info)
                ? masterId.ToString()
                : string.IsNullOrWhiteSpace(info.Name) ? masterId.ToString() : info.Name;
        }
    }
}
