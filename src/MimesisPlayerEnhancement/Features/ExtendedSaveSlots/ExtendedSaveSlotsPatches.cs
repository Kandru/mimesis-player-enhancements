using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    public static class ExtendedSaveSlotsPatches
    {
        private const string Feature = "ExtendedSaveSlots";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(ExtendedSaveSlotsPatches)));

            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        [HarmonyPatch(typeof(MMSaveGameData), nameof(MMSaveGameData.CheckSaveSlotID))]
        internal static class CheckSaveSlotIdPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(int slotID, bool includeAutoSlot, ref bool __result)
            {
                if (!ModConfig.EnableExtendedSaveSlots.Value)
                {
                    return true;
                }

                if (slotID == -1)
                {
                    __result = false;
                    return false;
                }

                if (includeAutoSlot && slotID == SaveSlotLimits.AutosaveSlotId)
                {
                    __result = true;
                    return false;
                }

                int maxManual = SaveSlotDiscovery.GetMaxManualSlots();
                __result = slotID >= SaveSlotLimits.MinManualSlotId && slotID <= maxManual;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.GetLoadedSaveData))]
        internal static class GetLoadedSaveDataPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(int slotID, ref MMSaveGameData __result)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

                if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
                {
                    __result = cached;
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.IsSlotVersionCompatible))]
        internal static class IsSlotVersionCompatiblePostfix
        {
            [HarmonyPostfix]
            private static void Postfix(int slotID, ref bool __result)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

                if (TramSavePickerController.TryGetCachedSave(slotID, out MMSaveGameData? cached) && cached != null)
                {
                    __result = cached.Version >= 1;
                }
            }
        }

        [HarmonyPatch(typeof(MainMenu), "Start")]
        internal static class MainMenuStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(MainMenu __instance)
            {
                UIPrefab_MainMenu? mainMenuUi = AccessTools.Field(typeof(MainMenu), "ui_mainmenu")
                    .GetValue(__instance) as UIPrefab_MainMenu;

                if (mainMenuUi == null)
                {
                    ModLog.Warn(Feature, "Failed to initialize save picker — main menu UI missing.");
                    return;
                }

                TramSavePickerController.OnMainMenuStarted(__instance, mainMenuUi);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "OnEnable")]
        internal static class MainMenuOnEnablePostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                TramSavePickerController.OnMainMenuShown(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), "Start")]
        internal static class MainMenuUiStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_MainMenu __instance)
            {
                TramSavePickerController.OnMainMenuShown(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefabScript), "OnButtonClick")]
        internal static class MainMenuHostButtonClickPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefabScript __instance, string _id)
            {
                if (__instance is not UIPrefab_MainMenu mainMenuUi)
                {
                    return true;
                }

                if (!MainMenuButtonWiring.IsHostButtonElement(__instance, _id, mainMenuUi.UE_HostButton))
                {
                    return true;
                }

                if (!TramSavePickerController.IsActive)
                {
                    return true;
                }

                TramSavePickerController.TryHandleHostButtonClick(mainMenuUi);
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_MainMenu), nameof(UIPrefab_MainMenu.OnHostButton), MethodType.Setter)]
        internal static class MainMenuHostButtonSetterPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(ref Action<string> value)
            {
                if (!TramSavePickerController.IsActive)
                {
                    return;
                }

                value = static _ => { };
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), "OnEnable")]
        internal static class PublicRoomListOnEnablePrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                return !TramSavePickerController.IsSavePickerList(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), "OnDisable")]
        internal static class PublicRoomListOnDisablePrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                if (!TramSavePickerController.IsSavePickerList(__instance))
                {
                    return true;
                }

                TramSavePickerController.SetSavePickerOpen(false, __instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.ShowPopup))]
        internal static class PublicRoomListShowPopupPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                return !TramSavePickerController.IsSavePickerList(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetRoomList))]
        internal static class PublicRoomListSetRoomListPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                return !TramSavePickerController.IsSavePickerList(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), nameof(UIPrefab_PublicRoomList.SetEmptyListText))]
        internal static class PublicRoomListSetEmptyListTextPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                return !TramSavePickerController.IsSavePickerList(__instance);
            }
        }

        [HarmonyPatch(typeof(UIPrefab_PublicRoomList), "RefreshBtn")]
        internal static class PublicRoomListRefreshBtnPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(UIPrefab_PublicRoomList __instance)
            {
                return !TramSavePickerController.IsSavePickerList(__instance);
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), "RequestLobbyList", typeof(int), typeof(UIPrefab_PublicRoomList))]
        internal static class SteamRequestLobbyListPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(ref UIPrefab_PublicRoomList _roomListUI)
            {
                if (TramSavePickerController.IsSavePickerList(_roomListUI))
                {
                    _roomListUI = null!;
                }
            }
        }

        [HarmonyPatch(typeof(SteamInviteDispatcher), "OnLobbyMatchListReceived")]
        internal static class SteamLobbyMatchListReceivedPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(SteamInviteDispatcher __instance)
            {
                if (TramSavePickerController.IsSavePickerList(__instance.roomListUI))
                {
                    __instance.roomListUI = null;
                }
            }
        }

        [HarmonyPatch(typeof(UiPrefab_RoomCard), nameof(UiPrefab_RoomCard.SetRoomData))]
        internal static class RoomCardSetRoomDataPrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(
                PublicRoomListData data,
                UIPrefab_PublicRoomList publicroomlist,
                UiPrefab_RoomCard __instance)
            {
                if (!TramSavePickerController.IsSavePickerList(publicroomlist))
                {
                    return true;
                }

                if (!TramSavePickerController.TryGetRowContext(data.lobbyID, out SaveSlotRowContext? context)
                    || context == null)
                {
                    return true;
                }

                ApplySaveRowDisplay(__instance, context.Entry);

                Button button = __instance.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    SaveSlotPickerPanel? panel = TramSavePickerController.Panel;
                    panel?.SelectRow(data.lobbyID, __instance);
                    EventSystem.current?.SetSelectedGameObject(null);
                });

                __instance.Show();
                return false;
            }

            private static void ApplySaveRowDisplay(UiPrefab_RoomCard card, SaveSlotEntry entry)
            {
                MMSaveGameData save = entry.Data;

                HideRoomCardElement(card, "UE_flag");
                HideRoomCardElement(card, "UE_LockIcon");
                HideRoomCardElement(card, "UE_RoomCycle");
                HideRoomCardElement(card, "UE_RepairStat");
                HideRoomCardElement(card, "UE_Status");
                HideRoomCardElement(card, "UE_Start");
                HideRoomCardElement(card, "UE_BeforeRepair");
                HideRoomCardElement(card, "UE_AfterRepair");

                ShowRoomCardElement(card, "UE_NationCode");
                SaveSlotTextHelper.SetText(
                    GetRoomCardText(card, "UE_NationCode"),
                    SaveSlotRoomListMapper.FormatSlotNumber(entry));
                SaveSlotTextHelper.SetText(
                    GetRoomCardText(card, "UE_RoomName"),
                    SaveSlotRoomListMapper.FormatLobbyName(entry));
                SaveSlotTextHelper.SetText(
                    GetRoomCardText(card, "UE_PlayerCount"),
                    SaveSlotRoomListMapper.FormatPlayerNames(save));
            }
        }

        private static void HideRoomCardElement(UiPrefab_RoomCard card, string propertyName)
        {
            if (typeof(UiPrefab_RoomCard).GetProperty(propertyName)?.GetValue(card) is Component component)
            {
                component.gameObject.SetActive(false);
            }
        }

        private static void ShowRoomCardElement(UiPrefab_RoomCard card, string propertyName)
        {
            if (typeof(UiPrefab_RoomCard).GetProperty(propertyName)?.GetValue(card) is Component component)
            {
                component.gameObject.SetActive(true);
            }
        }

        private static Component? GetRoomCardText(UiPrefab_RoomCard card, string propertyName) =>
            typeof(UiPrefab_RoomCard).GetProperty(propertyName)?.GetValue(card) as Component;
    }
}
