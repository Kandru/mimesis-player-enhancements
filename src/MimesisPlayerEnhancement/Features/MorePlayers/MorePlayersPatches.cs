namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class MorePlayersPatches
    {
        private const string Feature = "MorePlayers";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MorePlayersPatches)));
        }

        /// <summary>Re-applies player-cap limits to live networking state after config changes.</summary>
        public static void RefreshFromConfig()
        {
            InGameMenuExtendedSlots.SyncFromConfig();

            if (!ModConfig.EnableMorePlayers.Value)
            {
                if (MorePlayersPatchHelpers._lastAppliedMaxClients > MorePlayersPatchHelpers.VanillaMaxPlayers)
                {
                    MorePlayersPatchHelpers.ApplyMaxClientsToSocket(MorePlayersPatchHelpers.VanillaMaxPlayers);
                }

                MorePlayersPatchHelpers._lastAppliedMaxClients = -1;
                return;
            }

            int maxPlayers = MorePlayersPatchHelpers.GetMaxPlayers();
            if (maxPlayers != MorePlayersPatchHelpers._lastAppliedMaxClients
                && MorePlayersPatchHelpers.ApplyMaxClientsToSocket(maxPlayers))
            {
                MorePlayersPatchHelpers._lastAppliedMaxClients = maxPlayers;
            }

            VActorDictCapacity.ApplyToAllRooms();
        }

        internal static void OnSessionEnded()
        {
            MorePlayersPatchHelpers.ResetSessionState();
            InGameMenuDebugPreview.OnSessionEnded();
        }
    }
}
