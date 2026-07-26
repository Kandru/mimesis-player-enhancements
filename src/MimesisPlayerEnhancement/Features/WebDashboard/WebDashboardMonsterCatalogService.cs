using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMonsterCatalogService
    {
        private static readonly Dictionary<string, WebDashboardMonsterOptionDto> CatalogById = new(StringComparer.Ordinal);

        internal static IReadOnlyList<WebDashboardMonsterOptionDto> BuildCatalog()
        {
            CatalogById.Clear();
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return [];
            }

            List<WebDashboardMonsterOptionDto> options = [];

            foreach (KeyValuePair<int, MonsterInfo> entry in excel.MonsterInfoDict)
            {
                int masterId = entry.Key;
                MonsterInfo info = entry.Value;
                string id = masterId.ToString();
                WebDashboardMonsterOptionDto option = new()
                {
                    Id = id,
                    Label = ResolveLabel(info),
                    Type = ResolveType(info.MonsterType),
                    MasterId = masterId,
                };
                options.Add(option);
                CatalogById[id] = option;
            }

            options.Sort(CompareOptions);
            return options;
        }

        internal static bool TryResolveMasterId(string monsterId, out int masterId, out string errorKey)
        {
            masterId = 0;
            errorKey = "invalid_monster";

            if (string.IsNullOrWhiteSpace(monsterId))
            {
                return false;
            }

            if (!CatalogById.TryGetValue(monsterId.Trim(), out WebDashboardMonsterOptionDto? option))
            {
                return false;
            }

            if (option.MasterId is > 0)
            {
                masterId = option.MasterId.Value;
                return true;
            }

            return false;
        }

        private static string ResolveLabel(MonsterInfo info)
        {
            int masterId = info.MasterID;

            string modKey = $"dashboard.monster_label_{masterId}";
            string modLabel = WebDashboardL10n.Get(modKey);
            if (!string.Equals(modLabel, modKey, StringComparison.Ordinal))
            {
                return modLabel;
            }

            if (string.IsNullOrWhiteSpace(info.Name))
            {
                return masterId.ToString();
            }

            string nameKey = info.Name;
            string nameModKey = $"dashboard.monster_name_{nameKey}";
            string nameModLabel = WebDashboardL10n.Get(nameModKey);
            if (!string.Equals(nameModLabel, nameModKey, StringComparison.Ordinal))
            {
                return nameModLabel;
            }

            string resolved = GameLocaleAccess.GetL10NText(nameKey);
            if (!IsBrokenGameLabel(resolved, nameKey, masterId))
            {
                return resolved;
            }

            string humanized = HumanizeNameKey(nameKey);
            return string.IsNullOrWhiteSpace(humanized) ? masterId.ToString() : humanized;
        }

        private static string HumanizeNameKey(string nameKey)
        {
            string[] parts = nameKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return nameKey;
            }

            string[] words = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                {
                    words[i] = part;
                    continue;
                }

                if (part.Length == 1)
                {
                    words[i] = part.ToUpperInvariant();
                    continue;
                }

                words[i] = char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
            }

            return string.Join(" ", words);
        }

        private static bool IsBrokenGameLabel(string resolved, string nameKey, int masterId)
        {
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return true;
            }

            if (resolved.Contains("L10N error", StringComparison.OrdinalIgnoreCase))
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

            return false;
        }

        private static string ResolveType(MonsterType monsterType)
        {
            if (monsterType.Equals(MonsterType.Mimic))
            {
                return "Mimic";
            }

            if (monsterType.Equals(MonsterType.Boss))
            {
                return "Boss";
            }

            if (monsterType.Equals(MonsterType.Jako))
            {
                return "Jako";
            }

            if (monsterType.Equals(MonsterType.Special))
            {
                return "Special";
            }

            return "Special";
        }

        private static int GetTypeSortOrder(string type)
        {
            return type switch
            {
                "Mimic" => 0,
                "Boss" => 1,
                "Jako" => 2,
                "Special" => 3,
                _ => 4,
            };
        }

        private static int CompareOptions(WebDashboardMonsterOptionDto a, WebDashboardMonsterOptionDto b)
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
