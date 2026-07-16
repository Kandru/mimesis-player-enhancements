namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRandomizerLog
    {
        private const string Feature = "DungeonRandomizer";

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

        internal static void InfoLayoutFlowChanged(int dungeonId, string vanillaFlow, string flowName)
        {
            if (string.Equals(vanillaFlow, flowName, System.StringComparison.Ordinal))
            {
                ModLog.Debug(Feature, $"Layout flow: dungeon {dungeonId} unchanged at '{vanillaFlow}'");
                return;
            }

            ModLog.Info(Feature, $"Layout flow — dungeon {dungeonId}: '{vanillaFlow}' -> '{flowName}'");
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
            int poolSize)
        {
            ModLog.Info(
                Feature,
                $"Dungeon seed flavor {flavor} — flow '{flowId}', pool={poolSize}, {vanillaSeed} -> {curatedSeed}");
        }
    }
}
