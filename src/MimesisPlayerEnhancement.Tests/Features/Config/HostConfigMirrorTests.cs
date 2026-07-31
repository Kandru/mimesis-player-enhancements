using MimesisPlayerEnhancement.Config.HostConfigSync;
using MimesisPlayerEnhancement.Config.QuickSettings;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class HostConfigMirrorTests
    {
        [Fact]
        public void Clear_on_fresh_mirror_is_noop()
        {
            HostConfigMirror.Clear();
            Assert.False(HostConfigMirror.IsActive);
            Assert.Equal(-1, HostConfigMirror.MirroredSlotId);
            Assert.Equal(0, HostConfigMirror.Revision);
            Assert.False(HostConfigMirror.BlocksGlobalConfigPersistence);
        }

        [Fact]
        public void ApplySnapshot_sets_active_and_blocks_global_persistence()
        {
            HostConfigMirror.Clear();

            HostConfigSyncEnvelope envelope = new()
            {
                V = HostConfigSyncCodec.ProtocolVersion,
                Rev = 3,
                SlotId = 1,
                Profile = new SaveConfigProfileState { Mode = SaveConfigProfileMode.Global },
            };

            _ = HostConfigMirror.ApplySnapshot(envelope);

            Assert.True(HostConfigMirror.IsActive);
            Assert.True(HostConfigMirror.BlocksGlobalConfigPersistence);
            Assert.Equal(3, HostConfigMirror.Revision);
            Assert.Equal(1, HostConfigMirror.MirroredSlotId);

            HostConfigMirror.Clear();
            Assert.False(HostConfigMirror.BlocksGlobalConfigPersistence);
        }

        [Fact]
        public void ApplySnapshot_ignores_stale_revision_when_active()
        {
            HostConfigMirror.Clear();

            HostConfigSyncEnvelope initial = new()
            {
                V = HostConfigSyncCodec.ProtocolVersion,
                Rev = 5,
                SlotId = 2,
                Profile = new SaveConfigProfileState(),
            };
            Assert.True(HostConfigMirror.ApplySnapshot(initial));

            HostConfigSyncEnvelope stale = new()
            {
                V = HostConfigSyncCodec.ProtocolVersion,
                Rev = 4,
                SlotId = 2,
                Profile = new SaveConfigProfileState(),
            };
            Assert.False(HostConfigMirror.ApplySnapshot(stale));
            Assert.Equal(5, HostConfigMirror.Revision);

            HostConfigMirror.Clear();
        }
    }
}
