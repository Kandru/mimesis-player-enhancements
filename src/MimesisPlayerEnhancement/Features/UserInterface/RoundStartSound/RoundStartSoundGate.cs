namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundGate
    {
        internal static bool ShouldReplaceSfx(string? sfxId)
        {
            if (!RoundStartSoundResolver.ShouldApplyReplacement()
                || !MatchesLandingMelodySfxId(sfxId)
                || !IsDungeonLandingContext())
            {
                LogRejection(sfxId, "sfx or context");
                return false;
            }

            return true;
        }

        private static bool IsDungeonLandingContext()
        {
            if (GameSessionAccess.TryGetPdata()?.main is not GamePlayScene)
            {
                LogRejection(null, "scene");
                return false;
            }

            if (!DungeonLandingEntryTracker.IsActive)
            {
                LogRejection(null, "entry window");
                return false;
            }

            return true;
        }

        private static bool MatchesLandingMelodySfxId(string? sfxId)
        {
            if (string.IsNullOrWhiteSpace(sfxId))
            {
                return false;
            }

            string normalized = sfxId.Trim();
            return string.Equals(normalized, RoundStartSoundConstants.LandingMelodySfxId, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(normalized, RoundStartSoundConstants.LandingMelodySfxIdAlt, StringComparison.OrdinalIgnoreCase);
        }

        private static void LogRejection(string? sfxId, string reason)
        {
            if (!ModConfig.EnableDebugLogging.Value || string.IsNullOrWhiteSpace(sfxId))
            {
                return;
            }

            if (!MatchesLandingMelodySfxId(sfxId))
            {
                return;
            }

            ModLog.Debug(RoundStartSoundConstants.Feature, $"Dungeon landing sound skipped — {reason}");
        }
    }
}
