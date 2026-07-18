namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningRuntime
    {
        private const string Feature = "MimicTuning";

        private static bool _initialized;
        private static bool _lastMasterEnabled;
        private static MimicVoiceTuningMode _lastVoiceMode = MimicVoiceTuningMode.Vanilla;
        private static MimicInventoryCopyMode _lastInventoryMode = MimicInventoryCopyMode.Vanilla;

        internal static void RefreshFromConfig()
        {
            MimicVoiceTuningResolver.RefreshConfigCache();
            MimicInventoryCopyResolver.RefreshConfigCache();
            MimicPossessionResolver.RefreshConfigCache();

            bool masterEnabled = MimicVoiceTuningResolver.IsMasterEnabled;
            MimicVoiceTuningMode voiceMode = MimicVoiceTuningResolver.Mode;
            MimicInventoryCopyMode inventoryMode = MimicInventoryCopyResolver.Mode;

            if (!_initialized)
            {
                _initialized = true;
                _lastMasterEnabled = masterEnabled;
                _lastVoiceMode = voiceMode;
                _lastInventoryMode = inventoryMode;
                return;
            }

            if (_lastMasterEnabled != masterEnabled)
            {
                ModLog.Info(Feature, $"Mimic tuning master toggle — enabled={masterEnabled}");
                _lastMasterEnabled = masterEnabled;
            }

            if (_lastVoiceMode != voiceMode)
            {
                ModLog.Info(Feature, $"Mimic voice tuning mode — {voiceMode}");
                _lastVoiceMode = voiceMode;
            }

            if (_lastInventoryMode != inventoryMode)
            {
                ModLog.Info(Feature, $"Mimic inventory copy mode — {inventoryMode}");
                _lastInventoryMode = inventoryMode;
            }
        }

        internal static void OnDungeonEnter()
        {
            ClearTransientState();
            MimicPossessionResolver.RefreshConfigCache();
            ModLog.Debug(Feature, "Dungeon entered — mimic possession state initialized");
        }

        internal static void OnDungeonEnd()
        {
            ClearTransientState();
            ModLog.Debug(Feature, "Dungeon ended — mimic possession state reset");
        }

        internal static void OnSessionEnded()
        {
            ClearTransientState();
            ModLog.Debug(Feature, "Session ended — mimic tuning transient state reset");
        }

        private static void ClearTransientState()
        {
            MimicPossessionSessions.ClearAll();
            Patches.MimicPossessionPatchSupport.ClearProgressBarRestartStates();
            MimicVoiceTuning.MimicVoiceTuningPlaybackTrace.ClearAll();
        }
    }
}
