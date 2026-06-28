using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.Persistence;
using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class SpawnScalingHost
{
    internal static bool IsParticipantClient() =>
        JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Participant;

    internal static bool ShouldApplyScaling()
    {
        if (IsParticipantClient())
            return false;

        // Solo/local play often has no network host flags yet; pdata may also be null early on.
        if (JoinAnytimeHub.GetPdata() == null)
            return true;

        if (JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host)
            return true;

        return MimesisSaveManager.IsHost();
    }
}
