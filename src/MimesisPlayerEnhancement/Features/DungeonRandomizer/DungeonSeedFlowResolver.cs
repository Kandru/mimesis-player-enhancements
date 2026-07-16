namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonSeedFlowResolver
    {
        internal static bool TryResolveFlowId(int dungeonMasterId, int vanillaSeed, out string flowId)
        {
            flowId = string.Empty;
            ExcelDataManager? excel = DungeonDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            DungeonMasterInfo? info = excel.GetDungeonInfo(dungeonMasterId);
            if (info == null || info.DungenCandidates.Count == 0)
            {
                return false;
            }

            if (info.DungenCandidates.Count == 1)
            {
                foreach (KeyValuePair<string, int> candidate in info.DungenCandidates)
                {
                    flowId = candidate.Key;
                    return !string.IsNullOrEmpty(flowId);
                }
            }

            int maxRate = info.MaxDungenRate;
            if (maxRate <= 0)
            {
                return false;
            }

            int randVal = new GameMainBase.SyncRandom(vanillaSeed).Next(0, maxRate);
            flowId = info.GetRandomDungenName(randVal);
            return !string.IsNullOrEmpty(flowId);
        }
    }
}
