using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonVariantResolver
{
    internal static string? ResolveLayoutFlow(DungeonMasterInfo info, string vanillaFlow)
    {
        if (!ModConfig.RandomizeLayoutFlow.Value)
            return null;

        if (!DungeonDataAccess.TryPickUniformLayoutFlow(info, out string flowName))
        {
            DungeonRandomizerLog.Debug($"Layout flow: no candidates for dungeon {info.ID}; keeping '{vanillaFlow}'");
            return null;
        }

        DungeonRandomizerLog.Debug($"Layout flow: dungeon {info.ID} '{vanillaFlow}' -> '{flowName}'");
        return flowName;
    }

    internal static int? ResolveMapId(DungeonMasterInfo info, int vanillaMapId)
    {
        if (!ModConfig.RandomizeMapVariant.Value)
            return null;

        if (!DungeonDataAccess.TryPickUniformMapId(info, out int mapId))
        {
            DungeonRandomizerLog.Debug($"Map variant: no MapIDs for dungeon {info.ID}; keeping {vanillaMapId}");
            return null;
        }

        DungeonRandomizerLog.Debug($"Map variant: dungeon {info.ID} {vanillaMapId} -> {mapId}");
        return mapId;
    }
}
