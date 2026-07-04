namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonVariantResolver
    {
        private const string Feature = "DungeonRandomizer";

        internal static string? ResolveLayoutFlow(DungeonMasterInfo info, string vanillaFlow)
        {
            if (!ModConfig.RandomizeLayoutFlow.Value)
            {
                return null;
            }

            if (!DungeonDataAccess.TryPickUniformLayoutFlow(info, out string flowName))
            {
                ModLog.Debug(Feature, $"Layout flow: no candidates for dungeon {info.ID}; keeping '{vanillaFlow}'");
                return null;
            }

            DungeonRandomizerLog.InfoLayoutFlowChanged(info.ID, vanillaFlow, flowName);
            return flowName;
        }

        internal static int? ResolveMapId(DungeonMasterInfo info, int vanillaMapId)
        {
            if (!ModConfig.RandomizeMapVariant.Value)
            {
                return null;
            }

            if (!DungeonDataAccess.TryPickUniformMapId(info, out int mapId))
            {
                ModLog.Debug(Feature, $"Map variant: no MapIDs for dungeon {info.ID}; keeping {vanillaMapId}");
                return null;
            }

            DungeonRandomizerLog.InfoMapVariantChanged(info.ID, vanillaMapId, mapId);
            return mapId;
        }
    }
}
