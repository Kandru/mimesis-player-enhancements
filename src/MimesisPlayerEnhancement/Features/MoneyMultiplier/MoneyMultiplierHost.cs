using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.Persistence;
using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyMultiplierHost
{
    internal static bool IsParticipantClient() =>
        JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Participant;

    internal static bool ShouldApply()
    {
        if (IsParticipantClient())
            return false;

        if (JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host)
            return true;

        return MimesisSaveManager.IsHost();
    }
}
