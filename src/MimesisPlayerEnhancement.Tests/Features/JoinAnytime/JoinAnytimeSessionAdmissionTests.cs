using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeSessionAdmissionTests
    {
        [Theory]
        [InlineData(1)] // Ready
        [InlineData(2)] // WaitStartSession
        [InlineData(6)] // EndGame
        public void ResolveCanEnter_returns_true_for_open_admission_states(int stateValue)
        {
            bool actual = JoinAnytimeSessionAdmission.ResolveCanEnter(stateValue, joinsOpen: false);

            Assert.True(actual);
        }

        [Theory]
        [InlineData(4)] // OnPlaying
        [InlineData(7)] // DeathMatch
        [InlineData(5)] // AfterGame
        public void ResolveCanEnter_returns_false_for_closed_admission_states(int stateValue)
        {
            bool actual = JoinAnytimeSessionAdmission.ResolveCanEnter(stateValue, joinsOpen: true);

            Assert.False(actual);
        }

        [Theory]
        [InlineData(3, true, true)] // PreGame
        [InlineData(3, false, false)]
        public void ResolveCanEnter_uses_joinsOpen_for_default_states(
            int stateValue,
            bool joinsOpen,
            bool expected)
        {
            bool actual = JoinAnytimeSessionAdmission.ResolveCanEnter(stateValue, joinsOpen);

            Assert.Equal(expected, actual);
        }
    }
}
