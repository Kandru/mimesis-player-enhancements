using System.Linq;
using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using MimesisPlayerEnhancement.Features.ModVersionDisplay;
using MimesisPlayerEnhancement.Features.UserInterface.SpectatorPlayerList;
using MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays;
using MimesisPlayerEnhancement.Ui.MenuMirror;

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
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(ModVersionDisplayPatches)))
                .Concat(HarmonyPatchHelper.GetNestedPatchTypes(typeof(MenuMirrorPatches)))
                .Concat(WorldOverlayPatches.GetPatchTypes());

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("Start/UIPrefab_Spectator_PlayerListView", AccessTools.Method(typeof(UIPrefab_Spectator_PlayerListView), "Start")),
                ("UpdatePlayerListView/UIPrefab_Spectator_PlayerListView", AccessTools.Method(typeof(UIPrefab_Spectator_PlayerListView), nameof(UIPrefab_Spectator_PlayerListView.UpdatePlayerListView))),
                ("Hide/UIPrefabScript (spectator list)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))),
            ]);
        }
    }

    internal static class UiRuntime
    {
        internal static void RefreshFromConfig()
        {
            SpectatorPlayerGrid.RefreshFromConfig();
            ExtendedSaveSlotsRuntime.RefreshFromConfig();
            WorldOverlayGate.RefreshCache();
            WorldOverlayRuntime.RefreshFromConfig();
        }
    }
}
