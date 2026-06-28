using System;
using System.Reflection;
using Bifrost.Cooked;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

public static class MoneyMultiplierPatches
{
    private const string Feature = "MoneyMultiplier";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var result = HarmonyPatchHelper.ApplyPatchTypes(
            harmony,
            Feature,
            HarmonyPatchHelper.GetNestedPatchTypes(typeof(MoneyMultiplierPatches)));

        LogPatchAudit(harmony);
        HarmonyPatchHelper.LogPatchSummary(Feature, result);
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("set_Currency/IVroom", AccessTools.PropertySetter(typeof(IVroom), nameof(IVroom.Currency))),
            ("RefreshTargetCurrency/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))),
            ("ClampTargetCurrencyToMin/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), "ClampTargetCurrencyToMin")),
            ("ApplyLoadedGameData/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))),
            ("GetPrice/ItemMasterInfo", AccessTools.Method(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetPrice))),
            ("GetMeanPrice/ItemMasterInfo", AccessTools.Method(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))),
            ("TryGetShopItemPrice/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))),
            ("ReinforceItem/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ReinforceItem))),
        });
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
                    return;

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

    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))]
    public static class GameSessionInfoApplyLoadedGameDataPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance, MMSaveGameData saveGameData)
        {
            try
            {
                MoneyMultiplierApplier.ApplyRoundGoalFromSave(__instance, saveGameData);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ApplyLoadedGameData postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetPrice))]
    public static class ItemMasterInfoGetPricePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            try
            {
                __result = MoneyMultiplierApplier.ScaleScrapValue(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetPrice postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))]
    public static class ItemMasterInfoGetMeanPricePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            try
            {
                __result = MoneyMultiplierApplier.ScaleScrapValue(__result);
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
        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance, ref int price, bool __result)
        {
            try
            {
                if (!__result)
                    return;

                price = MoneyMultiplierApplier.ScaleShopPrice(__instance, price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryGetShopItemPrice postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ReinforceItem))]
    public static class MaintenanceRoomReinforceItemPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MaintenanceRoom __instance, ref int price)
        {
            try
            {
                price = MoneyMultiplierApplier.ScaleReinforcePrice(__instance, price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ReinforceItem prefix failed — {ex.Message}");
            }
        }
    }
}
