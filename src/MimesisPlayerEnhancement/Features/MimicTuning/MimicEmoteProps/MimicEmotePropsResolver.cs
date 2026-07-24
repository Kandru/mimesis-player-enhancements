using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicEmoteProps
{
    internal static class MimicEmotePropsResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        internal const float VanillaEmoteRespondChance = 1f;
        internal const float VanillaEmoteSuggestChance = 0.3f;
        internal const float VanillaReactToSprinklerChance = 1f;
        internal const float VanillaUseTrapSwitchChance = 1f;
        internal const float VanillaUseChargerChance = 1f;
        internal const float VanillaUseTransmitterChance = 1f;
        internal const float VanillaUseShutterSwitchChance = 1f;

        private static bool _cachedMasterEnabled;
        private static bool _cachedCustom;
        private static float _cachedEmoteRespondChance = VanillaEmoteRespondChance;
        private static float _cachedEmoteSuggestChance = VanillaEmoteSuggestChance;
        private static float _cachedReactToSprinklerChance = VanillaReactToSprinklerChance;
        private static float _cachedUseTrapSwitchChance = VanillaUseTrapSwitchChance;
        private static float _cachedUseChargerChance = VanillaUseChargerChance;
        private static float _cachedUseTransmitterChance = VanillaUseTransmitterChance;
        private static float _cachedUseShutterSwitchChance = VanillaUseShutterSwitchChance;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedCustom;

        internal static float EmoteRespondChance => _cachedEmoteRespondChance;
        internal static float EmoteSuggestChance => _cachedEmoteSuggestChance;
        internal static float ReactToSprinklerChance => _cachedReactToSprinklerChance;
        internal static float UseTrapSwitchChance => _cachedUseTrapSwitchChance;
        internal static float UseChargerChance => _cachedUseChargerChance;
        internal static float UseTransmitterChance => _cachedUseTransmitterChance;
        internal static float UseShutterSwitchChance => _cachedUseShutterSwitchChance;

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null || ModConfig.MimicEmotePropsMode == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedCustom = string.Equals(
                ModConfig.MimicEmotePropsMode.Value,
                "Custom",
                StringComparison.OrdinalIgnoreCase);
            _cachedEmoteRespondChance = ReadPercent(ModConfig.EmoteRespondChancePercent, 100);
            _cachedEmoteSuggestChance = ReadPercent(ModConfig.EmoteSuggestChancePercent, 30);
            _cachedReactToSprinklerChance = ReadPercent(ModConfig.ReactToSprinklerChancePercent, 100);
            _cachedUseTrapSwitchChance = ReadPercent(ModConfig.UseTrapSwitchChancePercent, 100);
            _cachedUseChargerChance = ReadPercent(ModConfig.UseChargerChancePercent, 100);
            _cachedUseTransmitterChance = ReadPercent(ModConfig.UseTransmitterChancePercent, 100);
            _cachedUseShutterSwitchChance = ReadPercent(ModConfig.UseShutterSwitchChancePercent, 100);
        }

        private static float ReadPercent(MelonLoader.MelonPreferences_Entry<int>? entry, int defaultPercent)
        {
            int percent = entry?.Value ?? defaultPercent;
            return MimicTuningModeHelpers.PercentToProbability(Mathf.Clamp(percent, 0, 100));
        }
    }
}
