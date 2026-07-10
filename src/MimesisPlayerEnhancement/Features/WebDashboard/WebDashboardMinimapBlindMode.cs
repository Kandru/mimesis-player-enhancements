using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Server-side blind mode for minimap marker filtering (host dashboard viewer).
    /// </summary>
    internal static class WebDashboardMinimapBlindMode
    {
        private static volatile bool _enabled = true;

        internal static bool Enabled => _enabled;

        internal static void SetEnabled(bool enabled)
        {
            bool wasEnabled = _enabled;
            _enabled = enabled;
            if (enabled && !wasEnabled)
            {
                WebDashboardHostCheatsRuntime.DisableAll("blind mode enabled");
            }

            WebDashboardSnapshotCache.MarkDirty();
            WebDashboardSseHub.NotifyMinimapChanged();
        }

        internal static bool ShouldHideOtherPlayers()
        {
            if (!_enabled)
            {
                return false;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (!WebDashboardSessionScene.IsBlindModeScene(pdata?.main))
            {
                return false;
            }

            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            if (localSteamId == 0)
            {
                return false;
            }

            foreach (WebDashboardPlayerDto player in WebDashboardSnapshotCache.Get().Players)
            {
                if (player.IsLocal)
                {
                    return player.IsAlive;
                }
            }

            return false;
        }
    }
}
