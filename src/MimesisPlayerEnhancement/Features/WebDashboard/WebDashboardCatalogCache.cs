using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Caches item/dungeon catalogs and host-cheats state on the main thread.
    /// </summary>
    internal static class WebDashboardCatalogCache
    {
        private static string? _itemsJson;
        private static string? _dungeonsJson;
        private static WebDashboardHostCheatsDto _hostCheats = new();
        private static int _catalogSessionToken;

        internal static void Invalidate()
        {
            _itemsJson = null;
            _dungeonsJson = null;
            _catalogSessionToken++;
        }

        internal static void RefreshHostCheats()
        {
            _hostCheats = WebDashboardHostCheatsService.BuildState();
        }

        internal static WebDashboardHostCheatsDto GetHostCheats() => _hostCheats;

        internal static string GetItemsJson()
        {
            return _itemsJson ?? WebDashboardJson.SerializeItems(new List<WebDashboardItemOptionDto>());
        }

        internal static string GetDungeonsJson()
        {
            return _dungeonsJson ?? WebDashboardJson.SerializeDungeons(new List<WebDashboardDungeonOptionDto>());
        }

        private static void EnsureItems()
        {
            if (_itemsJson != null || !GameLocaleAccess.IsMainThread)
            {
                return;
            }

            _itemsJson = WebDashboardJson.SerializeItems(WebDashboardItemCatalogService.BuildCatalog());
        }

        private static void EnsureDungeons()
        {
            if (_dungeonsJson != null || !GameLocaleAccess.IsMainThread)
            {
                return;
            }

            _dungeonsJson = WebDashboardJson.SerializeDungeons(WebDashboardDungeonCatalogService.BuildCatalog());
        }

        internal static void RefreshCatalogsIfNeeded(bool connected)
        {
            if (!connected)
            {
                Invalidate();
                return;
            }

            int token = WebDashboardSnapshotCache.Version;
            if (token == _catalogSessionToken && _itemsJson != null && _dungeonsJson != null)
            {
                return;
            }

            _catalogSessionToken = token;
            _itemsJson = null;
            _dungeonsJson = null;
            EnsureItems();
            EnsureDungeons();
        }
    }
}
