namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundGate
    {
        /// <summary>
        /// Harmony prefix return: true = run vanilla, false = skip vanilla (replacement played).
        /// </summary>
        internal static bool PrefixAllowVanilla(string? sfxId)
        {
            if (!ShouldReplaceSfx(sfxId))
            {
                return true;
            }

            return !RoundStartSoundPlayer.TryPlayReplacement();
        }

        internal static bool ShouldReplaceSfx(string? sfxId)
        {
            if (!RoundStartSoundResolver.ShouldApplyReplacement()
                || !MatchesLandingMelodySfxId(sfxId)
                || !IsDungeonLandingContext())
            {
                return false;
            }

            return true;
        }

        private static bool IsDungeonLandingContext()
        {
            return GameSessionAccess.TryGetPdata()?.main is GamePlayScene
                   && DungeonLandingEntryTracker.IsActive;
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
    }
}
