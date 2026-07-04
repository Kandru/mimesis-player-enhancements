using System;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneResolver
    {
        internal const float DefaultLookAngleDegrees = 45f;
        internal const float DefaultMaxRingTimeSeconds = 15f;
        internal const float MinDurationSeconds = 0.1f;
        internal const float MaxDurationSeconds = 120f;
        internal const float MinDistanceMeters = 1f;
        internal const float MaxAllowedDistanceMeters = 100f;

        private static bool _cachedMasterEnable;
        private static bool _cachedPhoneEnable;
        private static bool _cachedRandomizeTalk;
        private static float _cachedMaxDistance;
        private static float _cachedMaxAngle;
        private static float _cachedMaxRingTime;
        private static float _cachedTalkMin;
        private static float _cachedTalkMax;
        private static float _cachedCooldown;

        static DeadPlayerPhoneResolver()
        {
            ModConfig.Changed += OnConfigChanged;
        }

        internal static bool IsPhoneRingEnabled => _cachedMasterEnable && _cachedPhoneEnable;

        internal static bool ShouldApplyHost =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => IsPhoneRingEnabled);

        internal static float MaxDistanceMeters => _cachedMaxDistance;

        internal static float MaxLookAngleDegrees => _cachedMaxAngle;

        internal static float MaxRingTimeSeconds => _cachedMaxRingTime;

        internal static float CooldownSeconds => _cachedCooldown;

        internal static float GetVanillaPossessionDistanceMeters()
        {
            Bifrost.ConstEnum.DataConsts? consts = HubGameDataAccess.Excel?.Consts;
            if (consts != null && consts.C_PossessionDistance > 0)
            {
                return consts.C_PossessionDistance;
            }

            return 10f;
        }

        internal static float RollTalkDurationSeconds()
        {
            if (!_cachedRandomizeTalk)
            {
                return _cachedTalkMax;
            }

            float min = Math.Min(_cachedTalkMin, _cachedTalkMax);
            float max = Math.Max(_cachedTalkMin, _cachedTalkMax);
            return min >= max ? min : UnityEngine.Random.Range(min, max);
        }

        internal static bool IsModRingRequest(int state, bool occupy) =>
            state == (int)PhoneState.Ringing && occupy;

        /// <summary>
        /// Shape of the mod's dead-spectator end request. Reuses PhoneState values that also appear on
        /// other level objects (Idle=0, Busy=4) — pair with phone-type and dead-player checks before intercepting.
        /// </summary>
        internal static bool IsModEndRequest(int state, bool occupy) =>
            !occupy
            && (state == (int)PhoneState.Idle || state == (int)PhoneState.Busy);

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload
                || change.AffectsSection("MimesisPlayerEnhancement_DeadPlayerFeatures"))
            {
                RefreshConfigCache();
            }
        }

        private static void RefreshConfigCache()
        {
            if (ModConfig.EnableDeadPlayerFeatures == null
                || ModConfig.EnableDeadPlayerPhoneRing == null
                || ModConfig.RandomizeDeadPlayerPhoneTalkTime == null
                || ModConfig.DeadPlayerPhoneMaxDistanceMeters == null
                || ModConfig.DeadPlayerPhoneMaxLookAngleDegrees == null
                || ModConfig.DeadPlayerPhoneMaxRingTimeSeconds == null
                || ModConfig.DeadPlayerPhoneTalkMinTimeSeconds == null
                || ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds == null
                || ModConfig.DeadPlayerPhoneCooldownSeconds == null)
            {
                return;
            }

            _cachedMasterEnable = ModConfig.EnableDeadPlayerFeatures.Value;
            _cachedPhoneEnable = ModConfig.EnableDeadPlayerPhoneRing.Value;
            _cachedRandomizeTalk = ModConfig.RandomizeDeadPlayerPhoneTalkTime.Value;
            _cachedMaxDistance = ModConfig.DeadPlayerPhoneMaxDistanceMeters.Value;
            _cachedMaxAngle = ModConfig.DeadPlayerPhoneMaxLookAngleDegrees.Value;
            _cachedMaxRingTime = ModConfig.DeadPlayerPhoneMaxRingTimeSeconds.Value;
            _cachedTalkMin = ModConfig.DeadPlayerPhoneTalkMinTimeSeconds.Value;
            _cachedTalkMax = ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds.Value;
            _cachedCooldown = ModConfig.DeadPlayerPhoneCooldownSeconds.Value;
        }

        internal static void RefreshFromDungeonLifecycle() => RefreshConfigCache();

        internal static void RefreshFromConfigRegistration() => RefreshConfigCache();
    }
}
