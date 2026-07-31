using MimesisPlayerEnhancement.Features.SavegamePreparation;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationNewGameGateTests
    {
        [Fact]
        public void Arm_and_disarm_track_depth()
        {
            SavegamePreparationNewGameGate.Reset();
            Assert.False(SavegamePreparationNewGameGate.IsArmed);

            SavegamePreparationNewGameGate.Arm();
            Assert.True(SavegamePreparationNewGameGate.IsArmed);

            SavegamePreparationNewGameGate.Disarm();
            Assert.False(SavegamePreparationNewGameGate.IsArmed);
        }

        [Fact]
        public void Reset_clears_armed_state()
        {
            SavegamePreparationNewGameGate.Arm();
            SavegamePreparationNewGameGate.Reset();
            Assert.False(SavegamePreparationNewGameGate.IsArmed);
        }
    }
}
