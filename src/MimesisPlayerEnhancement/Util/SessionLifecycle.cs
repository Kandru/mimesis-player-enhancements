using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Util
{
    internal enum SessionRole
    {
        None = 0,
        Host = 1,
        Client = 2,
    }

    internal static class SessionLifecycle
    {
        private const int EnsureSlotRetryIntervalFrames = 60;

        private static bool _wasSessionJoined;
        private static bool _sessionStartedRaised;
        private static int _ensureSlotRetryFrame = -EnsureSlotRetryIntervalFrames;
        private static SessionRole _role = SessionRole.None;
        private static int _activeSlotId = -1;

        internal static void Tick()
        {
            bool joined = IsSessionJoined();
            if (_wasSessionJoined && !joined)
            {
                EndSession();
            }
            else if (joined)
            {
                TryRaiseSessionStarted();
                TryEnsureActiveSlotLoaded();
            }

            _wasSessionJoined = joined;
        }

        internal static void NotifySessionEndedIfActive()
        {
            if (!_wasSessionJoined)
            {
                return;
            }

            EndSession();
            _wasSessionJoined = false;
        }

        private static void EndSession()
        {
            HostStatusCache.Invalidate();
            FeatureModuleSessionHooks.InvokeSessionEnded();
            _sessionStartedRaised = false;
            _role = SessionRole.None;
            _activeSlotId = -1;
        }

        private static void TryRaiseSessionStarted()
        {
            if (_sessionStartedRaised)
            {
                return;
            }

            SessionRole role = ResolveRole();
            if (role == SessionRole.None)
            {
                return;
            }

            int slotId = role == SessionRole.Host ? ResolveHostSlotId() : -1;
            if (role == SessionRole.Host && slotId < 0)
            {
                return;
            }

            _role = role;
            _activeSlotId = slotId;
            _sessionStartedRaised = true;
            HostStatusCache.Invalidate();
            FeatureModuleSessionHooks.InvokeSessionStarted(role, slotId);
        }

        private static SessionRole ResolveRole()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return MimesisSaveManager.IsHost() ? SessionRole.Host : SessionRole.None;
            }

            if (pdata.ClientMode == NetworkClientMode.Participant)
            {
                return SessionRole.Client;
            }

            if (pdata.ClientMode == NetworkClientMode.Host || MimesisSaveManager.IsHost())
            {
                return SessionRole.Host;
            }

            return SessionRole.None;
        }

        private static int ResolveHostSlotId()
        {
            int activeSlotId = SaveSlotConfigStore.ActiveSlotId;
            if (activeSlotId >= 0)
            {
                return activeSlotId;
            }

            int slotId = GameSessionAccess.GetSaveSlotId();
            return MimesisSaveManager.IsValidSaveSlotId(slotId) ? slotId : -1;
        }

        private static void TryEnsureActiveSlotLoaded()
        {
            if (_role != SessionRole.Host && !MimesisSaveManager.IsHost())
            {
                return;
            }

            int frame = UnityEngine.Time.frameCount;
            if (frame - _ensureSlotRetryFrame < EnsureSlotRetryIntervalFrames)
            {
                return;
            }

            _ensureSlotRetryFrame = frame;

            if (SaveSlotConfigStore.ActiveSlotId >= 0)
            {
                return;
            }

            int slotId = GameSessionAccess.GetSaveSlotId();
            if (!MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                return;
            }

            SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            if (_sessionStartedRaised && _activeSlotId < 0)
            {
                _activeSlotId = slotId;
            }
        }

        private static bool IsSessionJoined()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }
    }
}
