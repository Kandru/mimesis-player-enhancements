using System;
using System.Reflection;
using System.Reflection.Emit;
using Bifrost.Cooked;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    public static class MoneyMultiplierPatches
    {
        private const string Feature = "MoneyMultiplier";

        private static readonly FieldInfo UpgradeCostField =
            AccessTools.Field(typeof(ItemEquipmentInfo), nameof(ItemEquipmentInfo.UpgradeCost))
            ?? throw new InvalidOperationException("ItemEquipmentInfo.UpgradeCost not found");

        private static readonly MethodInfo ScaleReinforceCostMethod =
            AccessTools.Method(typeof(MoneyMultiplierApplier), nameof(MoneyMultiplierApplier.ScaleReinforceCost))
            ?? throw new InvalidOperationException("MoneyMultiplierApplier.ScaleReinforceCost not found");

        private static bool IsScrapHotPathEnabled => ModConfig.EnableMoneyMultiplier.Value;

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MoneyMultiplierPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the MoneyMultiplier section changes.</summary>
        public static void RefreshFromConfig()
        {
            MaintenanceShopApplier.NotifyConfigChanged();

            if (!ModConfig.EnableMoneyMultiplier.Value)
            {
                MaintenanceShopApplier.RestoreVanillaPrices();
            }
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("set_Currency/IVroom", AccessTools.PropertySetter(typeof(IVroom), nameof(IVroom.Currency))),
                ("RefreshTargetCurrency/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))),
                ("ClampTargetCurrencyToMin/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), "ClampTargetCurrencyToMin")),
                ("InitMaintenenceRoom/VRoomManager", AccessTools.Method(typeof(VRoomManager), nameof(VRoomManager.InitMaintenenceRoom))),
                ("FinalPrice/ItemElement", AccessTools.PropertyGetter(typeof(ItemElement), nameof(ItemElement.FinalPrice))),
                ("toItemInfo/ConsumableItemElement", AccessTools.Method(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))),
                ("toItemInfo/MiscellanyItemElement", AccessTools.Method(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))),
                ("GetMeanPrice/ItemMasterInfo", AccessTools.Method(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))),
                ("TryGetShopItemPrice/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))),
                ("InitShopItems/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.InitShopItems))),
                ("ApplyLoadedGameData/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ApplyLoadedGameData))),
                ("OnEnterChannel/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnEnterChannel))),
                ("HandleReinforceItem/VPlayer", AccessTools.Method(typeof(VPlayer), nameof(VPlayer.HandleReinforceItem))),
            ]);
        }

        [HarmonyPatch(typeof(IVroom), nameof(IVroom.Currency), MethodType.Setter)]
        public static class IVroomSetCurrencyPatch
        {
            [HarmonyPrefix]
            public static void Prefix(IVroom __instance, ref int value)
            {
                try
                {
                    if (__instance is not MaintenanceRoom room)
                    {
                        return;
                    }

                    MoneyMultiplierApplier.ApplyStartupMoney(room, ref value);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"set_Currency prefix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))]
        public static class GameSessionInfoRefreshTargetCurrencyPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GameSessionInfo __instance)
            {
                try
                {
                    MoneyMultiplierApplier.ApplyRoundGoal(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"RefreshTargetCurrency postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GameSessionInfo), "ClampTargetCurrencyToMin")]
        public static class GameSessionInfoClampTargetCurrencyToMinPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GameSessionInfo __instance)
            {
                try
                {
                    MoneyMultiplierApplier.ApplyRoundGoal(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ClampTargetCurrencyToMin postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.InitMaintenenceRoom))]
        public static class VRoomManagerInitMaintenenceRoomStartupMoneyPatch
        {
            [HarmonyPrefix]
            public static void Prefix(int saveSlotID, ref bool __state)
            {
                __state = StartupMoneyLoadGuard.TryEnterForSaveSlot(saveSlotID);
            }

            [HarmonyFinalizer]
            public static void Finalizer(bool __state)
            {
                if (__state)
                {
                    StartupMoneyLoadGuard.Exit();
                }
            }
        }

        [HarmonyPatch(typeof(ItemElement), nameof(ItemElement.FinalPrice), MethodType.Getter)]
        public static class ItemElementFinalPricePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                if (!IsScrapHotPathEnabled)
                {
                    return;
                }

                try
                {
                    int scaled = MoneyMultiplierApplier.ScaleScrapValue(__result);
                    if (scaled != __result)
                    {
                        __result = scaled;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"FinalPrice postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))]
        public static class ConsumableItemElementToItemInfoPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ItemInfo __result)
            {
                if (!IsScrapHotPathEnabled)
                {
                    return;
                }

                try
                {
                    __result.price = MoneyMultiplierApplier.ScaleScrapValue(__result.price);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ConsumableItemElement.toItemInfo postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))]
        public static class MiscellanyItemElementToItemInfoPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ItemInfo __result)
            {
                if (!IsScrapHotPathEnabled)
                {
                    return;
                }

                try
                {
                    __result.price = MoneyMultiplierApplier.ScaleScrapValue(__result.price);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"MiscellanyItemElement.toItemInfo postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))]
        public static class ItemMasterInfoGetMeanPricePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                if (!IsScrapHotPathEnabled)
                {
                    return;
                }

                try
                {
                    int scaled = MoneyMultiplierApplier.ScaleScrapValue(__result);
                    if (scaled != __result)
                    {
                        __result = scaled;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"GetMeanPrice postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))]
        public static class MaintenanceRoomTryGetShopItemPricePatch
        {
            [HarmonyPrefix]
            public static void Prefix(MaintenanceRoom __instance)
            {
                try
                {
                    MaintenanceShopApplier.EnsureApplied(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"TryGetShopItemPrice prefix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.InitShopItems))]
        public static class MaintenanceRoomInitShopItemsPatch
        {
            [HarmonyPrefix]
            public static void Prefix(MaintenanceRoom __instance)
            {
                try
                {
                    MaintenanceShopApplier.PrepareForShopInit(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"InitShopItems prefix failed — {ex.Message}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(MaintenanceRoom __instance)
            {
                try
                {
                    MaintenanceShopApplier.ApplyAfterShopInit(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"InitShopItems postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ApplyLoadedGameData))]
        public static class MaintenanceRoomApplyLoadedGameDataPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MaintenanceRoom __instance)
            {
                try
                {
                    MaintenanceShopApplier.ApplyAfterLoad(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ApplyLoadedGameData postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnEnterChannel))]
        public static class MaintenanceRoomOnEnterChannelPatch
        {
            [HarmonyPrefix]
            public static void Prefix(MaintenanceRoom __instance)
            {
                try
                {
                    MaintenanceShopApplier.EnsureApplied(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnEnterChannel prefix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleReinforceItem))]
        public static class VPlayerHandleReinforceItemPatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions,
                MethodBase original)
            {
                LocalVariableInfo? maintenanceRoomLocal = FindMaintenanceRoomLocal(original);
                if (maintenanceRoomLocal == null)
                {
                    ModLog.Warn(Feature, "HandleReinforceItem transpiler skipped — MaintenanceRoom local not found");
                    return instructions;
                }

                List<CodeInstruction> codes = [.. instructions];
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Ldfld
                        || !ReferenceEquals(codes[i].operand, UpgradeCostField))
                    {
                        continue;
                    }

                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc, maintenanceRoomLocal.LocalIndex));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, ScaleReinforceCostMethod));
                    i += 2;
                }

                return codes;
            }

            private static LocalVariableInfo? FindMaintenanceRoomLocal(MethodBase method)
            {
                MethodBody? body = method.GetMethodBody();
                if (body == null)
                {
                    return null;
                }

                foreach (LocalVariableInfo local in body.LocalVariables)
                {
                    if (local.LocalType == typeof(MaintenanceRoom))
                    {
                        return local;
                    }
                }

                return null;
            }
        }
    }
}
