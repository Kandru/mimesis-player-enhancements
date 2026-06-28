using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.Persistence;
using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonRandomizerHost
{
    internal static bool IsParticipantClient() =>
        JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Participant;

    internal static bool ShouldApply()
    {
        if (!ModConfig.EnableDungeonRandomizer.Value)
            return false;

        if (IsParticipantClient())
            return false;

        if (JoinAnytimeHub.GetPdata() == null)
            return true;

        if (JoinAnytimeHub.GetPdata()?.ClientMode == NetworkClientMode.Host)
            return true;

        return MimesisSaveManager.IsHost();
    }
}
