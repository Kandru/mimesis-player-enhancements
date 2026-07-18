namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoicePickListBuilder
    {
        internal static void AppendArchiveWarmedPairs(
            SpeechEventArchive archive,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            if (archive == null || destination == null)
            {
                return;
            }

            if (VoicePerformanceRuntime.IsActive)
            {
                VoiceWarmCache.AppendWarmedPairs(archive, destination);
                return;
            }

            foreach (SpeechEvent speechEvent in archive.GetWarmedUpSpeechEvents())
            {
                destination.Add((archive.PlayerId, speechEvent));
            }
        }
    }
}
