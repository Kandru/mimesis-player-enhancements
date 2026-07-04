namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeRuntime
    {
        private static bool _wasEnabled;

        internal static void OnUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnUpdate();
            LateJoinManager.OnUpdate();
            JoinAnytimeLobbyController.OnUpdate();
        }

        /// <summary>Called via FeatureModule.SyncFromConfig when the JoinAnytime section changes.</summary>
        internal static void RefreshFromConfig()
        {
            bool enabled = ModConfig.EnableJoinAnytime.Value;
            if (_wasEnabled && !enabled)
            {
                LateJoinManager.Reset();
                JoinAnytimeConnectingTracker.Reset();
            }

            _wasEnabled = enabled;

            if (enabled)
            {
                JoinAnytimeLobbyController.RefreshLobbyState(force: true);
            }
        }
    }
}
