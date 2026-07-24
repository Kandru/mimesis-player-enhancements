using MimesisPlayerEnhancement.Features.MoreVoices;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MoreVoices
{
    public sealed class MoreVoicesRecordingTests
    {
        private static readonly Hub.PersistentData.eServerRoomState InGame =
            Hub.PersistentData.eServerRoomState.InGame;

        private static readonly Hub.PersistentData.eServerRoomState PreGame =
            Hub.PersistentData.eServerRoomState.PreGame;

        [Fact]
        public void ShouldSyncRecordedEvent_force_always_syncs()
        {
            Assert.True(MoreVoicesRecording.ShouldSyncRecordedEvent(isForce: true, null, shouldRecordInHubScene: false));
        }

        [Fact]
        public void ShouldSyncRecordedEvent_null_state_without_force_does_not_sync()
        {
            Assert.False(MoreVoicesRecording.ShouldSyncRecordedEvent(isForce: false, null, shouldRecordInHubScene: true));
        }

        [Fact]
        public void ShouldSyncRecordedEvent_ingame_syncs()
        {
            Assert.True(MoreVoicesRecording.ShouldSyncRecordedEvent(isForce: false, InGame, shouldRecordInHubScene: false));
        }

        [Fact]
        public void ShouldSyncRecordedEvent_pregame_requires_hub_recording()
        {
            Assert.True(MoreVoicesRecording.ShouldSyncRecordedEvent(isForce: false, PreGame, shouldRecordInHubScene: true));
            Assert.False(MoreVoicesRecording.ShouldSyncRecordedEvent(isForce: false, PreGame, shouldRecordInHubScene: false));
        }

        [Fact]
        public void VanillaWouldSyncRecordedEvent_matches_ingame_or_force()
        {
            Assert.True(MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce: true, PreGame));
            Assert.True(MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce: false, InGame));
            Assert.False(MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce: false, PreGame));
            Assert.False(MoreVoicesRecording.VanillaWouldSyncRecordedEvent(isForce: false, null));
        }
    }
}
