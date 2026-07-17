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
            FpsUiOverlay.RefreshFromConfig();
            FpsUiNetWorthOverlay.RefreshFromConfig();
            RoundStartSoundRuntime.RefreshFromConfig();
            CustomLoadingScreenRuntime.RefreshFromConfig();
        }

        internal static void OnUpdate()
        {
            WorldOverlayRuntime.OnUpdate();
            FpsUiOverlay.OnUpdate();
            FpsUiNetWorthOverlay.OnUpdate();
        }
    }
}
