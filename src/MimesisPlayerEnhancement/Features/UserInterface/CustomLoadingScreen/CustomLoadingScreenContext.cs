namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal enum CustomLoadingScreenContext
    {
        FirstEnter,
        Maintenance,
        InTramWaiting,
        Dungeon,
        DeathMatch,
    }

    internal static class CustomLoadingScreenContextUtil
    {
        internal static CustomLoadingScreenContext FromLoadingSceneKey(string? loadingSceneKey)
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
                return CustomLoadingScreenContext.InTramWaiting;
            }

            if (string.Equals(key, "DeathMatch", StringComparison.OrdinalIgnoreCase))
            {
                return CustomLoadingScreenContext.DeathMatch;
            }

            return CustomLoadingScreenContext.Dungeon;
        }

        internal static string ToFolderName(CustomLoadingScreenContext context)
        {
            return context switch
            {
                CustomLoadingScreenContext.FirstEnter => "FirstEnter",
                CustomLoadingScreenContext.Maintenance => "Maintenance",
                CustomLoadingScreenContext.InTramWaiting => "InTramWaiting",
                CustomLoadingScreenContext.DeathMatch => "DeathMatch",
                _ => "Dungeon",
            };
        }
    }
}
