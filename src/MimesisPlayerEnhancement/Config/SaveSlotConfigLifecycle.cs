namespace MimesisPlayerEnhancement
{
    internal static class SaveSlotConfigLifecycle
    {
        private const int EnsureSlotRetryIntervalFrames = 60;

        private static bool _wasSessionJoined;
        private static int _ensureSlotRetryFrame = -EnsureSlotRetryIntervalFrames;

        internal static void Tick()
        {
            bool joined = IsSessionJoined();
            if (_wasSessionJoined && !joined)
            {
                SaveSlotSidecarPersistence.OnSessionEnded();
            }
            else if (joined)
            {
                TryEnsureActiveSlotLoaded();
            }

            _wasSessionJoined = joined;
        }

        private static void TryEnsureActiveSlotLoaded()
        {
            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            int frame = UnityEngine.Time.frameCount;
            if (frame - _ensureSlotRetryFrame < EnsureSlotRetryIntervalFrames)
            {
                return;
            }

            _ensureSlotRetryFrame = frame;

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId)
                || SaveSlotConfigStore.ActiveSlotId == slotId)
            {
                return;
            }

            SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
        }

        private static bool IsSessionJoined()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }
    }
}
