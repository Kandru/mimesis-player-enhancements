namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class MapPlacedEncounterScheduleResolver
    {
        internal static bool ShouldScheduleEncounter(
            SpawnScalingSceneConfig config,
            SpawnCategory category,
            bool creditConsumed,
            bool hasRespawnBudget)
        {
            if (creditConsumed)
            {
                return true;
            }

            if (category == SpawnCategory.Trap && TrapRespawnDelayResolver.IsForceRespawnActive(config))
            {
                return true;
            }

            return hasRespawnBudget;
        }

        internal static bool HasRespawnBudget(
            SpawnType spawnType,
            int maxRespawnCount,
            int currentSpawnCount,
            bool enableReset)
        {
            return !spawnType.Equals(SpawnType.OnStartMap)
                && (maxRespawnCount == 0
                    || currentSpawnCount < maxRespawnCount
                    || enableReset);
        }
    }
}
