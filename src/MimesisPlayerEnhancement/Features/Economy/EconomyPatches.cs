namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyPatches
    {
        private const string Feature = "Economy";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(EconomyPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        internal static void OnSessionEnded()
        {
            EconomyApplier.OnSessionEnded();
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the Economy section changes.</summary>
        public static void RefreshFromConfig()
        {
            if (SceneScopedConfigGate.IsModuleSyncDeferred(Feature))
            {
                return;
            }

            EconomyApplier.InvalidateScrapScaling();
            MaintenanceShopApplier.NotifyConfigChanged();

            if (!ModConfig.EnableEconomy.Value)
            {
                MaintenanceShopApplier.RestoreVanillaPrices();
            }
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("set_Currency/IVroom", AccessTools.PropertySetter(typeof(IVroom), nameof(IVroom.Currency))),
                ("InitMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.InitMaintenenceRoom))),
                ("FinalPrice/ItemElement", AccessTools.PropertyGetter(typeof(ItemElement), nameof(ItemElement.FinalPrice))),
                ("toItemInfo/ConsumableItemElement", AccessTools.Method(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))),
                ("toItemInfo/MiscellanyItemElement", AccessTools.Method(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))),
                ("GetMeanPrice/ItemMasterInfo", AccessTools.Method(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))),
                ("GetNewItemElement/IVroom", AccessTools.Method(typeof(IVroom), nameof(IVroom.GetNewItemElement))),
                ("TryGetShopItemPrice/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))),
                ("InitShopItems/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.InitShopItems))),
                ("ApplyLoadedGameData/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ApplyLoadedGameData))),
                ("OnEnterChannel/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnEnterChannel))),
                ("OnRequestStartSession/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnRequestStartSession))),
                ("HandleReinforceItem/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.HandleReinforceItem))),
            ]);
        }
    }
}
