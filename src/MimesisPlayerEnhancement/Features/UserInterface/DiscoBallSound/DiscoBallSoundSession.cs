namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundSession
    {
        private static string? _stickyVariant;
        private static int _scopeId;
        private static bool _warnedNoVariants;

        internal static string? ResolveVariantFileName()
        {
            DiscoBallSoundMode mode = DiscoBallSoundResolver.GetMode();
            if (mode == DiscoBallSoundMode.Vanilla)
            {
                return null;
            }

            if (mode == DiscoBallSoundMode.Specific)
            {
                return WarnIfMissing(DiscoBallSoundResolver.ResolveSpecificVariantFileName());
            }

            int scopeId = CurrentScopeId();
            if (scopeId != _scopeId)
            {
                _scopeId = scopeId;
                _stickyVariant = null;
            }

            if (string.IsNullOrWhiteSpace(_stickyVariant))
            {
                _stickyVariant = DiscoBallSoundResolver.ResolveRandomVariantFileName();
                if (!string.IsNullOrWhiteSpace(_stickyVariant))
                {
                    ModLog.Debug(
                        DiscoBallSoundConstants.Feature,
                        $"Disco ball track picked for this dungeon — {_stickyVariant}");
                }
            }

            return WarnIfMissing(_stickyVariant);
        }

        internal static void ClearStickyVariant()
        {
            _stickyVariant = null;
            _scopeId = 0;
            _warnedNoVariants = false;
        }

        private static string? WarnIfMissing(string? variant)
        {
            if (!string.IsNullOrWhiteSpace(variant))
            {
                return variant;
            }

            if (!_warnedNoVariants)
            {
                _warnedNoVariants = true;
                ModLog.Warn(
                    DiscoBallSoundConstants.Feature,
                    "Disco ball sound replacement skipped — no embedded variants");
            }

            return null;
        }

        private static int CurrentScopeId()
        {
            GameMainBase? main = GameSessionAccess.TryGetPdata()?.main as GameMainBase;
            return main != null ? main.GetInstanceID() : 0;
        }
    }
}
