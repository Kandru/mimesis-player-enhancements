namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning
{
    internal readonly struct MimicVoicePlaybackSnapshot
    {
        internal MimicVoicePlaybackSnapshot(
            SpeechEvent speechEvent,
            string mimickingPlayerId,
            string pickReason)
        {
            SpeechEvent = speechEvent;
            MimickingPlayerId = mimickingPlayerId;
            PickReason = pickReason;
        }

        internal SpeechEvent SpeechEvent { get; }

        internal string MimickingPlayerId { get; }

        internal string PickReason { get; }
    }

    internal static class MimicVoiceTuningPlaybackTrace
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<int, MimicVoicePlaybackSnapshot> LastByMimicActorId = [];

        internal static void Record(
            int mimicActorId,
            SpeechEvent speechEvent,
            string mimickingPlayerId,
            string pickReason)
        {
            lock (Gate)
            {
                LastByMimicActorId[mimicActorId] = new MimicVoicePlaybackSnapshot(
                    speechEvent,
                    mimickingPlayerId,
                    pickReason);
            }
        }

        internal static bool TryTake(int mimicActorId, out MimicVoicePlaybackSnapshot snapshot)
        {
            lock (Gate)
            {
                if (!LastByMimicActorId.Remove(mimicActorId, out snapshot))
                {
                    snapshot = default;
                    return false;
                }

                return true;
            }
        }
    }
}
