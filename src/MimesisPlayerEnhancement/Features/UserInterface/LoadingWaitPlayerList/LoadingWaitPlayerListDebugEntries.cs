namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListDebugEntries
    {
        internal static List<LoadingWaitPlayerEntry> BuildScrambled(
            IReadOnlyList<string> fakeNames,
            System.Random? random = null)
        {
            int count = fakeNames.Count;
            bool[] loadedFlags = UiDebugScramble.ScrambleTrueFlags(count, trueRatio: 0.5f, ensureMix: true, random);
            bool[] speakingFlags = UiDebugScramble.ScrambleTrueFlags(
                count,
                trueRatio: 0.35f,
                ensureMix: false,
                random);

            List<LoadingWaitPlayerEntry> entries = new(count);
            for (int index = 0; index < count; index++)
            {
                entries.Add(new LoadingWaitPlayerEntry
                {
                    PlayerUid = -(index + 1),
                    DisplayName = fakeNames[index],
                    Loaded = loadedFlags[index],
                    Speaking = speakingFlags[index],
                });
            }

            return entries;
        }
    }
}
