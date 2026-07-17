namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal enum CustomLoadingScreenContext
    {
        FirstEnter,
        Maintenance,
        TramScene,
        DungeonStart,
        DungeonEnd,
        DeathMatch,
    }

    internal static class CustomLoadingScreenContextUtil
    {
        // Entering the tram uses the same game key ("InTramWaiting") whether the players come
        // from maintenance/start room or from a finished dungeon. The previous context decides
        // which of the two transitions this is.
        internal static CustomLoadingScreenContext FromLoadingSceneKey(
            string? loadingSceneKey,
            CustomLoadingScreenContext? previousContext)
        {
            string key = loadingSceneKey?.Trim() ?? "";
            if (string.Equals(key, "FirstEnter", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenContext.FirstEnter;
            }

            if (string.Equals(key, "Maintenance", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenContext.Maintenance;
            }

            if (string.Equals(key, "InTramWaiting", StringComparison.OrdinalIgnoreCase))
            {
                return previousContext == CustomLoadingScreenContext.DungeonStart
                    ? CustomLoadingScreenContext.DungeonEnd
                    : CustomLoadingScreenContext.TramScene;
            }

            if (string.Equals(key, "DeathMatch", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenContext.DeathMatch;
            }

            // Per-dungeon excel keys (e.g. "Dungeon_Forest") and the "Default" fallback.
            return CustomLoadingScreenContext.DungeonStart;
        }

        internal static string ToFolderName(CustomLoadingScreenContext context)
        {
            return context switch
            {
                CustomLoadingScreenContext.FirstEnter => "FirstEnter",
                CustomLoadingScreenContext.Maintenance => "Maintenance",
                CustomLoadingScreenContext.TramScene => "TramScene",
                CustomLoadingScreenContext.DungeonEnd => "DungeonEnd",
                CustomLoadingScreenContext.DeathMatch => "DeathMatch",
                _ => "DungeonStart",
            };
        }
    }
}
