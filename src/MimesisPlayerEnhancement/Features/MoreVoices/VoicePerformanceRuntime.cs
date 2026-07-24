namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoicePerformanceRuntime
    {
        internal static bool IsActive =>
            MoreVoicesRuntime.ShouldApply() && ModConfig.EnableVoicePerformanceCache.Value;

        internal static int ClipCacheMaxEntries
        {
            get
            {
                int max = ModConfig.VoiceClipCacheMaxEntries.Value;
                return max < 1 ? 1 : max;
            }
        }

        internal static void RefreshFromConfig()
        {
            if (!IsActive)
            {
                VoiceWarmCache.ClearAll();
                VoiceClipCache.ClearAll();
                VoiceDissonancePlayerCache.Clear();
                VoiceMimicActorCache.Clear();
                return;
            }

            foreach (SpeechEventArchive archive in SpeechEventArchiveRegistry.EnumerateActive())
            {
                VoiceWarmCache.Attach(archive);
            }
        }

        internal static void ClearAll()
        {
            VoiceWarmCache.ClearAll();
            VoiceClipCache.ClearAll();
            VoiceDissonancePlayerCache.Clear();
            VoiceMimicActorCache.Clear();
        }
    }
}
