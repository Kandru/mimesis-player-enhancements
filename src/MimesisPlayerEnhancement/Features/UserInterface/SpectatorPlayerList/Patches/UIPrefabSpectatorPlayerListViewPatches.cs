using System.Reflection;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.UserInterface.SpectatorPlayerList.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_Spectator_PlayerListView.cs:L25-41
    [HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), "Start")]
    internal static class PlayerListViewStartPostfix
    {
        private const string Feature = "Ui";

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

    // game@0.3.1 Assembly-CSharp/UIPrefab_Spectator_PlayerListView.cs:L43-86
    [HarmonyPatch(typeof(UIPrefab_Spectator_PlayerListView), nameof(UIPrefab_Spectator_PlayerListView.UpdatePlayerListView))]
    internal static class PlayerListViewUpdatePrefix
    {
        private const string Feature = "Ui";

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

    // game@0.3.1 Assembly-CSharp/UIPrefabScript.cs:L97-116
    [HarmonyPatch]
    internal static class PlayerListViewHidePostfix
    {
        private const string Feature = "Ui";

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
