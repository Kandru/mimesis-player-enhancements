using System;
using System.Collections.Immutable;
using System.Linq;
using Bifrost.ConstEnum;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardItemCatalogService
    {
        internal const string DetoxJuiceId = "detox_juice";
        internal const string HpJuiceId = "hp_juice";

        private static readonly HashSet<int> ExcludedMasterIds =
        [
            993002,
        ];

        private static readonly HashSet<int> VariantOnlyMasterIds =
        [
            3002, 3003, 3004, 3005, 3006, 3007,
            3102, 3103, 3104, 3105, 3106, 3107,
        ];

        private static readonly (string Id, int Percent, int MasterId)[] DetoxVariants =
        [
            (DetoxJuiceId, 100, 3002),
            (DetoxJuiceId, 75, 3007),
            (DetoxJuiceId, 50, 3006),
            (DetoxJuiceId, 25, 3004),
        ];

        private static readonly (string Id, int Percent, int MasterId)[] HpVariants =
        [
            (HpJuiceId, 100, 3102),
            (HpJuiceId, 75, 3107),
            (HpJuiceId, 50, 3106),
            (HpJuiceId, 25, 3104),
        ];

        private static readonly Dictionary<string, WebDashboardItemOptionDto> CatalogById = new(StringComparer.Ordinal);

        internal static IReadOnlyList<WebDashboardItemOptionDto> BuildCatalog()
        {
            CatalogById.Clear();
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return [];
            }

            ImmutableDictionary<int, ItemMasterInfo> itemDict = excel.ItemInfoDict;
            Dictionary<string, (int MasterId, ItemMasterInfo Info)> dedupe = new(StringComparer.Ordinal);

            foreach (KeyValuePair<int, ItemMasterInfo> entry in itemDict)
            {
                int masterId = entry.Key;
                ItemMasterInfo info = entry.Value;
                if (ExcludedMasterIds.Contains(masterId) || VariantOnlyMasterIds.Contains(masterId))
                {
                    continue;
                }

                if (info.IsPromotionItemHidden)
                {
                    continue;
                }

                string nameKey = info.Name ?? string.Empty;
                string dedupeKey = $"{NormalizeItemType(info.ItemType)}|{nameKey}";
                if (dedupe.TryGetValue(dedupeKey, out (int MasterId, ItemMasterInfo Info) existing))
                {
                    if (masterId < existing.MasterId)
                    {
                        dedupe[dedupeKey] = (masterId, info);
                    }

                    continue;
                }

                dedupe[dedupeKey] = (masterId, info);
            }

            List<WebDashboardItemOptionDto> options = [];

            AddVariantGroup(options, itemDict, DetoxJuiceId, DetoxVariants);
            AddVariantGroup(options, itemDict, HpJuiceId, HpVariants);

            foreach ((int masterId, ItemMasterInfo info) in dedupe.Values.OrderBy(x => x.MasterId))
            {
                string id = masterId.ToString();
                WebDashboardItemOptionDto option = new()
                {
                    Id = id,
                    Label = ResolveLabel(info),
                    Type = NormalizeItemType(info.ItemType),
                    MasterId = masterId,
                };
                options.Add(option);
                CatalogById[id] = option;
            }

            options.Sort(CompareOptions);
            return options;
        }

        internal static bool TryResolveMasterId(string itemId, int? percent, out int masterId, out string errorKey)
        {
            masterId = 0;
            errorKey = "invalid_item";

            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (!CatalogById.TryGetValue(itemId.Trim(), out WebDashboardItemOptionDto? option))
            {
                return false;
            }

            if (option.Variants is { Count: > 0 })
            {
                int targetPercent = percent ?? 100;
                foreach (WebDashboardItemVariantDto variant in option.Variants)
                {
                    if (variant.Percent == targetPercent)
                    {
                        masterId = variant.MasterId;
                        return masterId > 0;
                    }
                }

                errorKey = "invalid_item_percent";
                return false;
            }

            if (option.MasterId is > 0)
            {
                masterId = option.MasterId.Value;
                return true;
            }

            return false;
        }

        private static void AddVariantGroup(
            List<WebDashboardItemOptionDto> options,
            ImmutableDictionary<int, ItemMasterInfo> itemDict,
            string groupId,
            (string Id, int Percent, int MasterId)[] variants)
        {
            int labelMasterId = variants[0].MasterId;
            if (!itemDict.TryGetValue(labelMasterId, out ItemMasterInfo? labelInfo))
            {
                return;
            }

            List<WebDashboardItemVariantDto> variantDtos = [];
            foreach ((string _, int percent, int masterId) in variants)
            {
                if (!itemDict.ContainsKey(masterId))
                {
                    continue;
                }

                variantDtos.Add(new WebDashboardItemVariantDto
                {
                    Percent = percent,
                    MasterId = masterId,
                });
            }

            if (variantDtos.Count == 0)
            {
                return;
            }

            variantDtos.Sort((a, b) => b.Percent.CompareTo(a.Percent));

            WebDashboardItemOptionDto option = new()
            {
                Id = groupId,
                Label = ResolveLabel(labelInfo),
                Type = NormalizeItemType(labelInfo.ItemType),
                Variants = variantDtos,
            };
            options.Add(option);
            CatalogById[groupId] = option;
        }

        private static string ResolveLabel(ItemMasterInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Name))
            {
                return info.MasterID.ToString();
            }

            string resolved = GameLocaleAccess.GetL10NText(info.Name);
            return string.IsNullOrWhiteSpace(resolved) || string.Equals(resolved, info.Name, StringComparison.Ordinal)
                ? info.MasterID.ToString()
                : resolved;
        }

        private static string NormalizeItemType(ItemType itemType)
        {
            if (itemType.Equals(ItemType.Consumable))
            {
                return "Consumable";
            }

            if (itemType.Equals(ItemType.Equipment))
            {
                return "Equipment";
            }

            return "Miscellany";
        }

        private static int CompareOptions(WebDashboardItemOptionDto a, WebDashboardItemOptionDto b)
        {
            int typeCmp = string.Compare(a.Type, b.Type, StringComparison.Ordinal);
            if (typeCmp != 0)
            {
                return typeCmp;
            }

            return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        }
    }
}
