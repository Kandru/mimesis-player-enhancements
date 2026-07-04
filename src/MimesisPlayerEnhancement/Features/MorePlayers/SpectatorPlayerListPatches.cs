using System;
using System.Reflection;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class SpectatorPlayerListPatches
    {
        private const string Feature = "MorePlayers";

        [HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), "Start")]
        internal static class PlayerListViewStartPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(UIPrefab_Spectator_PlayerListView __instance)
            {
                if (!SpectatorPlayerGrid.IsEnabled())
                {
                    return;
                }

                try
                {
                    SpectatorPlayerGrid.Initialize(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Spectator player list init failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), nameof(UIPrefab_Spectator_PlayerListView.UpdatePlayerListView))]
        internal static class PlayerListViewUpdatePrefix
        {
            [HarmonyPrefix]
            private static bool Prefix(
                UIPrefab_Spectator_PlayerListView __instance,
                List<Tuple<int, bool, bool, bool>> actorsInfo,
                CancellationToken cancellationToken)
            {
                if (!SpectatorPlayerGrid.IsEnabled())
                {
                    return true;
                }

                try
                {
                    SpectatorPlayerGrid.Update(__instance, actorsInfo, cancellationToken);
                    return false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Spectator player list update failed — {ex.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch]
        internal static class PlayerListViewHidePostfix
        {
            internal static MethodBase? TargetMethod() =>
                AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide));

            [HarmonyPostfix]
            private static void Postfix(UIPrefabScript __instance)
            {
                if (__instance is not UIPrefab_Spectator_PlayerListView listView)
                {
                    return;
                }

                try
                {
                    SpectatorPlayerGrid.HandleDisable(listView);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Spectator player list hide cleanup failed — {ex.Message}");
                }
            }
        }
    }
}
