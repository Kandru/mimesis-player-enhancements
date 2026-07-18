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

            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(UiPatches))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SpectatorPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(LoadingWaitPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(InGameMenuPlayerListPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ExtendedSaveSlotsPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(ModVersionDisplayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MenuMirrorPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(WorldOverlayPatches)))
                .Concat(HarmonyPatchHelper.GetNamespacePatchTypes(typeof(FpsUiPatches)));

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
            RoundStartSoundPatches.Apply(harmony);
            CustomLoadingScreenPatches.Apply(harmony);
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
                ("Start/UIPrefab_InGame (FPS UI)", AccessTools.Method(typeof(UIPrefab_InGame), "Start")),
                ("OnShow/UIPrefab_InGame (FPS UI)", AccessTools.Method(typeof(UIPrefab_InGame), "OnShow")),
                ("OnHpChanged/UIPrefab_InGame (FPS UI)", AccessTools.Method(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.OnHpChanged))),
                ("OnContaChanged/UIPrefab_InGame (FPS UI)", AccessTools.Method(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.OnContaChanged))),
                ("SetVisibleOxyGauge/UIPrefab_InGame (FPS UI)", AccessTools.Method(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.SetVisibleOxyGauge))),
                ("Show/UIPrefab_Inventory (FPS UI layout)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))),
                ("UpdateSlot/UIPrefab_Inventory (FPS UI layout)", AccessTools.Method(typeof(UIPrefab_Inventory), nameof(UIPrefab_Inventory.UpdateSlot))),
                ("InitCommonUIValue/GameMainBase (FPS UI)", AccessTools.Method(typeof(GameMainBase), "InitCommonUIValue")),
                ("OnPlayerSpawn/GameMainBase (FPS UI)", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerSpawn))),
                ("OnDestroy/GameMainBase (FPS UI cleanup)", AccessTools.Method(typeof(GameMainBase), "OnDestroy")),
                ("Hide/UIPrefabScript (FPS UI vitals)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))),
                ("Hide/UIPrefab_Inventory (FPS UI net worth)", AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide))),
                ("SetVersionText/UIPrefab_MainMenu", AccessTools.Method(typeof(UIPrefab_MainMenu), "SetVersionText")),
                ("SetVersionText/UIPrefab_InGameMenu", AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetVersionText")),
            ]);
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
