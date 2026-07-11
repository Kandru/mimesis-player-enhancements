namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class MoreVoicesUnify
    {
        internal static bool IsActive =>
            ModConfig.EnableMoreVoices.Value && ModConfig.UnifyIndoorOutdoorVoices.Value;
    }
}
