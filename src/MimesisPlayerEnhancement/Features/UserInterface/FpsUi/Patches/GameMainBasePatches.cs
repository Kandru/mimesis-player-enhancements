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

        private static readonly System.Reflection.FieldInfo? InventoryUiField =
            AccessTools.Field(typeof(GameMainBase), "inventoryui");

        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance, ProtoActor actor)
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
                    if (InventoryUiField?.GetValue(__instance) is UIPrefab_Inventory inventoryUi)
                    {
                        inventoryUi.UpdateSlot(
                            actor.GetInventoryItems(),
                            actor.GetSelectedInventorySlotIndex());
                    }
                }

                __instance.UpdateInventoryUI(actor);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI player spawn sync failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class GameMainOnDestroyPostfix
    {
        private const string Feature = "Ui";

        internal static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GameMainBase), "OnDestroy");

        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                FpsUiOverlay.OnSessionEnded();
                FpsUiNetWorthOverlay.OnSessionEnded();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI session cleanup failed — {ex.Message}");
            }
        }
    }
}
