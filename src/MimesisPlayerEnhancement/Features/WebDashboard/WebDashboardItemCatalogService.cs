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
            (DetoxJuiceId, 71, 3007),
            (DetoxJuiceId, 47, 3006),
            (DetoxJuiceId, 23, 3004),
        ];

        private static readonly (string Id, int Percent, int MasterId)[] HpVariants =
        [
            (HpJuiceId, 100, 3102),
            (HpJuiceId, 71, 3107),
            (HpJuiceId, 47, 3106),
            (HpJuiceId, 23, 3104),
        ];

        private static readonly HashSet<int> DeveloperMasterIds =
        [
            3001,
            2531,
            59998,
            59999,
            772030,
            775161,
            785161,
            777777,
            10000,
            9999,
        ];

        private const int UsableEquipmentMaxSellPrice = 1;
        private const int DeveloperMinSellPrice = 9999;

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
                    Type = ResolveCategory(info),
                    MasterId = masterId,
                    SellPriceMin = info.PriceForSellMin,
                    SellPriceMax = info.PriceForSellMax,
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
                if (!itemDict.TryGetValue(masterId, out ItemMasterInfo? variantInfo))
                {
                    continue;
                }

                variantDtos.Add(new WebDashboardItemVariantDto
                {
                    Percent = percent,
                    MasterId = masterId,
                    SellPriceMin = variantInfo.PriceForSellMin,
                    SellPriceMax = variantInfo.PriceForSellMax,
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
            int masterId = info.MasterID;
            string modKey = $"dashboard.item_label_{masterId}";
            string modLabel = ModL10n.Get(modKey);
            if (!string.Equals(modLabel, modKey, StringComparison.Ordinal))
            {
                return modLabel;
            }

            if (string.IsNullOrWhiteSpace(info.Name))
            {
                return masterId.ToString();
            }

            string resolved = GameLocaleAccess.GetL10NText(info.Name);
            if (IsBrokenGameLabel(resolved, info.Name, masterId))
            {
                return masterId.ToString();
            }

            return resolved;
        }

        private static bool IsBrokenGameLabel(string resolved, string nameKey, int masterId)
        {
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return true;
            }

            if (string.Equals(resolved, nameKey, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(resolved, masterId.ToString(), StringComparison.Ordinal))
            {
                return true;
            }

            if (resolved.Contains("STRING_ITEM_NAME", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsDeveloperItem(ItemMasterInfo info)
        {
            if (DeveloperMasterIds.Contains(info.MasterID))
            {
                return true;
            }

            if (info.PriceForSellMin >= DeveloperMinSellPrice
                || info.PriceForSellMax >= DeveloperMinSellPrice)
            {
                return true;
            }

            string lootId = info.LootingObjectID ?? string.Empty;
            if (lootId.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string nameKey = info.Name ?? string.Empty;
            if (nameKey.Contains("forTest", StringComparison.Ordinal))
            {
                return true;
            }

            if (ContainsHangul(nameKey))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(nameKey))
            {
                return false;
            }

            string resolved = GameLocaleAccess.GetL10NText(nameKey);
            if (ContainsHangul(resolved))
            {
                return true;
            }

            if (IsBrokenGameLabel(resolved, nameKey, info.MasterID))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                if (resolved.Contains("forTest", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (resolved.Contains("test", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsHangul(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char ch in value)
            {
                if (ch >= '\uAC00' && ch <= '\uD7A3')
                {
                    return true;
                }
            }

            return false;
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

        private static string ResolveCategory(ItemMasterInfo info)
        {
            if (IsDeveloperItem(info))
            {
                return "Developer";
            }

            if (info.ItemType.Equals(ItemType.Consumable))
            {
                return "Consumable";
            }

            if (IsUsableEquipment(info))
            {
                return "Equipment";
            }

            return "Miscellany";
        }

        private static bool IsUsableEquipment(ItemMasterInfo info)
        {
            if (!info.ItemType.Equals(ItemType.Equipment))
            {
                return false;
            }

            return info.PriceForSellMax <= UsableEquipmentMaxSellPrice
                && info.PriceForSellMin <= UsableEquipmentMaxSellPrice;
        }

        private static int GetTypeSortOrder(string type)
        {
            return type switch
            {
                "Consumable" => 0,
                "Equipment" => 1,
                "Miscellany" => 2,
                "Developer" => 3,
                _ => 4,
            };
        }

        private static int CompareOptions(WebDashboardItemOptionDto a, WebDashboardItemOptionDto b)
        {
            int typeCmp = GetTypeSortOrder(a.Type).CompareTo(GetTypeSortOrder(b.Type));
            if (typeCmp != 0)
            {
                return typeCmp;
            }

            return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        }
    }
}
