namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi.Patches
{
    [HarmonyPatch(typeof(GameMainBase), "InitCommonUIValue")]
    internal static class InitCommonUiValuePostfix
    {
        private const string Feature = "Ui";

        private static readonly System.Reflection.FieldInfo? IngameUiField =
            AccessTools.Field(typeof(GameMainBase), "ingameui");

        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance)
        {
            if (!FpsUiOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                if (IngameUiField?.GetValue(__instance) is not UIPrefab_InGame ingameUi)
                {
                    return;
                }

                FpsUiOverlay.Attach(ingameUi);
                FpsUiOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI init values sync failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.OnPlayerSpawn))]
    internal static class OnPlayerSpawnPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(ProtoActor actor)
        {
            if (!actor.AmIAvatar())
            {
                return;
            }

            try
            {
                if (FpsUiOverlay.IsEnabled())
                {
                    FpsUiOverlay.NotifyInventoryShown();
                }

                if (FpsUiNetWorthOverlay.IsEnabled())
                {
                    FpsUiNetWorthOverlay.NotifyInventoryShown();
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI player spawn sync failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), nameof(GameMainBase.UpdateInventoryUI))]
    internal static class UpdateInventoryUiPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(ProtoActor actor)
        {
            try
            {
                if (FpsUiOverlay.IsEnabled())
                {
                    FpsUiOverlay.NotifyInventoryShown();
                }

                if (FpsUiNetWorthOverlay.IsEnabled())
                {
                    FpsUiNetWorthOverlay.NotifyInventoryShown();
                    FpsUiNetWorthOverlay.UpdateFromActor(actor);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI inventory net-worth update failed — {ex.Message}");
            }
        }
    }
}
