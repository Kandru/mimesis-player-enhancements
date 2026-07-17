using System.Reflection;
using MimesisPlayerEnhancement.Features.MoreVoices;
using MimesisPlayerEnhancement.Features.Players;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListVoice
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? IsSpeakingProperty =
            typeof(FishNetDissonancePlayer).GetProperty("IsSpeaking", InstanceFlags)
            ?? typeof(FishNetDissonancePlayer).GetProperty("isSpeaking", InstanceFlags);

        private static readonly PropertyInfo? AmplitudeProperty =
            typeof(FishNetDissonancePlayer).GetProperty("Amplitude", InstanceFlags)
            ?? typeof(FishNetDissonancePlayer).GetProperty("amplitude", InstanceFlags);

        internal static bool IsSpeaking(ulong steamId, long playerUid)
        {
            FishNetDissonancePlayer? player = ResolveDissonancePlayer(steamId, playerUid);
            if (player == null)
            {
                return false;
            }

            try
            {
                if (IsSpeakingProperty?.GetValue(player) is bool isSpeaking)
                {
                    return isSpeaking;
                }

                if (AmplitudeProperty?.GetValue(player) is float amplitude)
                {
                    return amplitude > 0.01f;
                }
            }
            catch
            {
                /* voice component may be tearing down */
            }

            return false;
        }

        private static FishNetDissonancePlayer? ResolveDissonancePlayer(ulong steamId, long playerUid)
        {
            Dictionary<string, FishNetDissonancePlayer> players = VoiceDissonancePlayerCache.GetPlayers();
            if (players.Count == 0)
            {
                return null;
            }

            if (PlayerRegistry.TryGetRecord(steamId, out PlayerRecord? record)
                && !string.IsNullOrEmpty(record.VoiceId)
                && players.TryGetValue(record.VoiceId, out FishNetDissonancePlayer? byVoiceId))
            {
                return byVoiceId;
            }

            _ = playerUid;
            return null;
        }
    }
}
