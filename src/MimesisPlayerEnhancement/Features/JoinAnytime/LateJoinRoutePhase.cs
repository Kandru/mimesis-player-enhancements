namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>Server-side late-join tram route progress for a connecting player.</summary>
    internal enum LateJoinRoutePhase
    {
        None = 0,
        InMaintenanceLobby,
        InMaintenance,
        AwaitingClient,
        InWaitingRoom,
    }
}
