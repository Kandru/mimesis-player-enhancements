using System.Linq;
using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;

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
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InGameMenuPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SurvivalResultPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ExtendedSaveSlotsPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ModVersionDisplayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MenuMirrorPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WorldOverlayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(FpsUiPatches)));

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);
            RoundStartSoundPatches.Apply(harmony);
            CustomLoadingScreenPatches.Apply(harmony);
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
            CustomLoadingScreenRuntime.RefreshFromConfig();
        }

        internal static void OnUpdate()
        {
            LoadingWaitPlayerListRuntime.OnUpdate();
            WorldOverlayRuntime.OnUpdate();
            FpsUiOverlay.OnUpdate();
            FpsUiNetWorthOverlay.OnUpdate();
        }

        internal static void OnSessionEnded()
        {
            MenuMirrorController.OnSessionEnded();
            SpectatorPlayerGrid.OnSessionEnded();
            InGameMenuPlayerListOverlay.OnSessionEnded();
            SurvivalResultDebugPreview.OnSessionEnded();
            LoadingWaitPlayerListRuntime.OnSessionEnded();
            CustomLoadingScreenRuntime.OnSessionEnded();
            ExtendedSaveSlotsRuntime.OnSessionEnded();
            WorldOverlayRuntime.OnSessionEnded();
            FpsUiOverlay.OnSessionEnded();
            FpsUiNetWorthOverlay.OnSessionEnded();
            RoundStartSoundRuntime.OnSessionEnded();
        }
    }
}
