namespace MimesisPlayerEnhancement.Ui.MenuMirror
{
    /// <summary>
    /// Per-menu registry of feature customizations. Any change immediately rebuilds
    /// the live menu (realtime), so config toggles reflect without reopening the menu.
    /// </summary>
    internal static class MenuMirrorRegistry
    {
        private static readonly Dictionary<MenuKind, Dictionary<string, MenuCustomization>> SpecsByMenu = new()
        {
            [MenuKind.MainMenu] = new Dictionary<string, MenuCustomization>(),
            [MenuKind.InGameMenu] = new Dictionary<string, MenuCustomization>(),
        };

        internal static void SetCustomization(MenuKind kind, string featureName, MenuCustomization customization)
        {
            Dictionary<string, MenuCustomization> specs = SpecsByMenu[kind];
            if (customization.IsEmpty)
            {
                if (!specs.Remove(featureName))
                {
                    return;
                }
            }
            else
            {
                specs[featureName] = customization;
            }

            MenuMirrorController.OnRegistryChanged(kind);
        }

        internal static void ClearCustomization(MenuKind kind, string featureName)
        {
            if (SpecsByMenu[kind].Remove(featureName))
            {
                MenuMirrorController.OnRegistryChanged(kind);
            }
        }

        internal static bool HasAny(MenuKind kind) => SpecsByMenu[kind].Count > 0;

        internal static IReadOnlyCollection<MenuCustomization> GetAll(MenuKind kind) => SpecsByMenu[kind].Values;
    }
}
