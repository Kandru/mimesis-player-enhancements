namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Host-only gate for MoreVoices. Joining clients keep the feature off even if local TOML enables it
    /// (host settings are not synced yet).
    /// </summary>
    internal static class MoreVoicesRuntime
    {
        internal static bool ShouldApply() =>
            ShouldApply(
                ModConfig.EnableMoreVoices.Value,
                HostApplyGate.ShouldApplyHostOnlyFeature());

        internal static bool ShouldApply(bool enableMoreVoices, bool isHost) =>
            enableMoreVoices && isHost;
    }
}
