using System.Globalization;
using System.Text;
using Bifrost.ShopGroup;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapPoiLabels
    {
        internal static string Resolve(LevelObject levelObject)
        {
            return levelObject switch
            {
                VendingMachineLevelObject vendingMachine => ResolveVendingLabel(vendingMachine),
                ShowerBoothLevelObject shower => ResolveSimpleText(shower, "dashboard.minimap_tooltip_shower"),
                GameSaveLevelObject save => ResolveSimpleText(save, "dashboard.minimap_tooltip_save"),
                NewTramLeverLevelObject lever => ResolveSimpleText(lever, "dashboard.minimap_tooltip_tram_start"),
                _ => string.Empty,
            };
        }

        internal static string ResolveTeleporterLabel(
            TeleporterLevelObject teleporter,
            string targetAreaId,
            WebDashboardMinimapLayoutDto? layout)
        {
            string callSign = HumanizeCallSign(teleporter.StartCallSign);
            string destination = ResolveAreaLabel(layout, targetAreaId);
            if (string.IsNullOrWhiteSpace(callSign))
            {
                return destination;
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                return callSign;
            }

            return callSign + " — " + destination;
        }

        private static string ResolveVendingLabel(VendingMachineLevelObject vendingMachine)
        {
            string prefix = ModL10n.Get("dashboard.minimap_tooltip_vending");
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null
                || !excel.ShopGroupDict.TryGetValue(vendingMachine.shopGroupID, out ShopGroup_MasterData? shop)
                || shop == null
                || !excel.ItemInfoDict.TryGetValue(shop.item_masterid, out ItemMasterInfo? item)
                || item == null)
            {
                return prefix;
            }

            string itemName = ResolveItemLabel(item);
            StringBuilder label = new(prefix);
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                label.Append(" — ").Append(itemName);
            }

            if (!string.IsNullOrWhiteSpace(item.VendingMachineTooltip))
            {
                string contents = GameLocaleAccess.GetL10NText(item.VendingMachineTooltip);
                if (!string.IsNullOrWhiteSpace(contents))
                {
                    label.Append(" — ").Append(contents);
                }
            }

            return SanitizeTooltip(label.ToString());
        }

        private static string ResolveSimpleText(LevelObject levelObject, string fallbackKey)
        {
            try
            {
                string text = levelObject.GetSimpleText(null);
                text = SanitizeTooltip(text);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            catch
            {
                /* fall back to static label */
            }

            return ModL10n.Get(fallbackKey);
        }

        private static string ResolveItemLabel(ItemMasterInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.Name))
            {
                string resolved = GameLocaleAccess.GetL10NText(info.Name);
                if (!string.IsNullOrWhiteSpace(resolved) && !string.Equals(resolved, info.Name, StringComparison.Ordinal))
                {
                    return resolved;
                }
            }

            return info.MasterID.ToString(CultureInfo.InvariantCulture);
        }

        private static string ResolveAreaLabel(WebDashboardMinimapLayoutDto? layout, string areaId)
        {
            if (layout == null || string.IsNullOrWhiteSpace(areaId))
            {
                return string.Empty;
            }

            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                if (area.Id == areaId && !string.IsNullOrWhiteSpace(area.Label))
                {
                    return area.Label;
                }
            }

            return HumanizeCallSign(areaId);
        }

        private static string HumanizeCallSign(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string[] parts = raw.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return raw.Trim();
            }

            StringBuilder builder = new();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    builder.Append(part[1..].ToLowerInvariant());
                }
            }

            return builder.ToString();
        }

        private static string SanitizeTooltip(string text)
        {
            return text
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }
    }
}
