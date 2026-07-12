using System.Linq;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiPatches
    {
        private const string Feature = "Ui";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(UiPatches))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SpectatorPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InGameMenuPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ExtendedSaveSlotsPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ModVersionDisplayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MenuMirrorPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WorldOverlayPatches)));

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
                ("Start/UIPrefab_InGameMenu (player list layout)", AccessTools.Method(typeof(UIPrefab_InGameMenu), "Start")),
                ("OnEnable/UIPrefab_InGameMenu (player list layout)", AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnEnable")),
            ]);
        }
    }

    internal static class UiRuntime
    {
        internal static void RefreshFromConfig()
        {
            SpectatorPlayerGrid.RefreshFromConfig();
            InGameMenuPlayerListOverlay.RefreshFromConfig();
            ExtendedSaveSlotsRuntime.RefreshFromConfig();
            WorldOverlayGate.RefreshCache();
            WorldOverlayRuntime.RefreshFromConfig();
        }
    }
}
