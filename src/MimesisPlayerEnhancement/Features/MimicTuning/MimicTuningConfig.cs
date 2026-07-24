using MelonLoader;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicEmoteProps;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicHorn;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicSocial;
using MimesisPlayerEnhancement.Features.MimicTuning.MimicTrust;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningConfig
    {
        private const string Feature = "MimicTuning";

        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMimicTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicTuning",
                false);

            ModConfig.MimicVoiceTuningMode = ModConfig.CreateTrackedEntry(_category,
                "MimicVoiceTuningMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.PeriodicVoiceIntervalMultiplier = ModConfig.CreateTrackedEntry(_category,
                "PeriodicVoiceIntervalMultiplier",
                1f);

            ModConfig.PlayerVoiceResponseChancePercent = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseChancePercent",
                100);

            ModConfig.PlayerVoiceResponseCooldownSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseCooldownSeconds",
                3f);

            ModConfig.PlayerVoiceResponseDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseDelayMinSeconds",
                0.2f);

            ModConfig.PlayerVoiceResponseDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseDelayMaxSeconds",
                0.2f);

            ModConfig.PlayerVoiceResponseMaxDistance = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseMaxDistance",
                20f);

            ModConfig.ClipReuseCooldownSeconds = ModConfig.CreateTrackedEntry(_category,
                "ClipReuseCooldownSeconds",
                60f);

            ModConfig.DeathMatchClipReuseCooldownSeconds = ModConfig.CreateTrackedEntry(_category,
                "DeathMatchClipReuseCooldownSeconds",
                3f);

            ModConfig.SpeakAudienceRangeMeters = ModConfig.CreateTrackedEntry(_category,
                "SpeakAudienceRangeMeters",
                15f);

            ModConfig.PostReplyIntervalMode = ModConfig.CreateTrackedEntry(_category,
                "PostReplyIntervalMode",
                nameof(MimicTuningPostReplyIntervalMode.Vanilla));

            ModConfig.PostReplyIntervalFixedSeconds = ModConfig.CreateTrackedEntry(_category,
                "PostReplyIntervalFixedSeconds",
                3f);

            ModConfig.PostReplyIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "PostReplyIntervalMinSeconds",
                2f);

            ModConfig.PostReplyIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "PostReplyIntervalMaxSeconds",
                4f);

            ModConfig.MinRequiredSpeechClips = ModConfig.CreateTrackedEntry(_category,
                "MinRequiredSpeechClips",
                3);

            ModConfig.HearOwnVoiceFromMimic = ModConfig.CreateTrackedEntry(_category,
                "HearOwnVoiceFromMimic",
                nameof(HearOwnVoiceFromMimicMode.Vanilla));

            ModConfig.VoiceInitIntervalMode = ModConfig.CreateTrackedEntry(_category,
                "VoiceInitIntervalMode",
                nameof(MimicTuningIntervalMode.Vanilla));

            ModConfig.VoiceInitIntervalMin = ModConfig.CreateTrackedEntry(_category,
                "VoiceInitIntervalMin",
                4f);

            ModConfig.VoiceInitIntervalMax = ModConfig.CreateTrackedEntry(_category,
                "VoiceInitIntervalMax",
                7f);

            ModConfig.VoicePeriodicIntervalMode = ModConfig.CreateTrackedEntry(_category,
                "VoicePeriodicIntervalMode",
                nameof(MimicTuningIntervalMode.Vanilla));

            ModConfig.VoicePeriodicIntervalMin = ModConfig.CreateTrackedEntry(_category,
                "VoicePeriodicIntervalMin",
                2f);

            ModConfig.VoicePeriodicIntervalMax = ModConfig.CreateTrackedEntry(_category,
                "VoicePeriodicIntervalMax",
                8f);

            ModConfig.VoiceDeathMatchIntervalMode = ModConfig.CreateTrackedEntry(_category,
                "VoiceDeathMatchIntervalMode",
                nameof(MimicTuningIntervalMode.Vanilla));

            ModConfig.VoiceDeathMatchIntervalMin = ModConfig.CreateTrackedEntry(_category,
                "VoiceDeathMatchIntervalMin",
                2f);

            ModConfig.VoiceDeathMatchIntervalMax = ModConfig.CreateTrackedEntry(_category,
                "VoiceDeathMatchIntervalMax",
                8f);

            ModConfig.MimicTrustMode = ModConfig.CreateTrackedEntry(_category,
                "MimicTrustMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.TrustOutdoorMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrustOutdoorMultiplier",
                MimicTrustResolver.VanillaOutdoorMultiplier);

            ModConfig.TrustLookingDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustLookingDelta",
                MimicTrustResolver.VanillaLookingDelta);

            ModConfig.TrustNotLookingDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustNotLookingDelta",
                MimicTrustResolver.VanillaNotLookingDelta);

            ModConfig.TrustApproachDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustApproachDelta",
                MimicTrustResolver.VanillaApproachDelta);

            ModConfig.TrustMaintainDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustMaintainDelta",
                MimicTrustResolver.VanillaMaintainDelta);

            ModConfig.TrustWalkAwayDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustWalkAwayDelta",
                MimicTrustResolver.VanillaWalkAwayDelta);

            ModConfig.TrustSprintAwayDelta = ModConfig.CreateTrackedEntry(_category,
                "TrustSprintAwayDelta",
                MimicTrustResolver.VanillaSprintAwayDelta);

            ModConfig.TrustHitDamageMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrustHitDamageMultiplier",
                MimicTrustResolver.VanillaHitDamageMultiplier);

            ModConfig.TrustFriendlyThreshold = ModConfig.CreateTrackedEntry(_category,
                "TrustFriendlyThreshold",
                MimicTrustResolver.VanillaFriendlyThreshold);

            ModConfig.TrustDistrustThreshold = ModConfig.CreateTrackedEntry(_category,
                "TrustDistrustThreshold",
                MimicTrustResolver.VanillaDistrustThreshold);

            ModConfig.TrustScoreValueMode = ModConfig.CreateTrackedEntry(_category,
                "TrustScoreValueMode",
                nameof(MimicTuningValueMode.Vanilla));

            ModConfig.TrustInitialFixed = ModConfig.CreateTrackedEntry(_category,
                "TrustInitialFixed",
                MimicTrustResolver.VanillaInitialTrust);

            ModConfig.TrustInitialRandomMin = ModConfig.CreateTrackedEntry(_category,
                "TrustInitialRandomMin",
                MimicTrustResolver.VanillaInitialTrust);

            ModConfig.TrustInitialRandomMax = ModConfig.CreateTrackedEntry(_category,
                "TrustInitialRandomMax",
                MimicTrustResolver.VanillaInitialTrust);

            ModConfig.TrustBehaviorFixed = ModConfig.CreateTrackedEntry(_category,
                "TrustBehaviorFixed",
                MimicTrustResolver.VanillaBehaviorTrust);

            ModConfig.TrustBehaviorRandomMin = ModConfig.CreateTrackedEntry(_category,
                "TrustBehaviorRandomMin",
                MimicTrustResolver.VanillaBehaviorTrust);

            ModConfig.TrustBehaviorRandomMax = ModConfig.CreateTrackedEntry(_category,
                "TrustBehaviorRandomMax",
                MimicTrustResolver.VanillaBehaviorTrust);

            ModConfig.ChaseActivationDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "ChaseActivationDistanceMeters",
                MimicTrustResolver.VanillaChaseActivationDistance);

            ModConfig.ChaseForceRunDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "ChaseForceRunDistanceMeters",
                MimicTrustResolver.VanillaChaseForceRunDistance);

            ModConfig.MimicSocialMode = ModConfig.CreateTrackedEntry(_category,
                "MimicSocialMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.MimicRunawayChance = ModConfig.CreateTrackedEntry(_category,
                "MimicRunawayChance",
                MimicSocialResolver.VanillaRunawayChance);

            ModConfig.JumpCopyChancePercent = ModConfig.CreateTrackedEntry(_category,
                "JumpCopyChancePercent",
                (int)(MimicSocialResolver.VanillaJumpCopyChance * 100f));

            ModConfig.SlotFollowChangeChancePercent = ModConfig.CreateTrackedEntry(_category,
                "SlotFollowChangeChancePercent",
                (int)(MimicSocialResolver.VanillaSlotFollowChangeChance * 100f));

            ModConfig.MimicInventoryCopyMode = ModConfig.CreateTrackedEntry(_category,
                "MimicInventoryCopyMode",
                nameof(MimicInventoryCopyMode.Vanilla));

            ModConfig.MimicInventoryCopyPickRule = ModConfig.CreateTrackedEntry(_category,
                "MimicInventoryCopyPickRule",
                nameof(BTTargetPickRule.MinDistance));

            ModConfig.EnableMimicPossessionTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicPossessionTuning",
                false);

            ModConfig.RandomizeMimicPossessionDuration = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMimicPossessionDuration",
                false);

            ModConfig.MimicPossessionMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMinTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds);

            ModConfig.MimicPossessionMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMaxTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds);

            ModConfig.MimicPossessionCooltimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionCooltimeMultiplier",
                1f);

            ModConfig.PossessionRangeMeters = ModConfig.CreateTrackedEntry(_category,
                "PossessionRangeMeters",
                MimicPossessionResolver.VanillaPossessionRangeMeters);

            ModConfig.PossessionBtGateMode = ModConfig.CreateTrackedEntry(_category,
                "PossessionBtGateMode",
                nameof(PossessionBtGateMode.Vanilla));

            ModConfig.MimicEmotePropsMode = ModConfig.CreateTrackedEntry(_category,
                "MimicEmotePropsMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.EmoteRespondChancePercent = ModConfig.CreateTrackedEntry(_category,
                "EmoteRespondChancePercent",
                100);

            ModConfig.EmoteSuggestChancePercent = ModConfig.CreateTrackedEntry(_category,
                "EmoteSuggestChancePercent",
                30);

            ModConfig.ReactToSprinklerChancePercent = ModConfig.CreateTrackedEntry(_category,
                "ReactToSprinklerChancePercent",
                100);

            ModConfig.UseTrapSwitchChancePercent = ModConfig.CreateTrackedEntry(_category,
                "UseTrapSwitchChancePercent",
                100);

            ModConfig.UseChargerChancePercent = ModConfig.CreateTrackedEntry(_category,
                "UseChargerChancePercent",
                100);

            ModConfig.UseTransmitterChancePercent = ModConfig.CreateTrackedEntry(_category,
                "UseTransmitterChancePercent",
                100);

            ModConfig.UseShutterSwitchChancePercent = ModConfig.CreateTrackedEntry(_category,
                "UseShutterSwitchChancePercent",
                100);

            ModConfig.HornImitationMode = ModConfig.CreateTrackedEntry(_category,
                "HornImitationMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.AllowHornImitation = ModConfig.CreateTrackedEntry(_category,
                "AllowHornImitation",
                true);

            ModConfig.HornMaxRecordSeconds = ModConfig.CreateTrackedEntry(_category,
                "HornMaxRecordSeconds",
                MimicHornResolver.VanillaMaxRecordSeconds);

            ModConfig.HornRecordingGapSeconds = ModConfig.CreateTrackedEntry(_category,
                "HornRecordingGapSeconds",
                MimicHornResolver.VanillaRecordingGapSeconds);

            ModConfig.HornMaxStoredRecords = ModConfig.CreateTrackedEntry(_category,
                "HornMaxStoredRecords",
                MimicHornResolver.VanillaMaxStoredRecords);

            RefreshAllCaches();
        }

        internal static void WireValidation()
        {
            ModConfig.EnableMimicTuning.OnEntryValueChanged.Subscribe((_, _) =>
                NotifyAndRefresh(ModConfig.EnableMimicTuning));
            ModConfig.MimicVoiceTuningMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnVoiceModeChanged(value));
            ModConfig.PeriodicVoiceIntervalMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnIntervalMultiplierChanged(value));
            ModConfig.PlayerVoiceResponseChancePercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnChanceChanged(value));
            ModConfig.PlayerVoiceResponseCooldownSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnCooldownChanged(value));
            ModConfig.PlayerVoiceResponseDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnDelayRangeChanged(value, ModConfig.PlayerVoiceResponseDelayMinSeconds, ModConfig.PlayerVoiceResponseDelayMaxSeconds));
            ModConfig.PlayerVoiceResponseDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnDelayRangeChanged(value, ModConfig.PlayerVoiceResponseDelayMinSeconds, ModConfig.PlayerVoiceResponseDelayMaxSeconds));
            ModConfig.PlayerVoiceResponseMaxDistance.OnEntryValueChanged.Subscribe((_, value) =>
                OnDistanceChanged(value));
            ModConfig.MimicInventoryCopyMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnInventoryModeChanged(value));
            ModConfig.MimicInventoryCopyPickRule.OnEntryValueChanged.Subscribe((_, value) =>
                OnInventoryPickRuleChanged(value));
            ModConfig.EnableMimicPossessionTuning.OnEntryValueChanged.Subscribe((_, _) =>
                NotifyAndRefresh(ModConfig.EnableMimicPossessionTuning));
            ModConfig.RandomizeMimicPossessionDuration.OnEntryValueChanged.Subscribe((_, _) =>
                NotifyAndRefresh(ModConfig.RandomizeMimicPossessionDuration));
            ModConfig.MimicPossessionMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(value, ModConfig.MimicPossessionMinTimeSeconds));
            ModConfig.MimicPossessionMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(value, ModConfig.MimicPossessionMaxTimeSeconds));
            ModConfig.MimicPossessionCooltimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionCooltimeMultiplierChanged(value));

            WireNotify(ModConfig.ClipReuseCooldownSeconds);
            WireNotify(ModConfig.DeathMatchClipReuseCooldownSeconds);
            WireNotify(ModConfig.SpeakAudienceRangeMeters);
            WireNotify(ModConfig.PostReplyIntervalMode);
            WireNotify(ModConfig.PostReplyIntervalFixedSeconds);
            WireNotify(ModConfig.PostReplyIntervalMinSeconds);
            WireNotify(ModConfig.PostReplyIntervalMaxSeconds);
            WireNotify(ModConfig.MinRequiredSpeechClips);
            WireNotify(ModConfig.HearOwnVoiceFromMimic);
            WireNotify(ModConfig.VoiceInitIntervalMode);
            WireNotify(ModConfig.VoiceInitIntervalMin);
            WireNotify(ModConfig.VoiceInitIntervalMax);
            WireNotify(ModConfig.VoicePeriodicIntervalMode);
            WireNotify(ModConfig.VoicePeriodicIntervalMin);
            WireNotify(ModConfig.VoicePeriodicIntervalMax);
            WireNotify(ModConfig.VoiceDeathMatchIntervalMode);
            WireNotify(ModConfig.VoiceDeathMatchIntervalMin);
            WireNotify(ModConfig.VoiceDeathMatchIntervalMax);
            WireNotify(ModConfig.MimicTrustMode);
            WireNotify(ModConfig.TrustOutdoorMultiplier);
            WireNotify(ModConfig.TrustLookingDelta);
            WireNotify(ModConfig.TrustNotLookingDelta);
            WireNotify(ModConfig.TrustApproachDelta);
            WireNotify(ModConfig.TrustMaintainDelta);
            WireNotify(ModConfig.TrustWalkAwayDelta);
            WireNotify(ModConfig.TrustSprintAwayDelta);
            WireNotify(ModConfig.TrustHitDamageMultiplier);
            WireNotify(ModConfig.TrustFriendlyThreshold);
            WireNotify(ModConfig.TrustDistrustThreshold);
            WireNotify(ModConfig.TrustScoreValueMode);
            WireNotify(ModConfig.TrustInitialFixed);
            WireNotify(ModConfig.TrustInitialRandomMin);
            WireNotify(ModConfig.TrustInitialRandomMax);
            WireNotify(ModConfig.TrustBehaviorFixed);
            WireNotify(ModConfig.TrustBehaviorRandomMin);
            WireNotify(ModConfig.TrustBehaviorRandomMax);
            WireNotify(ModConfig.ChaseActivationDistanceMeters);
            WireNotify(ModConfig.ChaseForceRunDistanceMeters);
            WireNotify(ModConfig.MimicSocialMode);
            WireNotify(ModConfig.MimicRunawayChance);
            WireNotify(ModConfig.JumpCopyChancePercent);
            WireNotify(ModConfig.SlotFollowChangeChancePercent);
            WireNotify(ModConfig.PossessionRangeMeters);
            WireNotify(ModConfig.PossessionBtGateMode);
            WireNotify(ModConfig.MimicEmotePropsMode);
            WireNotify(ModConfig.EmoteRespondChancePercent);
            WireNotify(ModConfig.EmoteSuggestChancePercent);
            WireNotify(ModConfig.ReactToSprinklerChancePercent);
            WireNotify(ModConfig.UseTrapSwitchChancePercent);
            WireNotify(ModConfig.UseChargerChancePercent);
            WireNotify(ModConfig.UseTransmitterChancePercent);
            WireNotify(ModConfig.UseShutterSwitchChancePercent);
            WireNotify(ModConfig.HornImitationMode);
            WireNotify(ModConfig.AllowHornImitation);
            WireNotify(ModConfig.HornMaxRecordSeconds);
            WireNotify(ModConfig.HornRecordingGapSeconds);
            WireNotify(ModConfig.HornMaxStoredRecords);
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.PeriodicVoiceIntervalMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseCooldownSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseMaxDistance);
            ModConfig.TrackFloatEntry(ModConfig.ClipReuseCooldownSeconds);
            ModConfig.TrackFloatEntry(ModConfig.DeathMatchClipReuseCooldownSeconds);
            ModConfig.TrackFloatEntry(ModConfig.SpeakAudienceRangeMeters);
            ModConfig.TrackFloatEntry(ModConfig.PostReplyIntervalFixedSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PostReplyIntervalMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PostReplyIntervalMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.VoiceInitIntervalMin);
            ModConfig.TrackFloatEntry(ModConfig.VoiceInitIntervalMax);
            ModConfig.TrackFloatEntry(ModConfig.VoicePeriodicIntervalMin);
            ModConfig.TrackFloatEntry(ModConfig.VoicePeriodicIntervalMax);
            ModConfig.TrackFloatEntry(ModConfig.VoiceDeathMatchIntervalMin);
            ModConfig.TrackFloatEntry(ModConfig.VoiceDeathMatchIntervalMax);
            ModConfig.TrackFloatEntry(ModConfig.TrustOutdoorMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrustLookingDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustNotLookingDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustApproachDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustMaintainDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustWalkAwayDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustSprintAwayDelta);
            ModConfig.TrackFloatEntry(ModConfig.TrustHitDamageMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrustFriendlyThreshold);
            ModConfig.TrackFloatEntry(ModConfig.TrustDistrustThreshold);
            ModConfig.TrackFloatEntry(ModConfig.TrustInitialFixed);
            ModConfig.TrackFloatEntry(ModConfig.TrustInitialRandomMin);
            ModConfig.TrackFloatEntry(ModConfig.TrustInitialRandomMax);
            ModConfig.TrackFloatEntry(ModConfig.TrustBehaviorFixed);
            ModConfig.TrackFloatEntry(ModConfig.TrustBehaviorRandomMin);
            ModConfig.TrackFloatEntry(ModConfig.TrustBehaviorRandomMax);
            ModConfig.TrackFloatEntry(ModConfig.ChaseActivationDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.ChaseForceRunDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.MimicRunawayChance);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionCooltimeMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.PossessionRangeMeters);
            ModConfig.TrackFloatEntry(ModConfig.HornMaxRecordSeconds);
            ModConfig.TrackFloatEntry(ModConfig.HornRecordingGapSeconds);
        }

        private static void RefreshAllCaches()
        {
            MimicVoiceTuningResolver.RefreshConfigCache();
            MimicInventoryCopyResolver.RefreshConfigCache();
            MimicPossessionResolver.RefreshConfigCache();
            MimicTrustResolver.RefreshConfigCache();
            MimicSocialResolver.RefreshConfigCache();
            MimicEmotePropsResolver.RefreshConfigCache();
            MimicHornResolver.RefreshConfigCache();
        }


        private static void WireNotify<T>(MelonPreferences_Entry<T> entry)
        {
            entry.OnEntryValueChanged.Subscribe((_, _) => NotifyAndRefresh(entry));
        }

        private static void NotifyAndRefresh(MelonPreferences_Entry entry)
        {
            ModConfig.NotifyChanged(entry);
            RefreshAllCaches();
        }

        private static void NotifyAndRefresh<T>(MelonPreferences_Entry<T> entry)
        {
            ModConfig.NotifyChanged(entry);
            RefreshAllCaches();
        }

        private static void OnVoiceModeChanged(string value)
        {
            if (!IsValidVoiceMode(value))
            {
                ModLog.Warn(Feature, "MimicVoiceTuningMode must be Vanilla or Custom; resetting to Vanilla.");
                ModConfig.MimicVoiceTuningMode.Value = nameof(MimicVoiceTuningMode.Vanilla);
                return;
            }

            NotifyAndRefresh(ModConfig.MimicVoiceTuningMode);
        }

        private static void OnInventoryModeChanged(string value)
        {
            if (!IsValidInventoryMode(value))
            {
                ModLog.Warn(Feature, "MimicInventoryCopyMode must be Vanilla or Custom; resetting to Vanilla.");
                ModConfig.MimicInventoryCopyMode.Value = nameof(MimicInventoryCopyMode.Vanilla);
                return;
            }

            NotifyAndRefresh(ModConfig.MimicInventoryCopyMode);
        }

        private static void OnInventoryPickRuleChanged(string value)
        {
            if (!IsValidPickRule(value))
            {
                ModLog.Warn(Feature, "MimicInventoryCopyPickRule must be MinDistance, MaxDistance, or Random; resetting to MinDistance.");
                ModConfig.MimicInventoryCopyPickRule.Value = nameof(BTTargetPickRule.MinDistance);
                return;
            }

            NotifyAndRefresh(ModConfig.MimicInventoryCopyPickRule);
        }

        private static void OnIntervalMultiplierChanged(float value)
        {
            if (value < 0.05f)
            {
                ModLog.Warn(Feature, "PeriodicVoiceIntervalMultiplier must be >= 0.05; resetting to 0.05.");
                ModConfig.PeriodicVoiceIntervalMultiplier.Value = 0.05f;
                return;
            }

            NotifyAndRefresh(ModConfig.PeriodicVoiceIntervalMultiplier);
        }

        private static void OnChanceChanged(int value)
        {
            if (value is < 0 or > 100)
            {
                ModLog.Warn(Feature, "PlayerVoiceResponseChancePercent must be 0–100; clamping.");
                ModConfig.PlayerVoiceResponseChancePercent.Value = Math.Clamp(value, 0, 100);
                return;
            }

            NotifyAndRefresh(ModConfig.PlayerVoiceResponseChancePercent);
        }

        private static void OnCooldownChanged(float value)
        {
            if (value < 0f)
            {
                ModLog.Warn(Feature, "PlayerVoiceResponseCooldownSeconds must be >= 0; resetting to 0.");
                ModConfig.PlayerVoiceResponseCooldownSeconds.Value = 0f;
                return;
            }

            NotifyAndRefresh(ModConfig.PlayerVoiceResponseCooldownSeconds);
        }

        private static void OnDistanceChanged(float value)
        {
            if (value < 1f)
            {
                ModLog.Warn(Feature, "PlayerVoiceResponseMaxDistance must be >= 1; resetting to 1.");
                ModConfig.PlayerVoiceResponseMaxDistance.Value = 1f;
                return;
            }

            NotifyAndRefresh(ModConfig.PlayerVoiceResponseMaxDistance);
        }

        private static void OnDelayRangeChanged(
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            if (value < 0f)
            {
                ModLog.Warn(Feature, $"{minEntry.Identifier} must be >= 0; resetting to 0.");
                minEntry.Value = 0f;
                return;
            }

            if (maxEntry.Value < minEntry.Value)
            {
                maxEntry.Value = minEntry.Value;
            }

            NotifyAndRefresh(minEntry);
        }

        private static bool IsValidVoiceMode(string value) =>
            string.Equals(value, nameof(MimicVoiceTuningMode.Vanilla), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(MimicVoiceTuningMode.Custom), StringComparison.OrdinalIgnoreCase);

        private static bool IsValidInventoryMode(string value) =>
            string.Equals(value, nameof(MimicInventoryCopyMode.Vanilla), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(MimicInventoryCopyMode.Custom), StringComparison.OrdinalIgnoreCase);

        private static bool IsValidPickRule(string value) =>
            string.Equals(value, nameof(BTTargetPickRule.MinDistance), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(BTTargetPickRule.MaxDistance), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(BTTargetPickRule.Random), StringComparison.OrdinalIgnoreCase);

        private static void OnMimicPossessionDurationSecondsChanged(
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < MimicPossessionResolver.MinDurationSeconds)
            {
                ModLog.Warn(
                    Feature,
                    $"{entry.Identifier} must be at least {MimicPossessionResolver.MinDurationSeconds}; resetting.");
                entry.Value = MimicPossessionResolver.MinDurationSeconds;
                return;
            }

            if (value > MimicPossessionResolver.MaxDurationSeconds)
            {
                ModLog.Warn(
                    Feature,
                    $"{entry.Identifier} must be at most {MimicPossessionResolver.MaxDurationSeconds}; resetting.");
                entry.Value = MimicPossessionResolver.MaxDurationSeconds;
                return;
            }

            float min = ModConfig.MimicPossessionMinTimeSeconds.Value;
            float max = ModConfig.MimicPossessionMaxTimeSeconds.Value;
            if (max < min)
            {
                ModLog.Warn(
                    Feature,
                    "MimicPossessionMaxTimeSeconds must be >= MimicPossessionMinTimeSeconds; syncing max to min.");
                ModConfig.MimicPossessionMaxTimeSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            NotifyAndRefresh(entry);
        }

        private static void OnMimicPossessionCooltimeMultiplierChanged(float value)
        {
            if (value < MimicPossessionResolver.MinCooltimeMultiplier)
            {
                ModLog.Warn(
                    Feature,
                    $"MimicPossessionCooltimeMultiplier must be at least {MimicPossessionResolver.MinCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicPossessionResolver.MinCooltimeMultiplier;
                return;
            }

            if (value > MimicPossessionResolver.MaxCooltimeMultiplier)
            {
                ModLog.Warn(
                    Feature,
                    $"MimicPossessionCooltimeMultiplier must be at most {MimicPossessionResolver.MaxCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicPossessionResolver.MaxCooltimeMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.MimicPossessionCooltimeMultiplier);
            NotifyAndRefresh(ModConfig.MimicPossessionCooltimeMultiplier);
        }
    }
}
