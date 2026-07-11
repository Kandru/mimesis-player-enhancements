using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Allocation-light reimplementation of vanilla <see cref="SpeechEventAdditionalGameData.PickBestMatch"/>.
    /// </summary>
    internal static class VoicePickBestMatch
    {
        private const float PlayTimeInterval = 60f;
        private const float DeathMatchPlayTimeInterval = 3f;
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

        private static readonly MethodInfo? GetTopFactorMethod =
            AccessTools.Method(typeof(SpeechEventAdditionalGameData), "GetTopFactor");

        private static readonly List<(string playerID, SpeechEvent evt)> _intervalFiltered = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _areaMatched = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _strictAreaPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _fallbackPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _generalIndoorPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _trapIndoorPool = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _noIncoming = [];
        private static readonly List<(string playerID, SpeechEvent evt)> _incomingMatches = [];
        private static readonly HashSet<SpeechEvent_IncomingType> _currentIncomingTypes = [];
        private static readonly HashSet<SpeechEvent_IncomingType> _matchedIncomingTypes = [];
        private static readonly HashSet<int> _eventIdsForType = [];
        private static readonly List<(string playerID, SpeechEvent evt, float similarity)> _topSimilar = [];

        internal static bool TryPick(
            MimicVoiceSpawner.MimicContext context,
            List<(string playerID, SpeechEvent evt)> allEvents,
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

            float playTimeIntervalTemp = curGameData.Area == SpeechType_Area.DeathMatch
                ? DeathMatchPlayTimeInterval + playTimeIntervalRandom
                : PlayTimeInterval;

            float now = GameSessionAccess.GetCurrentTickSec();
            FilterByReplayInterval(allEvents, playTimeIntervalTemp, now);
            if (_intervalFiltered.Count == 0)
            {
                return false;
            }

            if (!TryBuildCandidatePool(_intervalFiltered, curGameData, out string poolReason))
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

        private static void FilterByReplayInterval(
            List<(string playerID, SpeechEvent evt)> source,
            float playTimeIntervalTemp,
            float now)
        {
            _intervalFiltered.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                if (now - pair.evt.LastPlayedTime < playTimeIntervalTemp)
                {
                    continue;
                }

                _intervalFiltered.Add(pair);
            }
        }

        private static bool TryBuildCandidatePool(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechEventAdditionalGameData curGameData,
            out string poolReason)
        {
            _areaMatched.Clear();
            poolReason = string.Empty;

            if (!MoreVoicesUnify.IsActive)
            {
                FilterStrictIndoorEnteredAndArea(source, curGameData);
                if (_areaMatched.Count == 0)
                {
                    return false;
                }

                poolReason = $"Area({curGameData.Area}), Indoor({curGameData.IndoorEntered})";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (VoiceEventContext.IsDeathMatch(curGameData.Area))
            {
                FilterByArea(source, curGameData.Area, _areaMatched);
                if (_areaMatched.Count == 0)
                {
                    return false;
                }

                poolReason = $"StrictArea({curGameData.Area})";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (VoiceEventContext.IsOutdoorArea(curGameData.Area))
            {
                return TryBuildOutdoorCandidatePool(source, curGameData, out poolReason);
            }

            return TryBuildIndoorCandidatePool(source, curGameData, out poolReason);
        }

        private static bool TryBuildOutdoorCandidatePool(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechEventAdditionalGameData curGameData,
            out string poolReason)
        {
            FilterByArea(source, curGameData.Area, _strictAreaPool);
            BuildIndoorCrossPools(source);

            bool hasStrict = _strictAreaPool.Count > 0;
            bool hasGeneralIndoor = _generalIndoorPool.Count > 0;
            bool hasTrapIndoor = _trapIndoorPool.Count > 0;

            if (hasStrict && UnityEngine.Random.value >= OutdoorCrossIndoorChance)
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (hasGeneralIndoor)
            {
                CopyPool(_generalIndoorPool, _areaMatched);
                poolReason = "CrossIndoorGeneral";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (hasTrapIndoor && (!hasGeneralIndoor || UnityEngine.Random.value < TrapMonsterFallbackChance))
            {
                CopyPool(_trapIndoorPool, _areaMatched);
                poolReason = "CrossIndoorTrapMonster";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (hasStrict)
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            poolReason = string.Empty;
            return false;
        }

        private static bool TryBuildIndoorCandidatePool(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechEventAdditionalGameData curGameData,
            out string poolReason)
        {
            FilterByArea(source, curGameData.Area, _strictAreaPool);
            FilterCrossAreaFallback(source, curGameData.Area, _fallbackPool);

            bool hasStrict = _strictAreaPool.Count > 0;
            bool hasFallback = _fallbackPool.Count > 0;

            if (hasStrict && (UnityEngine.Random.value >= CrossAreaFallbackChance || !hasFallback))
            {
                CopyPool(_strictAreaPool, _areaMatched);
                poolReason = $"StrictArea({curGameData.Area})";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            if (hasFallback)
            {
                CopyPool(_fallbackPool, _areaMatched);
                poolReason = "CrossAreaFallback";
                ShuffleInPlace(_areaMatched);
                return true;
            }

            poolReason = string.Empty;
            return false;
        }

        private static void FilterStrictIndoorEnteredAndArea(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechEventAdditionalGameData curGameData)
        {
            _areaMatched.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                if (gameData.IndoorEntered == curGameData.IndoorEntered && gameData.Area == curGameData.Area)
                {
                    _areaMatched.Add(pair);
                }
            }
        }

        private static void FilterByArea(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechType_Area area,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                if (pair.evt.GameData?.Area == area)
                {
                    destination.Add(pair);
                }
            }
        }

        private static void FilterCrossAreaFallback(
            List<(string playerID, SpeechEvent evt)> source,
            SpeechType_Area currentArea,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null || VoiceEventContext.IsDeathMatch(gameData.Area))
                {
                    continue;
                }

                if (gameData.Area != currentArea)
                {
                    destination.Add(pair);
                }
            }
        }

        private static void BuildIndoorCrossPools(List<(string playerID, SpeechEvent evt)> source)
        {
            _generalIndoorPool.Clear();
            _trapIndoorPool.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null
                    || VoiceEventContext.IsDeathMatch(gameData.Area)
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

        private static bool TryPickIncomingMatch(
            SpeechEventAdditionalGameData curGameData,
            out SpeechEvent? speechEvent,
            out string mimickingPlayerID,
            out string pickReason)
        {
            speechEvent = null;
            mimickingPlayerID = string.Empty;
            pickReason = string.Empty;

            _currentIncomingTypes.Clear();
            List<IncomingEvent> incomingStart = curGameData.IncomingEventStart;
            for (int i = 0; i < incomingStart.Count; i++)
            {
                _ = _currentIncomingTypes.Add(incomingStart[i].EventType);
            }

            _incomingMatches.Clear();
            _matchedIncomingTypes.Clear();

            for (int typeIndex = 0; typeIndex < IncomingTypePriority.Length; typeIndex++)
            {
                SpeechEvent_IncomingType incType = IncomingTypePriority[typeIndex];
                if (!_currentIncomingTypes.Contains(incType))
                {
                    continue;
                }

                _eventIdsForType.Clear();
                for (int i = 0; i < incomingStart.Count; i++)
                {
                    IncomingEvent incoming = incomingStart[i];
                    if (incoming.EventType == incType)
                    {
                        _ = _eventIdsForType.Add(incoming.EventID);
                    }
                }

                for (int i = 0; i < _areaMatched.Count; i++)
                {
                    (string playerID, SpeechEvent evt) pair = _areaMatched[i];
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

            string reason = "Incoming:" + string.Join(", ", _matchedIncomingTypes);
            return FinishPickOldest(_incomingMatches, out speechEvent, out mimickingPlayerID, out pickReason, reason);
        }

        private static bool MatchesIncomingType(SpeechEvent evt, SpeechEvent_IncomingType incType)
        {
            List<IncomingEvent>? incoming = evt.GameData?.IncomingEventStart;
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }

            bool matched = false;
            for (int i = 0; i < incoming.Count; i++)
            {
                IncomingEvent candidate = incoming[i];
                if (candidate.EventType == incType && _eventIdsForType.Contains(candidate.EventID))
                {
                    matched = true;
                    break;
                }
            }

            if (incType == SpeechEvent_IncomingType.Monster && _eventIdsForType.Contains(MonsterMimicEventId))
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

            _topSimilar.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                (string playerID, SpeechEvent evt) pair = source[i];
                SpeechEventAdditionalGameData? gameData = pair.evt.GameData;
                if (gameData == null)
                {
                    continue;
                }

                float similarity = SpeechEventAdditionalGameData.CalculateSimilarity(gameData, curGameData);
                InsertTopSimilarity(pair.playerID, pair.evt, similarity);
            }

            if (_topSimilar.Count == 0)
            {
                return false;
            }

            int bestIndex = 0;
            float bestLastPlayed = _topSimilar[0].evt.LastPlayedTime;
            for (int i = 1; i < _topSimilar.Count; i++)
            {
                if (_topSimilar[i].evt.LastPlayedTime < bestLastPlayed)
                {
                    bestLastPlayed = _topSimilar[i].evt.LastPlayedTime;
                    bestIndex = i;
                }
            }

            (string playerID, SpeechEvent evt, float _) pick = _topSimilar[bestIndex];
            speechEvent = pick.evt;
            mimickingPlayerID = pick.playerID;
            pickReason = ResolveTopFactor(pick.evt.GameData, curGameData);
            return true;
        }

        private static void InsertTopSimilarity(string playerID, SpeechEvent evt, float similarity)
        {
            int insertAt = _topSimilar.Count;
            for (int i = 0; i < _topSimilar.Count; i++)
            {
                if (similarity > _topSimilar[i].similarity)
                {
                    insertAt = i;
                    break;
                }
            }

            if (_topSimilar.Count < TopK)
            {
                _topSimilar.Insert(insertAt, (playerID, evt, similarity));
                return;
            }

            if (insertAt >= TopK)
            {
                return;
            }

            _topSimilar.Insert(insertAt, (playerID, evt, similarity));
            _topSimilar.RemoveAt(TopK);
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

        private static string ResolveTopFactor(
            SpeechEventAdditionalGameData? eventGameData,
            SpeechEventAdditionalGameData curGameData)
        {
            if (eventGameData == null || GetTopFactorMethod == null)
            {
                return string.Empty;
            }

            try
            {
                return (string)GetTopFactorMethod.Invoke(null, [eventGameData, curGameData])!;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
