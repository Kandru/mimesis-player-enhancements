namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonMapVariantPickLogic
    {
        internal static int? Resolve(
            bool randomizeMapVariant,
            int dungeonId,
            IReadOnlyList<int> mapIds,
            int vanillaMapId,
            Func<IReadOnlyList<int>, int?> pickMapId)
        {
            if (!randomizeMapVariant)
            {
                return null;
            }

            int? replacement = pickMapId(mapIds);
            if (!replacement.HasValue)
            {
                return null;
            }

            return replacement.Value;
        }
    }
}
