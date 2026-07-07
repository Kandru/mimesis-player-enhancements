namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicPossession
{
    internal static class MimicPossessionResolver
    {
        private const string Feature = "MimicTuning";

        internal const float VanillaPossessionDurationSeconds = 12f;
        internal const float MinDurationSeconds = 0.1f;
        internal const float MaxDurationSeconds = 120f;
        internal const float MinCooltimeMultiplier = 0.1f;
        internal const float MaxCooltimeMultiplier = 10f;
        private const float VanillaEpsilon = 0.001f;

        private static bool _cachedEnable;
        private static bool _cachedRandomizeDuration;
        private static float _cachedCooltimeMultiplier;

        internal static bool IsEnabled => _cachedEnable;

        internal static bool ShouldApplyHost =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedEnable);

        internal static bool ShouldRandomizeDuration =>
            ShouldApplyHost && _cachedRandomizeDuration;

        internal static bool ShouldScaleCooltime =>
            ShouldApplyHost && !IsVanillaMultiplier(_cachedCooltimeMultiplier);

        internal static bool IsVanillaMultiplier(float multiplier) =>
            Math.Abs(multiplier - 1f) < VanillaEpsilon;

        internal static float GetVanillaPossessionDurationSeconds()
        {
            long ms = GetVanillaPossessionDurationMs();
            return ms > 0 ? ms * 0.001f : VanillaPossessionDurationSeconds;
        }

        internal static long GetVanillaPossessionDurationMs() =>
            TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
                ? consts.C_PossessionDuration
                : 0L;

        internal static long GetVanillaPossessionCooltimeMs() =>
            TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
                ? consts.C_PossessionCooltime
                : 0L;

        private static bool TryGetConsts(out Bifrost.ConstEnum.DataConsts consts)
        {
            consts = HubGameDataAccess.Excel?.Consts!;
            return consts != null;
        }

        internal static long RollPossessionDurationMs(long vanillaMs, int mimicActorId)
        {
            if (!_cachedEnable || !ShouldRandomizeDuration || vanillaMs <= 0)
            {
                return vanillaMs;
            }

            float minSeconds = ModConfig.MimicPossessionMinTimeSeconds.Value;
            float maxSeconds = ModConfig.MimicPossessionMaxTimeSeconds.Value;
            long minMs = Math.Max(1L, (long)(minSeconds * 1000f));
            long maxMs = Math.Max(minMs, (long)(maxSeconds * 1000f));

            long rolled = minMs >= maxMs
                ? minMs
                : UnityEngine.Random.Range((int)minMs, (int)maxMs + 1);

            MimicPossessionSessions.SetSessionDurationMs(mimicActorId, rolled);
            MimicPossessionLog.DebugPossessionDurationRolled(mimicActorId, vanillaMs, rolled);
            return rolled;
        }

        internal static long ScalePossessionCooltimeMs(long vanillaMs)
        {
            if (!_cachedEnable || !ShouldScaleCooltime || vanillaMs <= 0)
            {
                return vanillaMs;
            }

            long scaled = Math.Max(0L, (long)(vanillaMs * _cachedCooltimeMultiplier));
            MimicPossessionLog.DebugCooltimeScaled(vanillaMs, scaled, _cachedCooltimeMultiplier);
            return scaled;
        }

        internal static float GetProgressBarTotalSeconds(int mimicActorId, float serverLeftTimeMs)
        {
            float vanillaSeconds = GetVanillaPossessionDurationSeconds();
            if (!_cachedEnable || !_cachedRandomizeDuration)
            {
                return vanillaSeconds;
            }

            if (MimicPossessionSessions.TryGetSessionDurationMs(mimicActorId, out long sessionMs))
            {
                return sessionMs * 0.001f;
            }

            if (serverLeftTimeMs > 0f)
            {
                MimicPossessionSessions.SetSessionDurationMs(mimicActorId, (long)serverLeftTimeMs);
                return serverLeftTimeMs * 0.001f;
            }

            return vanillaSeconds;
        }

        internal static float GetCooltimeTotalSeconds()
        {
            long vanillaMs = GetVanillaPossessionCooltimeMs();
            if (vanillaMs <= 0)
            {
                return 0f;
            }

            if (!_cachedEnable || !ShouldScaleCooltime)
            {
                return vanillaMs * 0.001f;
            }

            return ScalePossessionCooltimeMs(vanillaMs) * 0.001f;
        }

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null
                || ModConfig.EnableMimicPossessionTuning == null
                || ModConfig.RandomizeMimicPossessionDuration == null
                || ModConfig.MimicPossessionCooltimeMultiplier == null)
            {
                return;
            }

            _cachedEnable = ModConfig.EnableMimicTuning.Value
                && ModConfig.EnableMimicPossessionTuning.Value;
            _cachedRandomizeDuration = ModConfig.RandomizeMimicPossessionDuration.Value;
            _cachedCooltimeMultiplier = ModConfig.MimicPossessionCooltimeMultiplier.Value;
        }
    }
}
