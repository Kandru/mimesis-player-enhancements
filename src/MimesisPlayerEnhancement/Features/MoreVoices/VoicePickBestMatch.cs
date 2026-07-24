using System.Diagnostics;
using System.Text;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Allocation-light reimplementation of vanilla <see cref="SpeechEventAdditionalGameData.PickBestMatch"/>.
    /// </summary>
    internal static class VoicePickBestMatch
    {
        private const string Feature = "MoreVoices";
        private const int ProfileEventThreshold = 500;
        private const long ProfileElapsedMsThreshold = 2;
        private const float CrossAreaFallbackChance = 0.25f;
        private const float OutdoorCrossIndoorChance = 0.25f;
        private const float TrapMonsterFallbackChance = 0.10f;
        private const int TopK = 5;
        private const int MaxRandomPool = 5;
        private const int MonsterMimicEventId = 20000010;
        private const int GrabSkillEventId = 60001;

        private static readonly SpeechEvent_IncomingType[] IncomingTypePriority =
        [
            SpeechEvent_IncomingType.BearTrapped,
            SpeechEvent_IncomingType.CheckFriendly,
            SpeechEvent_IncomingType.Monster,
            SpeechEvent_IncomingType.User,
            SpeechEvent_IncomingType.OnDeath,
            SpeechEvent_IncomingType.OnDamaged,
            SpeechEvent_IncomingType.UseSkill,
            SpeechEvent_IncomingType.HandHeldItem,
            SpeechEvent_IncomingType.TimeBombWarning,
            SpeechEvent_IncomingType.SprinklerActivated,
            SpeechEvent_IncomingType.Lightning,
            SpeechEvent_IncomingType.HeliumGasActivated,
            SpeechEvent_IncomingType.InvisibleMine,
            SpeechEvent_IncomingType.GrabSkill,
            SpeechEvent_IncomingType.CorridorSwitches,
            SpeechEvent_IncomingType.Paintspot,
            SpeechEvent_IncomingType.Paintball,
            SpeechEvent_IncomingType.ScrapObject,
            SpeechEvent_IncomingType.Charger,
            SpeechEvent_IncomingType.CrowShop,
            SpeechEvent_IncomingType.Teleporter,
            SpeechEvent_IncomingType.ClosedRoom,
            SpeechEvent_IncomingType.SquallWarning,
            SpeechEvent_IncomingType.Blackout,
            SpeechEvent_IncomingType.ChargeCompleted,
        ];

        private static readonly List<(string playerID, SpeechEvent evt)> _areaMatched = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _strictAreaPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _fallbackPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _generalIndoorPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _trapIndoorPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _noIncoming = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _incomingMatches = [];
        private static readonly Dictionary<SpeechEvent_IncomingType, List<(string playerID, SpeechEvent evt)>> _incomingCandidateIndex = [];
        private static readonly Dictionary<SpeechEvent_IncomingType, HashSet<int>> _incomingIdsByType = [];
        private static readonly HashSet<SpeechEvent_IncomingType> _matchedIncomingTypes = [];
        private static readonly HashSet<SpeechEvent_IncomingType> _typesAddedThisEvent = [];
        private static readonly StringBuilder _incomingReasonBuilder = new();

        private static readonly (string playerID, SpeechEvent evt, float similarity)[] _topSlots = new (string, SpeechEvent, float)[TopK];

        internal static bool TryPick(
            MimicVoiceSpawner.MimicContext context,
            List<(string playerID, SpeechEvent evt)>? allEvents,
            SpeechEventAdditionalGameData curGameData,
            bool periodic,
            int pickCount,
            float playTimeIntervalRandom,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason)
        {
            int eventCount = allEvents?.Count ?? 0;
            bool profile = ModConfig.EnableDebugLogging.Value;
            long startTicks = profile ? Stopwatch.GetTimestamp() : 0;

            bool picked = TryPickCore(
                context,
                allEvents,
                curGameData,
                periodic,
                pickCount,
                playTimeIntervalRandom,
                out speechEvent,
                out mimickingPlayerID,
                out pickReason);

            if (profile)
            {
                MaybeLogPickProfile(eventCount, startTicks, picked, pickReason);
            }

            return picked;
        }

        private static bool TryPickCore(
            MimicVoiceSpawner.MimicContext context,
            List<(string playerID, SpeechEvent evt)>? allEvents,
            SpeechEventAdditionalGameData curGameData,
            bool periodic,
            int pickCount,
            float playTimeIntervalRandom,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason)
        {
            _ = context;
            _ = periodic;

            speechEvent = null;
            mimickingPlayerID = string.Empty;
            pickReason = string.Empty;

            if (allEvents == null || allEvents.Count == 0 || curGameData == null)
            {
                return false;
            }

            float playTimeIntervalTemp = ResolvePlayTimeInterval(
                curGameData.Area,
                playTimeIntervalRandom,
                MimicVoiceTuningResolver.GetClipReuseCooldownSeconds(),
                MimicVoiceTuningResolver.GetDeathMatchClipReuseCooldownSeconds());

            float now = GameSessionAccess.GetCurrentTickSec();
            if (!TryBuildCandidatePoolSinglePass(allEvents, curGameData, now, playTimeIntervalTemp, out string poolReason))
            {
                return false;
            }

            if (_areaMatched.Count <= pickCount)
            {
                return FinishPickOldest(
                    _areaMatched,
                    out speechEvent,
                    out mimickingPlayerID,
                    out pickReason,
                    poolReason);
            }

            ShuffleInPlace(_areaMatched);

            if (curGameData.IncomingEventStart == null || curGameData.IncomingEventStart.Count == 0)
            {
                FilterNoIncomingEvents(_areaMatched);
                if (_noIncoming.Count == 0)
                {
                    return false;
                }

                if (_noIncoming.Count <= pickCount)
                {
                    return FinishPickOldest(
                        _noIncoming,
                        out speechEvent,
                        out mimickingPlayerID,
                        out pickReason,
                        "NoIncomingEvents");
                }

                return TryPickFromObservation(_noIncoming, curGameData, out speechEvent, out mimickingPlayerID, out pickReason);
            }

            return TryPickIncomingMatch(curGameData, out speechEvent, out mimickingPlayerID, out pickReason);
        }

        internal static float ResolvePlayTimeInterval(
            SpeechType_Area area,
            float playTimeIntervalRandom,
            float clipReuseCooldownSeconds,
            float deathMatchClipReuseCooldownSeconds)
        {
            return VoiceEventContext.IsDeathMatch(area)
                ? deathMatchClipReuseCooldownSeconds + playTimeIntervalRandom
                : clipReuseCooldownSeconds;
        }

        private static bool TryBuildCandidatePoolSinglePass(
            List<(string playerID, SpeechEvent evt)> allEvents,
            SpeechEventAdditionalGameData curGameData,
            float now,
            float playTimeIntervalTemp,
            out string poolReason)
        {
            _areaMatched.Clear();
            _strictAreaPool.Clear();
            _fallbackPool.Clear();
            _generalIndoorPool.Clear();
            _trapIndoorPool.Clear();
            poolReason = string.Empty;

            if (!MoreVoicesUnify.IsActive)
            {
                for (int i = 0; i < allEvents.Count; i++)
                {
                    (string playerID, SpeechEvent evt) pair = allEvents[i];
                    SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                    if (gameData == null)
                    {
                        continue;
                    }

                    if (now - pair.evt.LastPlayedTime < playTimeIntervalTemp)
                    {
                        continue;
                    }

                    if (gameData.IndoorEntered == curGameData.IndoorEntered && gameData.Area == curGameData.Area)
                    {
                        _areaMatched.Add(pair);
                    }
                }

                if (_areaMatched.Count == 0)
                {
                    return false;
                }

                poolReason = $"Area({curGameData.Area}), Indoor({curGameData.IndoorEntered})";
                return true;
            }

            if (VoiceEventContext.IsDeathMatch(curGameData.Area))
            {
                for (int i = 0; i < allEvents.Count; i++)
                {
                    (string playerID, SpeechEvent evt) pair = allEvents[i];
                    SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                    if (gameData == null)
                    {
                        continue;
                    }

                    if (now - pair.evt.LastPlayedTime < playTimeIntervalTemp)
                    {
                        continue;
                    }

                    if (gameData.Area == curGameData.Area)
                    {
                        _areaMatched.Add(pair);
                    }
                }

                if (_areaMatched.Count == 0)
                {
                    return false;
                }

                poolReason = $"StrictArea({curGameData.Area})";
                return true;
            }

            if (VoiceEventContext.IsOutdoorArea(curGameData.Area))
            {
                BucketIntervalEligibleOutdoor(allEvents, curGameData, now, playTimeIntervalTemp);
                return SelectOutdoorPool(curGameData, out poolReason);
            }

            BucketIntervalEligibleIndoor(allEvents, curGameData, now, playTimeIntervalTemp);
            return SelectIndoorPool(curGameData, out poolReason);
        }

        private static void BucketIntervalEligibleOutdoor(
            List<(string playerID, SpeechEvent evt)> allEvents,
            SpeechEventAdditionalGameData curGameData,
            float now,
            float playTimeIntervalTemp)
        {
            for (int i = 0; i < allEvents.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = allEvents[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                if (now - pair.evt.LastPlayedTime < playTimeIntervalTemp)
                {
                    continue;
                }

                if (gameData.Area == curGameData.Area)
                {
                    _strictAreaPool.Add(pair);
                    continue;
                }

                if (VoiceEventContext.IsDeathMatch(gameData.Area)
                    || VoiceEventContext.IsOutdoorArea(gameData.Area))
                {
                    continue;
                }

                if (VoiceEventContext.IsTrapOrMonster(pair.evt))
                {
                    _trapIndoorPool.Add(pair);
                }
                else
                {
                    _generalIndoorPool.Add(pair);
                }
            }
        }

        private static void BucketIntervalEligibleIndoor(
            List<(string playerID, SpeechEvent evt)> allEvents,
            SpeechEventAdditionalGameData curGameData,
            float now,
            float playTimeIntervalTemp)
        {
            for (int i = 0; i < allEvents.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = allEvents[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                if (now - pair.evt.LastPlayedTime < playTimeIntervalTemp)
                {
                    continue;
                }

                if (gameData.Area == curGameData.Area)
                {
                    _strictAreaPool.Add(pair);
                    continue;
                }

                if (VoiceEventContext.IsDeathMatch(gameData.Area))
                {
                    continue;
                }

                if (gameData.Area != curGameData.Area)
                {
                    _fallbackPool.Add(pair);
                }
            }
        }

        private static bool SelectOutdoorPool(SpeechEventAdditionalGameData curGameData, out string poolReason)
        {
            poolReason = string.Empty;
            bool hasStrict = _strictAreaPool.Count > 0;
            bool hasGeneralIndoor = _generalIndoorPool.Count > 0;
            bool hasTrapIndoor = _trapIndoorPool.Count > 0;

            if (hasStrict && UnityEngine.Random.value >= OutdoorCrossIndoorChance)
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                return _areaMatched.Count > 0;
            }

            if (hasGeneralIndoor)
            {
                CopyPool(_generalIndoorPool, _areaMatched);
                poolReason = "CrossIndoorGeneral";
                return true;
            }

            if (hasTrapIndoor && (!hasGeneralIndoor || UnityEngine.Random.value < TrapMonsterFallbackChance))
            {
                CopyPool(_trapIndoorPool, _areaMatched);
                poolReason = "CrossIndoorTrapMonster";
                return true;
            }

            if (hasStrict)
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                return true;
            }

            return false;
        }

        private static bool SelectIndoorPool(SpeechEventAdditionalGameData curGameData, out string poolReason)
        {
            poolReason = string.Empty;
            bool hasStrict = _strictAreaPool.Count > 0;
            bool hasFallback = _fallbackPool.Count > 0;

            if (hasStrict && (UnityEngine.Random.value >= CrossAreaFallbackChance || !hasFallback))
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                return true;
            }

            if (hasFallback)
            {
                CopyPool(_fallbackPool, _areaMatched);
                poolReason = "CrossAreaFallback";
                return true;
            }

            return false;
        }

        private static void CopyPool(
            List<(string playerID, SpeechEvent evt)> source,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            destination.Clear();
            destination.AddRange(source);
        }

        private static void FilterNoIncomingEvents(List<(string playerID, SpeechEvent evt)> source)
        {
            _noIncoming.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                List<IncomingEvent>? incoming = pair.evt.GameData?.IncomingEventStart;
                if (incoming == null || incoming.Count == 0)
                {
                    _noIncoming.Add(pair);
                }
            }
        }

        private static void BuildIncomingIdsByType(SpeechEventAdditionalGameData curGameData)
        {
            foreach (HashSet<int> ids in _incomingIdsByType.Values)
            {
                ids.Clear();
            }

            List<IncomingEvent> incomingStart = curGameData.IncomingEventStart;
            for (int i = 0; i < incomingStart.Count; i++)
            {
                IncomingEvent incoming = incomingStart[i];
                if (!_incomingIdsByType.TryGetValue(incoming.EventType, out HashSet<int>? ids))
                {
                    ids = [];
                    _incomingIdsByType[incoming.EventType] = ids;
                }

                _ = ids.Add(incoming.EventID);
            }
        }

        private static void BuildIncomingCandidateIndex()
        {
            foreach (List<(string playerID, SpeechEvent evt)> bucket in _incomingCandidateIndex.Values)
            {
                bucket.Clear();
            }

            for (int i = 0; i < _areaMatched.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = _areaMatched[i];
                List<IncomingEvent>? incoming = pair.evt.GameData?.IncomingEventStart;
                if (incoming == null || incoming.Count == 0)
                {
                    continue;
                }

                _typesAddedThisEvent.Clear();
                for (int j = 0; j < incoming.Count; j++)
                {
                    SpeechEvent_IncomingType eventType = incoming[j].EventType;
                    if (!_typesAddedThisEvent.Add(eventType))
                    {
                        continue;
                    }

                    if (!_incomingCandidateIndex.TryGetValue(eventType, out List<(string playerID, SpeechEvent evt)>? bucket))
                    {
                        bucket = [];
                        _incomingCandidateIndex[eventType] = bucket;
                    }

                    bucket.Add(pair);
                }
            }
        }

        private static bool TryPickIncomingMatch(
            SpeechEventAdditionalGameData curGameData,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason)
        {
            speechEvent = null;
            mimickingPlayerID = string.Empty;
            pickReason = string.Empty;

            BuildIncomingIdsByType(curGameData);
            BuildIncomingCandidateIndex();

            _incomingMatches.Clear();
            _matchedIncomingTypes.Clear();

            for (int typeIndex = 0; typeIndex < IncomingTypePriority.Length; typeIndex++)
            {
                SpeechEvent_IncomingType incType = IncomingTypePriority[typeIndex];
                if (!_incomingIdsByType.ContainsKey(incType))
                {
                    continue;
                }

                if (!_incomingCandidateIndex.TryGetValue(incType, out List<(string playerID, SpeechEvent evt)>? candidates))
                {
                    continue;
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    (string playerID, SpeechEvent evt) pair = candidates[i];
                    if (!MatchesIncomingType(pair.evt, incType))
                    {
                        continue;
                    }

                    _incomingMatches.Add(pair);
                    _ = _matchedIncomingTypes.Add(incType);
                }

                if (_incomingMatches.Count >= MaxRandomPool)
                {
                    break;
                }
            }

            if (_incomingMatches.Count == 0)
            {
                return false;
            }

            pickReason = BuildIncomingPickReason();
            return FinishPickOldest(_incomingMatches, out speechEvent, out mimickingPlayerID, out pickReason, pickReason);
        }

        private static string BuildIncomingPickReason()
        {
            _incomingReasonBuilder.Clear();
            _incomingReasonBuilder.Append("Incoming:");

            bool first = true;
            foreach (SpeechEvent_IncomingType type in _matchedIncomingTypes)
            {
                if (!first)
                {
                    _ = _incomingReasonBuilder.Append(", ");
                }

                _ = _incomingReasonBuilder.Append(type);
                first = false;
            }

            return _incomingReasonBuilder.ToString();
        }

        private static bool MatchesIncomingType(SpeechEvent evt, SpeechEvent_IncomingType incType)
        {
            if (!_incomingIdsByType.TryGetValue(incType, out HashSet<int>? eventIdsForType))
            {
                return false;
            }

            List<IncomingEvent>? incoming = evt.GameData?.IncomingEventStart;
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }

            bool matched = false;
            for (int i = 0; i < incoming.Count; i++)
            {
                IncomingEvent candidate = incoming[i];
                if (candidate.EventType == incType && eventIdsForType.Contains(candidate.EventID))
                {
                    matched = true;
                    break;
                }
            }

            if (incType == SpeechEvent_IncomingType.Monster && eventIdsForType.Contains(MonsterMimicEventId))
            {
                for (int i = 0; i < incoming.Count; i++)
                {
                    IncomingEvent candidate = incoming[i];
                    if (candidate.EventType == SpeechEvent_IncomingType.GrabSkill && candidate.EventID == GrabSkillEventId)
                    {
                        matched = true;
                        _ = _matchedIncomingTypes.Add(SpeechEvent_IncomingType.GrabSkill);
                        break;
                    }
                }
            }

            return matched;
        }

        private static bool TryPickFromObservation(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechEventAdditionalGameData curGameData,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason)
        {
            speechEvent = null;
            mimickingPlayerID = string.Empty;
            pickReason = string.Empty;

            VoicePickSimilarity.ScrapCounts curScrap = VoicePickSimilarity.CountScrap(curGameData.ScrapObjects);
            int topCount = 0;

            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                VoicePickSimilarity.ScrapCounts eventScrap = VoicePickSimilarity.CountScrap(gameData.ScrapObjects);
                float similarity = VoicePickSimilarity.CalculateSimilarity(
                    gameData,
                    curGameData,
                    eventScrap,
                    curScrap);
                InsertTopSimilarity(pair.playerID, pair.evt, similarity, ref topCount);
            }

            if (topCount == 0)
            {
                return false;
            }

            int bestIndex = 0;
            float bestLastPlayed = _topSlots[0].evt.LastPlayedTime;
            for (int i = 1; i < topCount; i++)
            {
                if (_topSlots[i].evt.LastPlayedTime < bestLastPlayed)
                {
                    bestLastPlayed = _topSlots[i].evt.LastPlayedTime;
                    bestIndex = i;
                }
            }

            (string playerID, SpeechEvent evt, float _) pick = _topSlots[bestIndex];
            speechEvent = pick.evt;
            mimickingPlayerID = pick.playerID;
            VoicePickSimilarity.ScrapCounts pickScrap = VoicePickSimilarity.CountScrap(pick.evt.GameData?.ScrapObjects);
            pickReason = VoicePickSimilarity.GetTopFactorName(pick.evt.GameData!, curGameData, pickScrap, curScrap);
            return true;
        }

        private static void InsertTopSimilarity(
            string playerID,
            SpeechEvent evt,
            float similarity,
            ref int topCount)
        {
            int insertAt = topCount;
            for (int i = 0; i < topCount; i++)
            {
                if (similarity > _topSlots[i].similarity)
                {
                    insertAt = i;
                    break;
                }
            }

            if (topCount < TopK)
            {
                for (int i = topCount; i > insertAt; i--)
                {
                    _topSlots[i] = _topSlots[i - 1];
                }

                _topSlots[insertAt] = (playerID, evt, similarity);
                topCount++;
                return;
            }

            if (insertAt >= TopK)
            {
                return;
            }

            for (int i = TopK - 1; i > insertAt; i--)
            {
                _topSlots[i] = _topSlots[i - 1];
            }

            _topSlots[insertAt] = (playerID, evt, similarity);
        }

        private static bool FinishPickOldest(
            List<(string playerID, SpeechEvent evt)> source,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason,
            string reason)
        {
            speechEvent = null;
            mimickingPlayerID = string.Empty;
            pickReason = reason;

            if (source.Count == 0)
            {
                return false;
            }

            int bestIndex = 0;
            float bestLastPlayed = source[0].evt.LastPlayedTime;
            for (int i = 1; i < source.Count; i++)
            {
                if (source[i].evt.LastPlayedTime < bestLastPlayed)
                {
                    bestLastPlayed = source[i].evt.LastPlayedTime;
                    bestIndex = i;
                }
            }

            speechEvent = source[bestIndex].evt;
            mimickingPlayerID = source[bestIndex].playerID;
            return true;
        }

        private static void ShuffleInPlace(List<(string playerID, SpeechEvent evt)> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                if (swapIndex == i)
                {
                    continue;
                }

                (string playerID, SpeechEvent evt) temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        private static void MaybeLogPickProfile(int eventCount, long startTicks, bool picked, string reason)
        {
            long elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000 / Stopwatch.Frequency;
            if (elapsedMs < ProfileElapsedMsThreshold && eventCount < ProfileEventThreshold)
            {
                return;
            }

            ModLog.Debug(
                Feature,
                $"PickBestMatch profile — events={eventCount}, elapsed={elapsedMs}ms, picked={picked}, reason={reason}");
        }
    }
}
