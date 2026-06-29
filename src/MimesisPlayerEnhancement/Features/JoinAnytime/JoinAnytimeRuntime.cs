namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeRuntime
    {
        internal static void OnUpdate()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnUpdate();
            JoinAnytimeLobbyController.OnUpdate();
        }
    }
}
