using System.Reflection;

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

        [HarmonyPatch(typeof(UIPrefabScript), "OnButtonClick", typeof(string))]
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

        [HarmonyPatch(typeof(UIManager), "Update")]
        internal static class UiManagerUpdateEscapePrefix
        {
            private static Type? _inputActionType;
            private static object? _escapeActionValue;
            private static MethodInfo? _wasPressedThisFrameMethod;
            private static Type? _cachedInputManagerType;

            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (!TramSavePickerController.IsSavePickerOpen || TramSavePickerController.Panel == null)
                {
                    return true;
                }

                object? inputman = SaveSlotGameAccess.TryGetInputManager();
                if (inputman == null || !WasEscapePressed(inputman))
                {
                    return true;
                }

                TramSavePickerController.Panel.Close();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                return false;
            }

            private static bool WasEscapePressed(object inputman)
            {
                EnsureInputReflectionCached(inputman);
                return _wasPressedThisFrameMethod != null
                    && _escapeActionValue != null
                    && _wasPressedThisFrameMethod.Invoke(inputman, [_escapeActionValue]) is true;
            }

            private static void EnsureInputReflectionCached(object inputman)
            {
                Type inputManagerType = inputman.GetType();
                if (_cachedInputManagerType == inputManagerType
                    && _wasPressedThisFrameMethod != null
                    && _escapeActionValue != null)
                {
                    return;
                }

                _cachedInputManagerType = inputManagerType;
                _inputActionType ??= AccessTools.TypeByName("Mimic.InputSystem.InputAction");
                if (_inputActionType == null)
                {
                    _wasPressedThisFrameMethod = null;
                    _escapeActionValue = null;
                    return;
                }

                _escapeActionValue ??= Enum.Parse(_inputActionType, "Escape");
                _wasPressedThisFrameMethod = AccessTools.Method(
                    inputManagerType,
                    "wasPressedThisFrame",
                    [_inputActionType]);
            }
        }
    }
}
