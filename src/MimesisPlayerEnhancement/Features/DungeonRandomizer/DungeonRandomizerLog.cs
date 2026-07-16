namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRandomizerLog
    {
        private const string Feature = "DungeonRandomizer";
        private const string DungeonSectionId = "MimesisPlayerEnhancement_DungeonRandomizer";
        private const int MaxGenerationAttempts = 4;

        internal static void InfoDungeonPick(int vanillaResult, int picked, DungeonPickPoolMode mode, int poolSize)
        {
            if (picked == vanillaResult)
            {
                ModLog.Debug(Feature, $"Dungeon pick ({mode}): kept vanilla result {vanillaResult} (pool={poolSize})");
                return;
            }

            ModLog.Info(
                Feature,
                $"Dungeon pick ({mode}): {vanillaResult} -> {picked} (pool={poolSize})");
        }

        internal static void InfoMapVariantChanged(int dungeonId, int vanillaMapId, int mapId)
        {
            if (vanillaMapId == mapId)
            {
                ModLog.Debug(Feature, $"Map variant: dungeon {dungeonId} unchanged at {vanillaMapId}");
                return;
            }

            ModLog.Info(Feature, $"Map variant — dungeon {dungeonId}: {vanillaMapId} -> {mapId}");
        }

        internal static void InfoSeedFlavorApplied(
            DungeonSeedFlavor flavor,
            string flowId,
            int vanillaSeed,
            int curatedSeed,
            int poolSize,
            int skipped)
        {
            string flavorLabel = ModL10n.GetConfigSelectOptionLabel(
                DungeonSectionId,
                "DungeonSeedFlavor",
                DungeonSeedFlavorUtil.ToConfigValue(flavor))
                ?? flavor.ToString();

            string skippedSuffix = skipped > 0 ? $", skipped {skipped}" : string.Empty;
            ModLog.Info(
                Feature,
                $"Seed flavor '{flavorLabel}' applied — flow={flowId}, seed {vanillaSeed} -> {curatedSeed} (pool {poolSize}{skippedSuffix})");
        }

        internal static void DebugSeedCandidateSkipped(
            string expectedFlowId,
            int poolIndex,
            int candidateSeed,
            string derivedFlowId)
        {
            ModLog.Debug(
                Feature,
                $"Seed candidate skipped — pool[{poolIndex}]={candidateSeed} re-derives flow '{derivedFlowId}' (want '{expectedFlowId}')");
        }

        internal static void DebugSeedCurationReused(int vanillaSeed, int curatedSeed) =>
            ModLog.Debug(Feature, $"Seed curation reused — seed {vanillaSeed} -> {curatedSeed} (cached)");

        internal static void WarnGenerationFailed(int attempt, int failedSeed, int nextSeed) =>
            ModLog.WarnRed(
                Feature,
                $"Dungeon generation failed — attempt {attempt}/{MaxGenerationAttempts}, seed {failedSeed} -> {nextSeed} (curated seed abandoned)");
    }
}
