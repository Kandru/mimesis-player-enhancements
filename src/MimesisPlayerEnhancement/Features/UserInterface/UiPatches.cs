using System.Linq;
using MimesisPlayerEnhancement.Features.UserInterface.InventoryNumberKeys;
using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitMovementLock;
using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using MimesisPlayerEnhancement.Features.UserInterface.SpectatorVoiceBalance;
using MimesisPlayerEnhancement.Features.UserInterface.VoiceNoiseGate;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiPatches
    {
        private const string Feature = "Ui";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            // Umbrella registration: ExtendedSaveSlots, ModVersionDisplay, and MenuMirror live
            // outside Features/UserInterface/ but share the Ui FeatureModule lifecycle.
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(UiPatches))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SpectatorPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(LoadingWaitPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(LoadingWaitMovementLockPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InGameMenuPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SurvivalResultPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ExtendedSaveSlotsPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ModVersionDisplayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MenuMirrorPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WorldOverlayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(FpsUiPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InventorySlotOptimizationPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InventoryNumberKeyPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(VoiceNoiseGatePatches)));

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);
            RoundStartSoundPatches.Apply(harmony);
            DiscoBallSoundPatches.Apply(harmony);
        }
    }

    internal static class UiRuntime
    {
        internal static void RefreshFromConfig()
        {
            SpectatorPlayerGrid.RefreshFromConfig();
            LoadingWaitPlayerListRuntime.RefreshFromConfig();
            InGameMenuPlayerListOverlay.RefreshFromConfig();
            ExtendedSaveSlotsRuntime.RefreshFromConfig();
            WorldOverlayGate.RefreshCache();
            WorldOverlayRuntime.RefreshFromConfig();
            FpsUiOverlay.RefreshFromConfig();
            FpsUiNetWorthOverlay.RefreshFromConfig();
            RoundStartSoundRuntime.RefreshFromConfig();
            DiscoBallSoundRuntime.RefreshFromConfig();
            SpectatorVoiceBalanceRuntime.RefreshFromConfig();
            VoiceNoiseGateRuntime.RefreshFromConfig();
        }

        internal static void OnUpdate()
        {
            LoadingWaitMovementLockRuntime.OnUpdate();
            LoadingWaitPlayerListRuntime.OnUpdate();
            WorldOverlayRuntime.OnUpdate();
            FpsUiOverlay.OnUpdate();
            FpsUiNetWorthOverlay.OnUpdate();
            SpectatorVoiceBalanceRuntime.OnUpdate();
            ProtoActorInventoryAccess.ProcessPendingSelect();
        }

        internal static void OnSessionEnded()
        {
            MenuMirrorController.OnSessionEnded();
            SpectatorPlayerGrid.OnSessionEnded();
            InGameMenuPlayerListOverlay.OnSessionEnded();
            SurvivalResultDebugPreview.OnSessionEnded();
            LoadingWaitMovementLockRuntime.OnSessionEnded();
            LoadingWaitPlayerListRuntime.OnSessionEnded();
            ExtendedSaveSlotsRuntime.OnSessionEnded();
            WorldOverlayRuntime.OnSessionEnded();
            FpsUiOverlay.OnSessionEnded();
            FpsUiNetWorthOverlay.OnSessionEnded();
            RoundStartSoundRuntime.OnSessionEnded();
            DiscoBallSoundRuntime.OnSessionEnded();
            SpectatorVoiceBalanceRuntime.OnSessionEnded();
            VoiceNoiseGateRuntime.OnSessionEnded();
            ProtoActorInventoryAccess.ClearPendingSelect();
        }
    }
}
