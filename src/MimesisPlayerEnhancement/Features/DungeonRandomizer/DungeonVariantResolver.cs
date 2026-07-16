namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonVariantResolver
    {
        private const string Feature = "DungeonRandomizer";

        internal static int? ResolveMapId(DungeonMasterInfo info, int vanillaMapId)
        {
            if (!SceneScopedConfigGate.DungeonRandomizer.RandomizeMapVariant)
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
