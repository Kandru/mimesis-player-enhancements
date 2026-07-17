using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootItemFilter
    {
        private const string Feature = "LootMultiplicator";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? CandidatesProperty =
            typeof(RandomSpawnedItemActorData).GetProperty("Candidates", InstanceFlags);

        private static readonly FieldInfo? MaxRateField =
            typeof(RandomSpawnedItemActorData).GetField("_maxRate", InstanceFlags);

        private static readonly FieldInfo SpawnedActorDatasField =
            typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
            ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

        private static HashSet<int> _cachedAllowlist = [];
        private static HashSet<int> _cachedBlocklist = [];
        private static LootItemFilterMode _cachedMode = LootItemFilterMode.All;
        private static List<int> _validAllowlistIds = [];

        private enum RandomSpawnFilterResult
        {
            Skipped,
            Filtered,
            Emptied,
            Failed,
        }

        static LootItemFilter()
        {
            ModConfig.Changed += OnConfigChanged;
            ReloadFromConfig();
        }

        internal static bool ShouldApply()
        {
            return LootScalingGate.ShouldScale() && IsFilterActive();
        }

        internal static bool IsFilterActive()
        {
            return _cachedMode switch
            {
                LootItemFilterMode.AllowlistOnly => _validAllowlistIds.Count > 0,
                LootItemFilterMode.BlocklistOnly => _cachedBlocklist.Count > 0,
                _ => false,
            };
        }

        internal static bool IsSpawnAllowed(int masterId)
        {
            if (masterId <= 0)
            {
                return false;
            }

            return _cachedMode switch
            {
                LootItemFilterMode.AllowlistOnly => _cachedAllowlist.Contains(masterId),
                LootItemFilterMode.BlocklistOnly => !_cachedBlocklist.Contains(masterId),
                _ => true,
            };
        }

        internal static int PickReplacementMasterId()
        {
            if (_validAllowlistIds.Count == 0)
            {
                return 0;
            }

            return _validAllowlistIds[SimpleRandUtil.Next(0, _validAllowlistIds.Count)];
        }

        internal static void ApplyToDropList(List<int>? result, ItemDropInfo? dropInfo)
        {
            _ = dropInfo;

            if (!ShouldApply() || result == null || result.Count == 0)
            {
                return;
            }

            if (_cachedMode == LootItemFilterMode.BlocklistOnly)
            {
                result.RemoveAll(static id => !IsSpawnAllowed(id));
                return;
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (IsSpawnAllowed(result[i]))
                {
                    continue;
                }

                int replacement = PickReplacementMasterId();
                if (replacement <= 0)
                {
                    result.RemoveAt(i);
                    i--;
                    continue;
                }

                result[i] = replacement;
            }
        }

        internal static bool TryPrepareSpawn(IVroom vroom, ref ItemElement? element)
        {
            if (element == null)
            {
                return false;
            }

            if (!ShouldApply() || IsSpawnAllowed(element.ItemMasterID))
            {
                return true;
            }

            if (_cachedMode == LootItemFilterMode.BlocklistOnly)
            {
                return false;
            }

            int replacementId = PickReplacementMasterId();
            if (replacementId <= 0)
            {
                return false;
            }

            ItemElement? replacement = CreateReplacementElement(vroom, element, replacementId);
            if (replacement == null)
            {
                return false;
            }

            element = replacement;
            return true;
        }

        internal static void ApplyToRandomSpawnDatas(DungeonRoom room)
        {
            if (!ShouldApply() || room == null)
            {
                return;
            }

            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.LootSpawnFilter))
            {
                return;
            }

            if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            {
                return;
            }

            int filtered = 0;
            int emptied = 0;
            int failed = 0;

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not RandomSpawnedItemActorData randomSpawn)
                {
                    continue;
                }

                switch (TryFilterRandomSpawnCandidates(randomSpawn))
                {
                    case RandomSpawnFilterResult.Filtered:
                        filtered++;
                        break;
                    case RandomSpawnFilterResult.Emptied:
                        emptied++;
                        break;
                    case RandomSpawnFilterResult.Failed:
                        failed++;
                        break;
                }
            }

            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.LootSpawnFilter);

            if (filtered > 0 || emptied > 0)
            {
                LootMultiplicatorLog.DebugRandomSpawnPoolsFiltered(filtered, emptied, _cachedMode);
            }

            if (failed > 0)
            {
                LootMultiplicatorLog.WarnRandomSpawnPoolFilterFailed(failed);
            }
        }

        internal static void GetFilters(
            out LootItemFilterMode mode,
            out HashSet<int> allowlist,
            out HashSet<int> blocklist)
        {
            mode = _cachedMode;
            allowlist = _cachedAllowlist;
            blocklist = _cachedBlocklist;
        }

        private static RandomSpawnFilterResult TryFilterRandomSpawnCandidates(RandomSpawnedItemActorData spawnData)
        {
            ImmutableDictionary<int, (int rate, int meanPrice)> candidates = spawnData.Candidates;
            if (candidates.Count == 0 && _cachedMode != LootItemFilterMode.AllowlistOnly)
            {
                return RandomSpawnFilterResult.Skipped;
            }

            List<(int masterId, int weight, int meanPrice)> entries = ExtractIndividualRates(candidates);
            entries.RemoveAll(entry => !IsSpawnAllowed(entry.masterId));

            if (_cachedMode == LootItemFilterMode.AllowlistOnly)
            {
                InjectMissingAllowlistEntries(entries);
            }

            if (entries.Count == 0)
            {
                return RandomSpawnFilterResult.Emptied;
            }

            ImmutableDictionary<int, (int, int)> filtered = BuildCumulativeCandidates(entries);
            if (!SetRandomSpawnCandidates(spawnData, filtered))
            {
                return RandomSpawnFilterResult.Failed;
            }

            return RandomSpawnFilterResult.Filtered;
        }

        private static List<(int masterId, int weight, int meanPrice)> ExtractIndividualRates(
            ImmutableDictionary<int, (int rate, int meanPrice)> candidates)
        {
            List<(int masterId, int weight, int meanPrice)> entries = [];
            int previousCumulative = 0;

            foreach (KeyValuePair<int, (int rate, int meanPrice)> entry in candidates)
            {
                int weight = entry.Value.rate - previousCumulative;
                previousCumulative = entry.Value.rate;
                if (weight > 0)
                {
                    entries.Add((entry.Key, weight, entry.Value.meanPrice));
                }
            }

            return entries;
        }

        private static void InjectMissingAllowlistEntries(List<(int masterId, int weight, int meanPrice)> entries)
        {
            HashSet<int> present = [];
            foreach ((int masterId, _, _) in entries)
            {
                _ = present.Add(masterId);
            }

            foreach (int masterId in _validAllowlistIds)
            {
                if (present.Contains(masterId))
                {
                    continue;
                }

                if (!ItemTypeLookup.TryGetItem(masterId, out ItemMasterInfo info))
                {
                    continue;
                }

                entries.Add((masterId, 1, info.GetMeanPrice()));
            }
        }

        private static ImmutableDictionary<int, (int, int)> BuildCumulativeCandidates(
            List<(int masterId, int weight, int meanPrice)> entries)
        {
            ImmutableDictionary<int, (int, int)>.Builder builder =
                ImmutableDictionary.CreateBuilder<int, (int, int)>();
            int cumulative = 0;

            foreach ((int masterId, int weight, int meanPrice) in entries)
            {
                cumulative += weight;
                builder.Add(masterId, (cumulative, meanPrice));
            }

            return builder.ToImmutable();
        }

        private static bool SetRandomSpawnCandidates(
            RandomSpawnedItemActorData spawnData,
            ImmutableDictionary<int, (int, int)> candidates)
        {
            if (CandidatesProperty == null || MaxRateField == null)
            {
                return false;
            }

            CandidatesProperty.SetValue(spawnData, candidates);

            int maxRate = 0;
            foreach (KeyValuePair<int, (int, int)> entry in candidates)
            {
                maxRate = entry.Value.Item1;
            }

            MaxRateField.SetValue(spawnData, maxRate);
            return true;
        }

        private static ItemElement? CreateReplacementElement(IVroom vroom, ItemElement template, int replacementId)
        {
            try
            {
                ItemInfo info = template.toItemInfo();
                return vroom.GetNewItemElement(
                    replacementId,
                    template.IsFake,
                    ItemElementStackHelper.GetStackCount(template),
                    info.durability,
                    info.remainGauge,
                    info.price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Spawn filter replacement failed — master={replacementId}, {ex.Message}");
                return null;
            }
        }

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            if (change.IsFullReload
                || change.AffectsSection("MimesisPlayerEnhancement_LootMultiplicator"))
            {
                ReloadFromConfig();
            }
        }

        private static void ReloadFromConfig()
        {
            _cachedAllowlist = LootItemIdListParser.Parse(ModConfig.LootAllowlist.Value ?? "");
            _cachedBlocklist = LootItemIdListParser.Parse(ModConfig.LootBlocklist.Value ?? "");
            _cachedMode = LootItemIdListParser.ParseMode(ModConfig.LootItemFilterMode.Value ?? "");
            RebuildValidAllowlistIds();
            WarnIfFilterModeHasEmptyList();
        }

        private static void RebuildValidAllowlistIds()
        {
            _validAllowlistIds = [];
            foreach (int masterId in _cachedAllowlist)
            {
                if (ItemTypeLookup.TryGetItem(masterId, out _))
                {
                    _validAllowlistIds.Add(masterId);
                }
                else
                {
                    ModLog.Warn(Feature, $"Loot allowlist ID not found in masterdata — id={masterId}");
                }
            }
        }

        private static void WarnIfFilterModeHasEmptyList()
        {
            if (_cachedMode == LootItemFilterMode.AllowlistOnly && _validAllowlistIds.Count == 0)
            {
                ModLog.Warn(Feature, "LootItemFilterMode is AllowlistOnly but allowlist is empty — spawn filter inactive");
                return;
            }

            if (_cachedMode == LootItemFilterMode.BlocklistOnly && _cachedBlocklist.Count == 0)
            {
                ModLog.Warn(Feature, "LootItemFilterMode is BlocklistOnly but blocklist is empty — spawn filter inactive");
            }
        }
    }
}
