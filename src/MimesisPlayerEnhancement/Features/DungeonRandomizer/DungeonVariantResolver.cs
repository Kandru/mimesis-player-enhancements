namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonVariantResolver
    {
        internal static int? ResolveMapId(DungeonMasterInfo info, int vanillaMapId)
        {
            DungeonRandomizerSceneConfig config = SceneScopedConfigGate.DungeonRandomizer;
            int? mapId = DungeonMapVariantPickLogic.Resolve(
                config.RandomizeMapVariant,
                info.ID,
                info.MapIDs,
                vanillaMapId,
                TryPickUniformMapId);

            if (mapId.HasValue)
            {
                DungeonRandomizerLog.InfoMapVariantChanged(info.ID, vanillaMapId, mapId.Value);
            }

            return mapId;
        }

        private static int? TryPickUniformMapId(IReadOnlyList<int> mapIds) =>
            DungeonDataAccess.TryPickUniformMapId(mapIds, out int mapId) ? mapId : null;
    }
}
