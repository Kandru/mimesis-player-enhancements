using System.Linq;
using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using MimesisPlayerEnhancement.Features.ModVersionDisplay;
using MimesisPlayerEnhancement.Features.UserInterface.SpectatorPlayerList;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiPatches
    {
        private const string Feature = "Ui";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(UiPatches))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(SpectatorPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(ExtendedSaveSlotsPatches)))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(ModVersionDisplayPatches)));

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }
    }

    internal static class UiRuntime
    {
        internal static void RefreshFromConfig()
        {
            SpectatorPlayerGrid.RefreshFromConfig();
            ExtendedSaveSlotsRuntime.RefreshFromConfig();
        }
    }
}
