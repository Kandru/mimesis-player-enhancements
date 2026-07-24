using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeLobbyNameLogicTests
    {
        [Theory]
        [InlineData("Train [join now] (2/4)", "Train")]
        [InlineData("My Lobby [join in 5 min] (3/8)", "My Lobby")]
        [InlineData("Plain (1/4)", "Plain")]
        [InlineData("NoSuffix", "NoSuffix")]
        public void StripDisplaySuffix_removes_join_tag_and_count(string input, string expected)
        {
            Assert.Equal(expected, JoinAnytimeLobbyNameLogic.StripDisplaySuffix(input));
        }

        [Fact]
        public void BuildDisplayLobbyName_uses_join_now_outside_timed_dungeon()
        {
            string actual = JoinAnytimeLobbyNameLogic.BuildDisplayLobbyName(
                "Train",
                JoinAnytimeSessionPhase.Tram,
                waitMinutes: 0,
                sessionCount: 2,
                maxPlayers: 4);

            Assert.Equal("Train [join now] (2/4)", actual);
        }

        [Fact]
        public void BuildDisplayLobbyName_uses_join_in_minutes_in_dungeon()
        {
            string actual = JoinAnytimeLobbyNameLogic.BuildDisplayLobbyName(
                "Train",
                JoinAnytimeSessionPhase.Dungeon,
                waitMinutes: 7,
                sessionCount: 3,
                maxPlayers: 8);

            Assert.Equal("Train [join in 7 min] (3/8)", actual);
        }

        [Fact]
        public void BuildDisplayLobbyName_falls_back_to_default_base_name()
        {
            string actual = JoinAnytimeLobbyNameLogic.BuildDisplayLobbyName(
                "  ",
                JoinAnytimeSessionPhase.Maintenance,
                waitMinutes: 0,
                sessionCount: 1,
                maxPlayers: 4);

            Assert.Equal("Train [join now] (1/4)", actual);
        }

        [Fact]
        public void ToPhaseKey_maps_session_phase()
        {
            Assert.Equal("maintenance", JoinAnytimeLobbyNameLogic.ToPhaseKey(JoinAnytimeSessionPhase.Maintenance));
            Assert.Equal("tram", JoinAnytimeLobbyNameLogic.ToPhaseKey(JoinAnytimeSessionPhase.Tram));
            Assert.Equal("dungeon", JoinAnytimeLobbyNameLogic.ToPhaseKey(JoinAnytimeSessionPhase.Dungeon));
            Assert.Equal("", JoinAnytimeLobbyNameLogic.ToPhaseKey(JoinAnytimeSessionPhase.None));
        }
    }
}
