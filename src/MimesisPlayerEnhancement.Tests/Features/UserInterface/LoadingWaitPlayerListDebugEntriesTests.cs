using MimesisPlayerEnhancement.Features.UserInterface;
using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListDebugEntriesTests
    {
        [Fact]
        public void BuildScrambled_includes_loaded_and_unloaded_when_multiple_players()
        {
            string[] names = ["Player 01", "Player 02", "Player 03", "Player 04"];

            List<LoadingWaitPlayerEntry> entries = LoadingWaitPlayerListDebugEntries.BuildScrambled(
                names,
                new System.Random(42));

            Assert.Equal(names.Length, entries.Count);
            Assert.Contains(entries, entry => entry.Loaded);
            Assert.Contains(entries, entry => !entry.Loaded);
        }

        [Fact]
        public void BuildScrambled_can_include_speaking_players()
        {
            string[] names = Enumerable.Range(1, 8).Select(index => $"Player {index:00}").ToArray();

            List<LoadingWaitPlayerEntry> entries = LoadingWaitPlayerListDebugEntries.BuildScrambled(
                names,
                new System.Random(7));

            Assert.Contains(entries, entry => entry.Speaking);
            Assert.Contains(entries, entry => !entry.Speaking);
        }

        [Fact]
        public void ScrambleTrueFlags_ensureMix_forces_both_states_when_count_is_two_or_more()
        {
            bool[] flags = UiDebugScramble.ScrambleTrueFlags(
                count: 6,
                trueRatio: 0.5f,
                ensureMix: true,
                new System.Random(99));

            Assert.Equal(6, flags.Length);
            Assert.Contains(true, flags);
            Assert.Contains(false, flags);
        }

        [Fact]
        public void BuildScrambled_sets_player_uid_and_display_name()
        {
            string[] names = ["Alpha", "Bravo"];

            List<LoadingWaitPlayerEntry> entries = LoadingWaitPlayerListDebugEntries.BuildScrambled(
                names,
                new System.Random(1));

            Assert.Equal("Alpha", entries[0].DisplayName);
            Assert.Equal("Bravo", entries[1].DisplayName);
            Assert.Equal(-1, entries[0].PlayerUid);
            Assert.Equal(-2, entries[1].PlayerUid);
        }
    }
}
