using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class MoreVoicesPatchHelpers
    {
        private const string Feature = "MoreVoices";

        internal static readonly MethodInfo? BroadcastNewEventWithRemovalMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "ServerRpcBroadcastNewEventWithRemoval");

        [ThreadStatic]
        internal static List<long>? _lastRemovalIds;

        internal static PlayerLifecycleContribution? TryDescribeArchiveStarted(SpeechEventArchive archive)
        {
            if (!ModConfig.EnableMoreVoices.Value || archive == null)
            {
                return null;
            }

            try
            {
                SpeechEventArchiveLimits.EffectiveCaps caps = SpeechEventArchiveLimits.ReadEffectiveCaps(archive);
                return new PlayerLifecycleContribution(
                    Feature,
                    $"caps {SpeechEventArchiveLimits.FormatEffectiveCaps(caps)}");
            }
            catch
            {
                return null;
            }
        }
    }
}
