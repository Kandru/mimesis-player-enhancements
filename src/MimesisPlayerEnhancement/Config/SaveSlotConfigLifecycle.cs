using MimesisPlayerEnhancement.Features.Persistence;
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
                SaveSlotConfigStore.ClearRuntimeToGlobal();
            }

            _wasSessionJoined = joined;
        }

        internal static void OnSaveSlotLoaded(int slotId)
        {
            if (!MimesisSaveManager.IsHost())
            {
                return;
            }

            SaveSlotConfigStore.ApplyOverridesToRuntime(slotId);
            _wasSessionJoined = IsSessionJoined();
        }

        private static bool IsSessionJoined()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            return pdata != null && pdata.SessionJoined;
        }
    }
}
