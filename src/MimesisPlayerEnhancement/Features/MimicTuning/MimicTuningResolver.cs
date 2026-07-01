using System;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningResolver
    {
        // Const.json POSSESSION_DURATION (12000 ms) -> C_PossessionDuration.
        internal const float VanillaPossessionDurationSeconds = 12f;
        internal const float MinDurationSeconds = 0.1f;
        internal const float MaxDurationSeconds = 120f;
        internal const float MinCooltimeMultiplier = 0.1f;
        internal const float MaxCooltimeMultiplier = 10f;
        private const float VanillaEpsilon = 0.001f;

        private static bool _cachedEnable;
        private static bool _cachedRandomizeDuration;
        private static float _cachedCooltimeMultiplier;

        static MimicTuningResolver()
        {
            ModConfig.Changed += OnConfigChanged;
            RefreshConfigCache();
        }

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

            MimicTuningPossessionSessions.SetSessionDurationMs(mimicActorId, rolled);
            return rolled;
        }

        internal static long ScalePossessionCooltimeMs(long vanillaMs)
        {
            if (!_cachedEnable || !ShouldScaleCooltime || vanillaMs <= 0)
            {
                return vanillaMs;
            }

            return Math.Max(0L, (long)(vanillaMs * _cachedCooltimeMultiplier));
        }

        internal static float GetProgressBarTotalSeconds(int mimicActorId, float serverLeftTimeMs)
        {
            float vanillaSeconds = GetVanillaPossessionDurationSeconds();
            if (!_cachedEnable || !_cachedRandomizeDuration)
            {
                return vanillaSeconds;
            }

            if (MimicTuningPossessionSessions.TryGetSessionDurationMs(mimicActorId, out long sessionMs))
            {
                return sessionMs * 0.001f;
            }

            if (serverLeftTimeMs > 0f)
            {
                MimicTuningPossessionSessions.SetSessionDurationMs(mimicActorId, (long)serverLeftTimeMs);
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

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload
                || change.AffectsSection("MimesisPlayerEnhancement_MimicTuning"))
            {
                RefreshConfigCache();
            }
        }

        private static void RefreshConfigCache()
        {
            _cachedEnable = ModConfig.EnableMimicTuning.Value;
            _cachedRandomizeDuration = ModConfig.RandomizeMimicPossessionDuration.Value;
            _cachedCooltimeMultiplier = ModConfig.MimicPossessionCooltimeMultiplier.Value;
        }
    }
}
