using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Allocation-free helpers for <see cref="VoicePickBestMatch"/> observation scoring.
    /// </summary>
    internal static class VoicePickSimilarity
    {
        private const int WeightPlayerCount = 1;
        private const int WeightFacingPlayerCount = 2;
        private const int WeightOneHandScrapObject = 1;
        private const int WeightTwoHandScrapObject = 1;
        private const int WeightMonsters = 1;
        private const int WeightTeleporter = 1;
        private const int WeightCharger = 1;
        private const int WeightCrowShop = 1;
        private const int WeightPlayTime = 1;

        private static readonly MethodInfo? IsOneHandScrapMethod =
            AccessTools.Method(typeof(SpeechEventAdditionalGameData), "IsOneHandScrap");

        internal readonly struct ScrapCounts
        {
            internal ScrapCounts(int oneHand, int twoHand)
            {
                OneHand = oneHand;
                TwoHand = twoHand;
            }

            internal int OneHand { get; }
            internal int TwoHand { get; }
        }

        internal static ScrapCounts CountScrap(List<int>? scrapObjects)
        {
            if (scrapObjects == null || scrapObjects.Count == 0 || IsOneHandScrapMethod == null)
            {
                return new ScrapCounts(0, 0);
            }

            int oneHand = 0;
            int twoHand = 0;
            for (int i = 0; i < scrapObjects.Count; i++)
            {
                bool isOneHand;
                try
                {
                    isOneHand = (bool)IsOneHandScrapMethod.Invoke(null, [scrapObjects[i]])!;
                }
                catch
                {
                    continue;
                }

                if (isOneHand)
                {
                    if (oneHand < 3)
                    {
                        oneHand++;
                    }
                }
                else if (twoHand < 2)
                {
                    twoHand++;
                }

                if (oneHand >= 3 && twoHand >= 2)
                {
                    break;
                }
            }

            return new ScrapCounts(oneHand, twoHand);
        }

        internal static float CalculateSimilarity(
            SpeechEventAdditionalGameData eventGameData,
            SpeechEventAdditionalGameData curGameData,
            ScrapCounts eventScrap,
            ScrapCounts curScrap)
        {
            float monsters = CalcSimTerm(
                eventGameData.Monsters.Count < 3 ? eventGameData.Monsters.Count : 3,
                curGameData.Monsters.Count < 3 ? curGameData.Monsters.Count : 3,
                3f);
            float score = WeightMonsters * monsters;

            score += WeightPlayerCount * CalcSimTerm(
                (float)eventGameData.AdjacentPlayerCount,
                (float)curGameData.AdjacentPlayerCount,
                2f);

            float eventFacing = eventGameData.FacingPlayerCount == SpeechType_FacingPlayerCount.None ? 0f : 1f;
            float curFacing = curGameData.FacingPlayerCount == SpeechType_FacingPlayerCount.None ? 0f : 1f;
            score += WeightFacingPlayerCount * CalcSimTerm(eventFacing, curFacing, 1f);

            float eventTeleporter = eventGameData.Teleporter == SpeechType_Teleporter.None ? 0f : 1f;
            float curTeleporter = curGameData.Teleporter == SpeechType_Teleporter.None ? 0f : 1f;
            score += WeightTeleporter * CalcSimTerm(eventTeleporter, curTeleporter, 1f);

            score += WeightPlayTime * CalcSimTerm(
                (float)eventGameData.GameTime,
                (float)curGameData.GameTime,
                2f);

            score += WeightOneHandScrapObject * CalcSimTerm(
                eventScrap.OneHand,
                curScrap.OneHand,
                3f);

            score += WeightTwoHandScrapObject * CalcSimTerm(
                eventScrap.TwoHand,
                curScrap.TwoHand,
                2f);

            float eventCharger = eventGameData.Charger == SpeechType_Charger.None ? 0f : 1f;
            float curCharger = curGameData.Charger == SpeechType_Charger.None ? 0f : 1f;
            score += WeightCharger * CalcSimTerm(eventCharger, curCharger, 1f);

            float eventCrowShop = eventGameData.CrowShop == SpeechType_CrowShop.None ? 0f : 1f;
            float curCrowShop = curGameData.CrowShop == SpeechType_CrowShop.None ? 0f : 1f;
            score += WeightCrowShop * CalcSimTerm(eventCrowShop, curCrowShop, 1f);

            return score;
        }

        internal static string GetTopFactorName(
            SpeechEventAdditionalGameData eventGameData,
            SpeechEventAdditionalGameData curGameData,
            ScrapCounts eventScrap,
            ScrapCounts curScrap)
        {
            string bestKey = string.Empty;
            float bestValue = float.MinValue;

            Consider(ref bestKey, ref bestValue, "Monsters", WeightMonsters * CalcSimTerm(
                eventGameData.Monsters.Count < 3 ? eventGameData.Monsters.Count : 3,
                curGameData.Monsters.Count < 3 ? curGameData.Monsters.Count : 3,
                3f));

            Consider(ref bestKey, ref bestValue, "AdjacentPlayerCount", WeightPlayerCount * CalcSimTerm(
                (float)eventGameData.AdjacentPlayerCount,
                (float)curGameData.AdjacentPlayerCount,
                2f));

            float eventFacing = eventGameData.FacingPlayerCount == SpeechType_FacingPlayerCount.None ? 0f : 1f;
            float curFacing = curGameData.FacingPlayerCount == SpeechType_FacingPlayerCount.None ? 0f : 1f;
            Consider(ref bestKey, ref bestValue, "FacingPlayerCount", WeightFacingPlayerCount * CalcSimTerm(eventFacing, curFacing, 1f));

            float eventTeleporter = eventGameData.Teleporter == SpeechType_Teleporter.None ? 0f : 1f;
            float curTeleporter = curGameData.Teleporter == SpeechType_Teleporter.None ? 0f : 1f;
            Consider(ref bestKey, ref bestValue, "Teleporter", WeightTeleporter * CalcSimTerm(eventTeleporter, curTeleporter, 1f));

            Consider(ref bestKey, ref bestValue, "GameTime", WeightPlayTime * CalcSimTerm(
                (float)eventGameData.GameTime,
                (float)curGameData.GameTime,
                2f));

            Consider(ref bestKey, ref bestValue, "OneHandScrap", WeightOneHandScrapObject * CalcSimTerm(
                eventScrap.OneHand,
                curScrap.OneHand,
                3f));

            Consider(ref bestKey, ref bestValue, "TwoHandScrap", WeightTwoHandScrapObject * CalcSimTerm(
                eventScrap.TwoHand,
                curScrap.TwoHand,
                2f));

            float eventCharger = eventGameData.Charger == SpeechType_Charger.None ? 0f : 1f;
            float curCharger = curGameData.Charger == SpeechType_Charger.None ? 0f : 1f;
            Consider(ref bestKey, ref bestValue, "Charger", WeightCharger * CalcSimTerm(eventCharger, curCharger, 1f));

            float eventCrowShop = eventGameData.CrowShop == SpeechType_CrowShop.None ? 0f : 1f;
            float curCrowShop = curGameData.CrowShop == SpeechType_CrowShop.None ? 0f : 1f;
            Consider(ref bestKey, ref bestValue, "CrowShop", WeightCrowShop * CalcSimTerm(eventCrowShop, curCrowShop, 1f));

            return bestKey;
        }

        private static void Consider(ref string bestKey, ref float bestValue, string key, float value)
        {
            if (value > bestValue)
            {
                bestValue = value;
                bestKey = key;
            }
        }

        internal static float CalcSimTerm(float a, float b, float maxDiff)
        {
            if (maxDiff <= 0f)
            {
                return 0f;
            }

            float normalized = 1f - Math.Abs(a - b) / maxDiff;
            if (normalized < 0f)
            {
                return 0f;
            }

            if (normalized > 1f)
            {
                return 1f;
            }

            return normalized;
        }
    }
}
