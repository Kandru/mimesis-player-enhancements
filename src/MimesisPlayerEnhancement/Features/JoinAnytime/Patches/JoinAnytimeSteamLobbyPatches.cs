namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.CreateLobby), typeof(bool), typeof(bool))]
    internal static class SteamInviteDispatcherCreateLobbyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isOpenForRandomMatch, bool isRetryAttempt)
        {
            JoinAnytimeLobbyController.OnLobbyCreated(__instance, isOpenForRandomMatch, isRetryAttempt);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyPublic))]
    internal static class SteamInviteDispatcherSetLobbyPublicPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(bool isPublic)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (isPublic)
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }
            else if (JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug(Feature, "Blocked SetLobbyPublic(false) for join-anytime host.");
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(SteamInviteDispatcher __instance, bool isPublic)
        {
            JoinAnytimeLobbyController.OnSetLobbyPublicCompleted(__instance, isPublic);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresenceInLobby))]
    internal static class SteamInviteDispatcherSetLobbyPublicPresencePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(__instance))
            {
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetPresencePlaying))]
    internal static class SteamInviteDispatcherSetPresencePlayingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (JoinAnytimeHub.IsHostLobbyPublic(__instance))
            {
                JoinAnytimeLobbyController.ApplyLobbyPresence(__instance, wantsPublic: true);
                JoinAnytimeLobbyController.RefreshLobbyState(force: true);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdatePlayerGroupSize))]
    internal static class SteamInviteDispatcherUpdatePlayerGroupSizePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref int playerCount)
        {
            if (!ModConfig.EnableJoinAnytime.Value || !JoinAnytimeHub.IsHost())
            {
                return;
            }

            int sessionCount = JoinAnytimeRoomTools.GetSessionPlayerCount();
            playerCount = JoinAnytimeLobbyDisplay.GetBrowsePlayerCount(sessionCount);
        }
    }

    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.UpdateLobbyData))]
    internal static class SteamInviteDispatcherUpdateLobbyDataPatch
    {
        private const string Feature = "JoinAnytime";

        [HarmonyPrefix]
        private static bool Prefix(SteamInviteDispatcher __instance, string key, string value)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return true;
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeHub.IsHost())
            {
                JoinAnytimeLobbyController.SetHostWantsPublicMatchmaking(true);
            }

            if (string.Equals(key, SteamInviteDispatcher.IS_PUBLIC_KEY, StringComparison.Ordinal)
                && string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                && JoinAnytimeLobbyController.ShouldBlockPublicRoomClose())
            {
                ModLog.Debug(Feature, "Blocked PublicRoom=false lobby data update for join-anytime host.");
                JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
                return false;
            }

            return true;
        }
    }
}
