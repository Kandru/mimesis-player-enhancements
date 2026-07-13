using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapPoiCollector
    {
        internal static List<WebDashboardMinimapPoiDto> CollectForLayout(
            GameMainBase? main,
            WebDashboardMinimapBoundsDto bounds)
        {
            List<WebDashboardMinimapPoiDto> pois = [];
            if (main == null || IsPlaceholderBounds(bounds))
            {
                return pois;
            }

            DynamicDataManager? dynamicData = HubGameDataAccess.DynamicData;
            if (dynamicData == null)
            {
                return pois;
            }

            foreach (LevelObject levelObject in dynamicData.GetAllLevelObjects(excludeClientOnly: false).Values)
            {
                if (levelObject == null || !levelObject.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string? kind = ResolvePoiKind(levelObject);
                if (kind == null)
                {
                    continue;
                }

                WebDashboardMinimapTramSpace.WorldToLocal(
                    main,
                    levelObject.transform,
                    out float x,
                    out float z,
                    out _);

                pois.Add(new WebDashboardMinimapPoiDto
                {
                    Kind = kind,
                    X = NormalizeCoord(x, bounds.MinX, bounds.MaxX),
                    Z = NormalizeCoord(z, bounds.MinZ, bounds.MaxZ),
                    AreaId = WebDashboardMinimapAreaResolver.HubAreaId,
                    Label = WebDashboardMinimapPoiLabels.Resolve(levelObject),
                });
            }

            return pois;
        }

        private static string? ResolvePoiKind(LevelObject levelObject) => levelObject switch
        {
            VendingMachineLevelObject => "vending",
            ShowerBoothLevelObject => "shower",
            GameSaveLevelObject => "save",
            NewTramLeverLevelObject => "tram_start",
            _ => null,
        };

        private static float NormalizeCoord(float value, float min, float max)
        {
            float span = max - min;
            return span <= 0f ? 0.5f : Mathf.Clamp01((value - min) / span);
        }

        private static bool IsPlaceholderBounds(WebDashboardMinimapBoundsDto bounds)
        {
            return bounds.MinX == 0f
                && bounds.MinZ == 0f
                && bounds.MaxX == 1f
                && bounds.MaxZ == 1f;
        }
    }
}
