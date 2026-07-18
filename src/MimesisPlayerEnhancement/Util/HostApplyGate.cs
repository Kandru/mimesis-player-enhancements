using ReluNetwork.ConstEnum;

namespace MimesisPlayerEnhancement.Util
{
    internal static class HostApplyGate
    {
        internal static bool IsParticipantClient()
        {
            return GameSessionAccess.TryGetPdata()?.ClientMode == NetworkClientMode.Participant;
        }

        internal static bool ShouldApplyHostOnlyFeature(Func<bool>? isFeatureEnabled = null)
        {
            if (isFeatureEnabled != null && !isFeatureEnabled())
            {
                return false;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata?.ClientMode == NetworkClientMode.Participant)
            {
                return false;
            }

            // Solo/local play often has no network host flags yet; pdata may also be null early on.
            return pdata == null || pdata.ClientMode == NetworkClientMode.Host || MimesisSaveManager.IsHost();
        }
    }
}
