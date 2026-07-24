using MimesisPlayerEnhancement.Util;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicVoiceTuning
{
    internal static class MimicVoiceTuningResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";
        private const float VanillaResponseCooldownSeconds = 3f;
        private const float VanillaResponseDelaySeconds = 0.2f;
        private const float VanillaResponseMaxDistance = 20f;
        private const float VanillaClipReuseCooldownSeconds = 60f;
        private const float VanillaDeathMatchClipReuseCooldownSeconds = 3f;
        private const float VanillaSpeakAudienceRangeMeters = 15f;
        private const float VanillaPostReplyIntervalMinSeconds = 2f;
        private const float VanillaPostReplyIntervalMaxSeconds = 4f;
        private const float VanillaPostReplyIntervalFixedSeconds = 3f;
        private const int VanillaMinRequiredSpeechClips = 3;
        private const float VanillaInitIntervalMinSeconds = 4f;
        private const float VanillaInitIntervalMaxSeconds = 7f;
        private const float VanillaPeriodicIntervalMinSeconds = 2f;
        private const float VanillaPeriodicIntervalMaxSeconds = 8f;
        private const float VanillaDeathMatchIntervalMinSeconds = 2f;
        private const float VanillaDeathMatchIntervalMaxSeconds = 8f;
        private const float MinCooldownSeconds = 0f;
        private const float MaxCooldownSeconds = 120f;
        private const float MinDelaySeconds = 0f;
        private const float MaxDelaySeconds = 30f;
        private const float MinDistanceMeters = 1f;
        private const float MaxDistanceMeters = 200f;
        private const float MinIntervalMultiplier = 0.05f;
        private const float MaxIntervalMultiplier = 10f;
        private const float MinClipReuseSeconds = 0f;
        private const float MaxClipReuseSeconds = 600f;
        private const float MinAudienceRangeMeters = 1f;
        private const float MaxAudienceRangeMeters = 200f;
        private const float MinIntervalSeconds = 0f;
        private const float MaxIntervalSeconds = 120f;
        private const int MinRequiredSpeechClipsMin = 0;
        private const int MinRequiredSpeechClipsMax = 50;

        private static bool _cachedMasterEnabled;
        private static MimicVoiceTuningMode _cachedMode = MimicVoiceTuningMode.Vanilla;
        private static float _cachedIntervalMultiplier = 1f;
        private static int _cachedResponseChancePercent = 100;
        private static float _cachedResponseCooldownSeconds = VanillaResponseCooldownSeconds;
        private static float _cachedResponseDelayMinSeconds = VanillaResponseDelaySeconds;
        private static float _cachedResponseDelayMaxSeconds = VanillaResponseDelaySeconds;
        private static float _cachedResponseMaxDistance = VanillaResponseMaxDistance;
        private static float _cachedClipReuseCooldownSeconds = VanillaClipReuseCooldownSeconds;
        private static float _cachedDeathMatchClipReuseCooldownSeconds = VanillaDeathMatchClipReuseCooldownSeconds;
        private static float _cachedSpeakAudienceRangeMeters = VanillaSpeakAudienceRangeMeters;
        private static MimicTuningPostReplyIntervalMode _cachedPostReplyIntervalMode =
            MimicTuningPostReplyIntervalMode.Vanilla;
        private static float _cachedPostReplyIntervalFixedSeconds = VanillaPostReplyIntervalFixedSeconds;
        private static float _cachedPostReplyIntervalMinSeconds = VanillaPostReplyIntervalMinSeconds;
        private static float _cachedPostReplyIntervalMaxSeconds = VanillaPostReplyIntervalMaxSeconds;
        private static int _cachedMinRequiredSpeechClips = VanillaMinRequiredSpeechClips;
        private static HearOwnVoiceFromMimicMode _cachedHearOwnVoiceMode = HearOwnVoiceFromMimicMode.Vanilla;
        private static MimicTuningIntervalMode _cachedInitIntervalMode = MimicTuningIntervalMode.Vanilla;
        private static float _cachedInitIntervalMinSeconds = VanillaInitIntervalMinSeconds;
        private static float _cachedInitIntervalMaxSeconds = VanillaInitIntervalMaxSeconds;
        private static MimicTuningIntervalMode _cachedPeriodicIntervalMode = MimicTuningIntervalMode.Vanilla;
        private static float _cachedPeriodicIntervalMinSeconds = VanillaPeriodicIntervalMinSeconds;
        private static float _cachedPeriodicIntervalMaxSeconds = VanillaPeriodicIntervalMaxSeconds;
        private static MimicTuningIntervalMode _cachedDeathMatchIntervalMode = MimicTuningIntervalMode.Vanilla;
        private static float _cachedDeathMatchIntervalMinSeconds = VanillaDeathMatchIntervalMinSeconds;
        private static float _cachedDeathMatchIntervalMaxSeconds = VanillaDeathMatchIntervalMaxSeconds;

        internal static bool IsMasterEnabled => _cachedMasterEnabled;

        internal static MimicVoiceTuningMode Mode => _cachedMode;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedMode == MimicVoiceTuningMode.Custom;

        internal static float GetResponseCooldownSeconds() => _cachedResponseCooldownSeconds;

        internal static float GetResponseMaxDistance() =>
            GetResponseMaxDistance(ShouldApplyCustom, _cachedResponseMaxDistance);

        internal static float GetResponseMaxDistance(bool shouldApplyCustom, float configuredDistance) =>
            shouldApplyCustom ? configuredDistance : VanillaResponseMaxDistance;

        internal static float GetClipReuseCooldownSeconds() =>
            ShouldApplyCustom ? _cachedClipReuseCooldownSeconds : VanillaClipReuseCooldownSeconds;

        internal static float GetDeathMatchClipReuseCooldownSeconds() =>
            ShouldApplyCustom
                ? _cachedDeathMatchClipReuseCooldownSeconds
                : VanillaDeathMatchClipReuseCooldownSeconds;

        internal static float GetSpeakAudienceRangeMeters() =>
            ShouldApplyCustom ? _cachedSpeakAudienceRangeMeters : VanillaSpeakAudienceRangeMeters;

        internal static int GetMinRequiredSpeechClips() =>
            ShouldApplyCustom ? _cachedMinRequiredSpeechClips : VanillaMinRequiredSpeechClips;

        internal static bool ResolveMuteLocalPlayerVoice(bool tableMuteLocalPlayerVoice)
        {
            if (!ShouldApplyCustom)
            {
                return tableMuteLocalPlayerVoice;
            }

            return _cachedHearOwnVoiceMode switch
            {
                HearOwnVoiceFromMimicMode.AlwaysHear => false,
                HearOwnVoiceFromMimicMode.OnlyWhenSingleplayer =>
                    !IsSingleplayerLobby() && tableMuteLocalPlayerVoice,
                _ => tableMuteLocalPlayerVoice,
            };
        }

        internal static bool TryResolveInitIntervalSeconds(out float seconds)
        {
            seconds = 0f;
            if (!ShouldApplyCustom || _cachedInitIntervalMode != MimicTuningIntervalMode.Random)
            {
                return false;
            }

            seconds = MimicTuningModeHelpers.RollSeconds(
                _cachedInitIntervalMinSeconds,
                _cachedInitIntervalMaxSeconds);
            return true;
        }

        internal static bool TryResolvePeriodicIntervalSeconds(out float seconds)
        {
            seconds = 0f;
            if (!ShouldApplyCustom || _cachedPeriodicIntervalMode != MimicTuningIntervalMode.Random)
            {
                return false;
            }

            seconds = MimicTuningModeHelpers.RollSeconds(
                _cachedPeriodicIntervalMinSeconds,
                _cachedPeriodicIntervalMaxSeconds);
            return true;
        }

        internal static bool TryResolveDeathMatchIntervalSeconds(out float seconds)
        {
            seconds = 0f;
            if (!ShouldApplyCustom || _cachedDeathMatchIntervalMode != MimicTuningIntervalMode.Random)
            {
                return false;
            }

            seconds = MimicTuningModeHelpers.RollSeconds(
                _cachedDeathMatchIntervalMinSeconds,
                _cachedDeathMatchIntervalMaxSeconds);
            return true;
        }

        internal static bool TryResolvePostReplyIntervalSeconds(out float seconds)
        {
            seconds = 0f;
            if (!ShouldApplyCustom)
            {
                return false;
            }

            switch (_cachedPostReplyIntervalMode)
            {
                case MimicTuningPostReplyIntervalMode.Fixed:
                    seconds = _cachedPostReplyIntervalFixedSeconds;
                    return true;
                case MimicTuningPostReplyIntervalMode.Random:
                    seconds = MimicTuningModeHelpers.RollSeconds(
                        _cachedPostReplyIntervalMinSeconds,
                        _cachedPostReplyIntervalMaxSeconds);
                    return true;
                default:
                    return false;
            }
        }

        internal static float ScalePeriodicIntervalSeconds(float vanillaSeconds) =>
            ScaleIntervalSeconds(vanillaSeconds, ShouldApplyCustom, _cachedIntervalMultiplier);

        internal static float ScaleIntervalSeconds(float vanillaSeconds) =>
            ScaleIntervalSeconds(vanillaSeconds, ShouldApplyCustom, _cachedIntervalMultiplier);

        internal static float ScaleIntervalSeconds(
            float vanillaSeconds,
            bool shouldApplyCustom,
            float intervalMultiplier)
        {
            if (!shouldApplyCustom || vanillaSeconds <= 0f)
            {
                return vanillaSeconds;
            }

            return Mathf.Max(0f, vanillaSeconds * intervalMultiplier);
        }

        internal static float RollResponseDelaySeconds() =>
            RollResponseDelaySeconds(
                ShouldApplyCustom,
                _cachedResponseDelayMinSeconds,
                _cachedResponseDelayMaxSeconds);

        internal static float RollResponseDelaySeconds(bool shouldApplyCustom, float minSeconds, float maxSeconds)
        {
            if (!shouldApplyCustom)
            {
                return VanillaResponseDelaySeconds;
            }

            return minSeconds >= maxSeconds ? minSeconds : UnityEngine.Random.Range(minSeconds, maxSeconds);
        }

        internal static bool RollResponseChance() =>
            RollResponseChance(ShouldApplyCustom, _cachedResponseChancePercent);

        internal static bool RollResponseChance(bool shouldApplyCustom, int chancePercent)
        {
            if (!shouldApplyCustom)
            {
                return true;
            }

            int chance = Mathf.Clamp(chancePercent, 0, 100);
            if (chance >= 100)
            {
                return true;
            }

            if (chance <= 0)
            {
                return false;
            }

            return UnityEngine.Random.Range(0, 100) < chance;
        }

        internal static MimicVoiceTuningMode ParseMode(string? value)
        {
            if (string.Equals(value, nameof(MimicVoiceTuningMode.Custom), StringComparison.OrdinalIgnoreCase))
            {
                return MimicVoiceTuningMode.Custom;
            }

            return MimicVoiceTuningMode.Vanilla;
        }

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null
                || ModConfig.MimicVoiceTuningMode == null
                || ModConfig.PeriodicVoiceIntervalMultiplier == null
                || ModConfig.PlayerVoiceResponseChancePercent == null
                || ModConfig.PlayerVoiceResponseCooldownSeconds == null
                || ModConfig.PlayerVoiceResponseDelayMinSeconds == null
                || ModConfig.PlayerVoiceResponseDelayMaxSeconds == null
                || ModConfig.PlayerVoiceResponseMaxDistance == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedMode = ParseMode(ModConfig.MimicVoiceTuningMode.Value);
            _cachedIntervalMultiplier = Mathf.Clamp(
                ModConfig.PeriodicVoiceIntervalMultiplier.Value,
                MinIntervalMultiplier,
                MaxIntervalMultiplier);
            _cachedResponseChancePercent = Mathf.Clamp(ModConfig.PlayerVoiceResponseChancePercent.Value, 0, 100);
            _cachedResponseCooldownSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseCooldownSeconds.Value,
                MinCooldownSeconds,
                MaxCooldownSeconds);
            _cachedResponseDelayMinSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseDelayMinSeconds.Value,
                MinDelaySeconds,
                MaxDelaySeconds);
            _cachedResponseDelayMaxSeconds = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseDelayMaxSeconds.Value,
                MinDelaySeconds,
                MaxDelaySeconds);
            if (_cachedResponseDelayMaxSeconds < _cachedResponseDelayMinSeconds)
            {
                (_cachedResponseDelayMinSeconds, _cachedResponseDelayMaxSeconds) =
                    (_cachedResponseDelayMaxSeconds, _cachedResponseDelayMinSeconds);
            }

            _cachedResponseMaxDistance = Mathf.Clamp(
                ModConfig.PlayerVoiceResponseMaxDistance.Value,
                MinDistanceMeters,
                MaxDistanceMeters);
            _cachedClipReuseCooldownSeconds = Mathf.Clamp(
                ModConfig.ClipReuseCooldownSeconds?.Value ?? VanillaClipReuseCooldownSeconds,
                MinClipReuseSeconds,
                MaxClipReuseSeconds);
            _cachedDeathMatchClipReuseCooldownSeconds = Mathf.Clamp(
                ModConfig.DeathMatchClipReuseCooldownSeconds?.Value ?? VanillaDeathMatchClipReuseCooldownSeconds,
                MinClipReuseSeconds,
                MaxClipReuseSeconds);
            _cachedSpeakAudienceRangeMeters = Mathf.Clamp(
                ModConfig.SpeakAudienceRangeMeters?.Value ?? VanillaSpeakAudienceRangeMeters,
                MinAudienceRangeMeters,
                MaxAudienceRangeMeters);
            _cachedPostReplyIntervalMode = MimicTuningModeHelpers.ParsePostReplyIntervalMode(
                ModConfig.PostReplyIntervalMode?.Value);
            _cachedPostReplyIntervalFixedSeconds = Mathf.Clamp(
                ModConfig.PostReplyIntervalFixedSeconds?.Value ?? VanillaPostReplyIntervalFixedSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedPostReplyIntervalMinSeconds = Mathf.Clamp(
                ModConfig.PostReplyIntervalMinSeconds?.Value ?? VanillaPostReplyIntervalMinSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedPostReplyIntervalMaxSeconds = Mathf.Clamp(
                ModConfig.PostReplyIntervalMaxSeconds?.Value ?? VanillaPostReplyIntervalMaxSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            if (_cachedPostReplyIntervalMaxSeconds < _cachedPostReplyIntervalMinSeconds)
            {
                (_cachedPostReplyIntervalMinSeconds, _cachedPostReplyIntervalMaxSeconds) =
                    (_cachedPostReplyIntervalMaxSeconds, _cachedPostReplyIntervalMinSeconds);
            }

            _cachedMinRequiredSpeechClips = Mathf.Clamp(
                ModConfig.MinRequiredSpeechClips?.Value ?? VanillaMinRequiredSpeechClips,
                MinRequiredSpeechClipsMin,
                MinRequiredSpeechClipsMax);
            _cachedHearOwnVoiceMode = MimicTuningModeHelpers.ParseHearOwnVoiceMode(
                ModConfig.HearOwnVoiceFromMimic?.Value);
            _cachedInitIntervalMode = MimicTuningModeHelpers.ParseIntervalMode(ModConfig.VoiceInitIntervalMode?.Value);
            _cachedInitIntervalMinSeconds = Mathf.Clamp(
                ModConfig.VoiceInitIntervalMin?.Value ?? VanillaInitIntervalMinSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedInitIntervalMaxSeconds = Mathf.Clamp(
                ModConfig.VoiceInitIntervalMax?.Value ?? VanillaInitIntervalMaxSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedPeriodicIntervalMode = MimicTuningModeHelpers.ParseIntervalMode(
                ModConfig.VoicePeriodicIntervalMode?.Value);
            _cachedPeriodicIntervalMinSeconds = Mathf.Clamp(
                ModConfig.VoicePeriodicIntervalMin?.Value ?? VanillaPeriodicIntervalMinSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedPeriodicIntervalMaxSeconds = Mathf.Clamp(
                ModConfig.VoicePeriodicIntervalMax?.Value ?? VanillaPeriodicIntervalMaxSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedDeathMatchIntervalMode = MimicTuningModeHelpers.ParseIntervalMode(
                ModConfig.VoiceDeathMatchIntervalMode?.Value);
            _cachedDeathMatchIntervalMinSeconds = Mathf.Clamp(
                ModConfig.VoiceDeathMatchIntervalMin?.Value ?? VanillaDeathMatchIntervalMinSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            _cachedDeathMatchIntervalMaxSeconds = Mathf.Clamp(
                ModConfig.VoiceDeathMatchIntervalMax?.Value ?? VanillaDeathMatchIntervalMaxSeconds,
                MinIntervalSeconds,
                MaxIntervalSeconds);
            NormalizeIntervalRange(
                ref _cachedInitIntervalMinSeconds,
                ref _cachedInitIntervalMaxSeconds);
            NormalizeIntervalRange(
                ref _cachedPeriodicIntervalMinSeconds,
                ref _cachedPeriodicIntervalMaxSeconds);
            NormalizeIntervalRange(
                ref _cachedDeathMatchIntervalMinSeconds,
                ref _cachedDeathMatchIntervalMaxSeconds);
        }

        private static void NormalizeIntervalRange(ref float minSeconds, ref float maxSeconds)
        {
            if (maxSeconds < minSeconds)
            {
                (minSeconds, maxSeconds) = (maxSeconds, minSeconds);
            }
        }

        private static bool IsSingleplayerLobby()
        {
            return SessionPlayerCountHelper.TryResolveExactFromSession(out int count) && count <= 1;
        }
    }
}
