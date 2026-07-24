namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonVariantResolver
    {
        internal static int? ResolveMapId(DungeonMasterInfo info, int vanillaMapId) =>
            ResolveMapId(SceneScopedConfigGate.DungeonRandomizer, info, vanillaMapId, TryPickUniformMapId);

        internal static int? ResolveMapId(
            DungeonRandomizerSceneConfig config,
            DungeonMasterInfo info,
            int vanillaMapId,
            Func<IReadOnlyList<int>, int?> pickMapId) =>
            ResolveMapId(config, info.ID, info.MapIDs, vanillaMapId, pickMapId);

        internal static int? ResolveMapId(
            DungeonRandomizerSceneConfig config,
            int dungeonId,
            IReadOnlyList<int> mapIds,
            int vanillaMapId,
            Func<IReadOnlyList<int>, int?> pickMapId,
            bool logResult = true)
        {
            int? mapId = DungeonMapVariantPickLogic.Resolve(
                config.RandomizeMapVariant,
                dungeonId,
                mapIds,
                vanillaMapId,
                pickMapId);

            if (logResult && mapId.HasValue)
            {
                DungeonRandomizerLog.InfoMapVariantChanged(dungeonId, vanillaMapId, mapId.Value);
            }

            return mapId;
        }

        private static int? TryPickUniformMapId(IReadOnlyList<int> mapIds) =>
            DungeonDataAccess.TryPickUniformMapId(mapIds, out int mapId) ? mapId : null;
    }
}
