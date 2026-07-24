namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class MoreVoicesUnify
    {
        internal static bool IsActive =>
            MoreVoicesRuntime.ShouldApply() && ModConfig.UnifyIndoorOutdoorVoices.Value;
    }
}
