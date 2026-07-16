namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplaysRuntime
    {
        private const string Feature = "Replays";

        internal static bool IsEnabled => ModConfig.EnableReplays.Value;

        internal static bool ShouldKeepLocalReplays() =>
            IsEnabled && ModConfig.KeepLocalReplays.Value && HostStatusCache.IsHostFast();

        internal static void RefreshFromConfig()
        {
            ReplayMenuButton.SyncVisibility();
            if (!IsEnabled)
            {
                ReplayPickerController.CloseIfOpen();
                ReplayPlaybackEngine.StopPlayback(silent: true);
            }

            ModLog.Debug(Feature, $"Replays refreshed — enabled={IsEnabled}, keepLocal={ModConfig.KeepLocalReplays.Value}");
        }
    }
}
