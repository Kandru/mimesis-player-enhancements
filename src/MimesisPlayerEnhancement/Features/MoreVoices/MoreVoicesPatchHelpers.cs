using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class MoreVoicesPatchHelpers
    {
        internal static readonly MethodInfo? BroadcastNewEventWithRemovalMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "ServerRpcBroadcastNewEventWithRemoval");

        [ThreadStatic]
        internal static List<long>? _lastRemovalIds;
    }
}
