using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement
{
    internal static class SaveSlotConfigLifecycle
    {
        private static bool _wasSessionJoined;

        internal static void Tick()
        {
            bool joined = IsSessionJoined();
            if (_wasSessionJoined && !joined)
            {
                SaveSlotSidecarPersistence.OnSessionEnded();
            }

            _wasSessionJoined = joined;
        }

        private static bool IsSessionJoined()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }
    }
}
